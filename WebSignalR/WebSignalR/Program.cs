using WebSignalR;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddSingleton<RedisConnectionManager>();
builder.Services.AddHostedService<RedisSentinelMonitor>();

// Configure SignalR with Redis backplane (initially empty, will swap at runtime)
builder.Services.AddSignalR().AddStackExchangeRedis("mymaster", options =>
{
    // We'll update this connection later via RedisSentinelMonitor
    options.Configuration.ChannelPrefix = "signalr";
    options.ConnectionFactory = async writer =>
    {
        while (RedisConnectionManager.CurrentConnection == null)
        {
            Console.WriteLine("⏳ Waiting for Redis master connection from Sentinel...");
            await Task.Delay(1000).ConfigureAwait(false); // wait 1 second before retry
        }

        Console.WriteLine("✅ Using current Redis connection for SignalR.");
        return RedisConnectionManager.CurrentConnection!.GetSubscriber().Multiplexer;
    };
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<ChatHub>("/chat");

app.Run();
