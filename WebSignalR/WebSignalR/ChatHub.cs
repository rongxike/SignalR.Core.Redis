using Microsoft.AspNetCore.SignalR;

namespace WebSignalR;

public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message).ConfigureAwait(false);
    }
}
