using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
[assembly: OwinStartup(typeof(SignalRChat.Startup))]
namespace SignalRChat
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.DependencyResolver.UseStackExchangeRedis("localhost", 6379, "", "memurai");
            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}