using StackExchange.Redis;

namespace WebSignalR;

public class RedisConnectionManager
{
    private static readonly object _lock = new();
    public static ConnectionMultiplexer? CurrentConnection { get; private set; }

    public void UpdateConnection(string host, int port)
    {
        lock (_lock)
        {
            var config = new ConfigurationOptions
            {
                EndPoints = { $"{host}:{port}" },
                AbortOnConnectFail = false
            };

            CurrentConnection?.Dispose();
            CurrentConnection = ConnectionMultiplexer.Connect(config);
            Console.WriteLine($"✅ Redis master connected: {host}:{port}");
        }
    }
}
