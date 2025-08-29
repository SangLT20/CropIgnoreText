using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// (Tùy chọn) Nếu bạn host frontend chỗ khác, bật CORS:
// builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
//     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
//     .SetIsOriginAllowed(_ => true)));

builder.Services.AddSignalR();
builder.Services.AddRouting();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
// app.UseCors();

app.MapHub<SignalingHub>("/signalr");
app.Run();

public class SignalingHub : Hub
{
    public string GetConnectionId() => Context.ConnectionId;

    public async Task SendOffer(string targetConnectionId, string sdp)
        => await Clients.Client(targetConnectionId)
            .SendAsync("ReceiveOffer", Context.ConnectionId, sdp);

    public async Task SendAnswer(string targetConnectionId, string sdp)
        => await Clients.Client(targetConnectionId)
            .SendAsync("ReceiveAnswer", Context.ConnectionId, sdp);

    public async Task SendIceCandidate(string targetConnectionId, string candidate)
        => await Clients.Client(targetConnectionId)
            .SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidate);
}
