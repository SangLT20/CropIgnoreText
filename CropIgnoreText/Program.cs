using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Dnn;

class Program
{
    // Đường dẫn FFmpeg executable (nếu chưa thêm vào PATH, chỉnh phù hợp)
    const string FfmpegExe = "ffmpeg";

    static void Main(string[] args)
    {
        try
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: CropIgnoreText.exe <input.mp4> <output.mp4>");
                return;
            }

            var inputPath = args[0];
            var outputPath = args[1];

            // 1. Detect text bounding boxes trên N sample frames
            var boxes = SampleAndDetect(inputPath, samples: 10);

            if (boxes.Count == 0)
            {
                Console.WriteLine("Không phát hiện văn bản nào, bỏ qua bước crop.");
                return;
            }

            // 2. Tính union bounding box
            var union = boxes.Aggregate(Rect.Union);

            Console.WriteLine($"Detected text region: X={union.X}, Y={union.Y}, W={union.Width}, H={union.Height}");

            // 3. Lấy thông số crop hai phần trái + phải
            //    Left width = union.X
            //    Right width = fullWidth - union.X - union.Width
            using var cap = new VideoCapture(inputPath);
            int iw = (int)cap.Get(VideoCaptureProperties.FrameWidth);
            int ih = (int)cap.Get(VideoCaptureProperties.FrameHeight);

            int leftW = union.X;
            int rightW = iw - union.X - union.Width;

            if (leftW <= 0 || rightW <= 0)
            {
                Console.WriteLine("Vùng text chiếm toàn bộ chiều ngang, không thể crop hai phía.");
                return;
            }

            // 4. Build filter_complex: crop trái + crop phải + hstack
            string filter =
                $"[0:v]crop={leftW}:{ih}:0:0[left];" +
                $"[0:v]crop={rightW}:{ih}:{union.X + union.Width}:0[right];" +
                $"[left][right]hstack=inputs=2";

            // 5. Chạy FFmpeg
            var psi = new ProcessStartInfo
            {
                FileName = FfmpegExe,
                Arguments = $"-i \"{inputPath}\" -filter_complex \"{filter}\" -c:v libx264 -c:a copy \"{outputPath}\" -y",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = true
            };

            Console.WriteLine("Running FFmpeg:\n" + psi.FileName + " " + psi.Arguments);
            var ff = Process.Start(psi);
            ff.WaitForExit();
            Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

        }
    }

    // Lấy N frame mẫu và detect text bounding box
    static List<Rect> SampleAndDetect(string videoPath, int samples)
    {
        var net = CvDnn.ReadNet("frozen_east_text_detection.pb");
        var cap = new VideoCapture(videoPath);
        int total = (int)cap.Get(VideoCaptureProperties.FrameCount);
        var boxes = new List<Rect>();

        for (int i = 0; i < samples; i++)
        {
            int idx = i * total / samples;
            cap.Set(VideoCaptureProperties.PosFrames, idx);
            using var frame = new Mat();
            if (!cap.Read(frame) || frame.Empty()) continue;

            boxes.AddRange(DetectTextBoxes(frame, net));
        }
        cap.Release();
        return boxes;
    }

    // Dùng EAST để detect text
    static IEnumerable<Rect> DetectTextBoxes(Mat frame, Net net)
    {
        int h = frame.Rows, w = frame.Cols;
        int newW = (w / 32) * 32, newH = (h / 32) * 32;
        using var blob = CvDnn.BlobFromImage(frame, 1.0, new Size(newW, newH),
                                             new Scalar(123.68, 116.78, 103.94), true, false);
        net.SetInput(blob);
        Mat scores = net.Forward("feature_fusion/Conv_7/Sigmoid");
        Mat geometry = net.Forward("feature_fusion/concat_3");

        var rects = new List<Rect>();
        var confidences = new List<float>();

        int H = scores.Size(2), W = scores.Size(3);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float score = scores.At<float>(0, 0, y, x);
                if (score < 0.5) continue;

                float offsetX = x * 4.0f, offsetY = y * 4.0f;
                float angle   = geometry.At<float>(0, 4, y, x);
                float cosA = (float)Math.Cos(angle), sinA = (float)Math.Sin(angle);
                float hBox = geometry.At<float>(0, 0, y, x) + geometry.At<float>(0, 2, y, x);
                float wBox = geometry.At<float>(0, 1, y, x) + geometry.At<float>(0, 3, y, x);

                int endX = (int)(offsetX + cosA * geometry.At<float>(0, 1, y, x) + sinA * geometry.At<float>(0, 2, y, x));
                int endY = (int)(offsetY - sinA * geometry.At<float>(0, 1, y, x) + cosA * geometry.At<float>(0, 2, y, x));
                int startX = endX - (int)wBox;
                int startY = endY - (int)hBox;

                rects.Add(new Rect(startX, startY, (int)wBox, (int)hBox));
                confidences.Add(score);
            }
        }

        // NMS
        int[] indices;
        CvDnn.NMSBoxes(rects, confidences, 0.5f, 0.4f, out indices);
        foreach (var idx in indices) yield return rects[idx];
    }
}
