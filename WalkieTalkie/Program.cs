using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

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
    private static Dictionary<string, string> ConnectedUsers = new();
    public string GetConnectionId() => Context.ConnectionId;
    public override Task OnConnectedAsync()
    {
        string userName = Context.GetHttpContext().Request.Query["username"];
        ConnectedUsers[Context.ConnectionId] = userName + " " + DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

        // Send updated user list to everyone
        Clients.All.SendAsync("UserListUpdated", ConnectedUsers);

        return base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception exception)
    {
        ConnectedUsers.Remove(Context.ConnectionId);
        Clients.All.SendAsync("UserListUpdated", ConnectedUsers);
        return base.OnDisconnectedAsync(exception);
    }
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
