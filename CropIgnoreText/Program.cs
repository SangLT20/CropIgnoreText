using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenCvSharp;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            string ffmpegPath = @"ffmpeg"; // path to ffmpeg.exe
            string inputFile = @"D:\4.BEvideo\FFmpeg\TestProject\CropIgnoreText\CropIgnoreText\input.mp4";
            string outputFile = @"D:\4.BEvideo\FFmpeg\TestProject\CropIgnoreText\CropIgnoreText\frame.png";
            int min = 10, max = 200;
            int xDimension = 2, yDimension = 2;

            // Load the image
            Mat image = Cv2.ImRead(outputFile, ImreadModes.Color);

            if (image.Empty())
            {
                Console.WriteLine("Could not open or find the image!");
                return;
            }

            int width = image.Width;
            int height = image.Height;

            Console.WriteLine($"Image size: {width}x{height}");

            // Loop through all pixels
            for (int y = 0; y < height; y+=yDimension)
            {
                for (int x = 0; x < width; x+=xDimension)
                {
                    int sumBlue = 0, sumGreen = 0, sumRed = 0;
                    for (int w = 0; w < xDimension; w++)
                    {
                        for (int h = 0; h < yDimension; h++)
                        {
                            // Get pixel value (BGR format)
                            Vec3b color = image.At<Vec3b>(y+h, x+w);

                            byte blue = color.Item0;
                            byte green = color.Item1;
                            byte red = color.Item2;

                            if (blue > min || green > min && red > min)
                                goto newPosi;

                            //if (blue > min
                            //&& sumGreen > min
                            //&& sumRed > min
                            //&& blue > max
                            //&& sumGreen > max
                            //&& sumRed > max)
                            //{
                            //    //Console.WriteLine($"Pixel[{x},{y}] = (R:{red}, G:{green}, B:{blue})");
                            //    image.Set(y, x, new Vec3b(255, 0, 0));
                            //}

                            //sumBlue += blue;
                            //sumGreen += green;
                            //sumRed += red;
                        }
                    }

                    image.Set(y, x, new Vec3b(255, 0, 0));
                newPosi:;
                    //if (sumBlue/(xDimension*yDimension) > min 
                    //    && sumGreen / (xDimension * yDimension) > min 
                    //    && sumRed / (xDimension * yDimension) > min
                    //    && sumBlue / (xDimension * yDimension) > max
                    //    && sumGreen / (xDimension * yDimension) > max
                    //    && sumRed / (xDimension * yDimension) > max)
                    //{
                    //    //Console.WriteLine($"Pixel[{x},{y}] = (R:{red}, G:{green}, B:{blue})");
                    //    image.Set(y, x, new Vec3b(255, 0, 0));
                    //}
                }
            }
            // Show the image in a window
            Cv2.ImShow("My Image", image);
            // Wait until a key is pressed
            Cv2.WaitKey(0);
            // Destroy all windows after closing
            Cv2.DestroyAllWindows();

            ////if (args.Length < 2)
            ////{
            ////    Console.WriteLine("Usage: CropIgnoreText.exe <input.mp4> <output.mp4>");
            ////    return;
            ////}


            //// Command: ffmpeg -ss 1 -i input.mp4 -frames:v 1 output.png
            //string arguments = $"-ss 1 -i \"{inputFile}\" -frames:v 1 \"{outputFile}\" -y";

            //ProcessStartInfo startInfo = new ProcessStartInfo
            //{
            //    FileName = ffmpegPath,
            //    Arguments = arguments,
            //    RedirectStandardOutput = true,
            //    RedirectStandardError = true,
            //    UseShellExecute = false,
            //    CreateNoWindow = true
            //};

            //using Process process = new Process { StartInfo = startInfo };
            //process.Start();

            //// Capture FFmpeg logs if needed
            //string output = process.StandardOutput.ReadToEnd();
            //string error = process.StandardError.ReadToEnd();

            //process.WaitForExit();

            //Console.WriteLine("FFmpeg finished.");
            //Console.WriteLine(error); // shows ffmpeg log
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

        }
    }
}
