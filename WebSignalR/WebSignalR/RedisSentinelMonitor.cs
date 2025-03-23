using StackExchange.Redis;
using System.Net;

namespace WebSignalR;

public class RedisSentinelMonitor : BackgroundService
{
    private readonly RedisConnectionManager _connectionManager;
    private readonly ILogger<RedisSentinelMonitor> _logger;

    private readonly (string host, int port)[] sentinelEndpoints = new[]
    {
        ("localhost", 26379),
        ("localhost", 26380),
        ("localhost", 26381)
    };

    private const string MasterName = "mymaster";
    private string? lastMaster;

    public RedisSentinelMonitor(RedisConnectionManager connectionManager, ILogger<RedisSentinelMonitor> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var (host, port) in sentinelEndpoints)
            {
                try
                {
                    var sentinelConfig = new ConfigurationOptions
                    {
                        CommandMap = CommandMap.Sentinel,
                        AllowAdmin = true,
                        AbortOnConnectFail = false,
                        TieBreaker = "",
                    };
                    sentinelConfig.EndPoints.Add(host, port);

                    using var sentinel = await ConnectionMultiplexer.ConnectAsync(sentinelConfig).ConfigureAwait(false);

                    var server = sentinel.GetServer(host, port);
                    var master = server.SentinelGetMasterAddressByName(MasterName);

                    if (master == null)
                    {
                        _logger.LogWarning($"❌ Sentinel {host}:{port} returned null for master.");
                        continue;
                    }

                    // Cast to IPEndPoint to get IP and port
                    if (master is IPEndPoint ipEndPoint)
                    {
#if DEBUG
                        var masterHost = "localhost"; // ipEndPoint.Address.ToString();
#else
                        var masterHost = ipEndPoint.Address.ToString();
#endif
                        var masterPort = ipEndPoint.Port;
                        var newMaster = $"{masterHost}:{masterPort}";

                        if (newMaster != lastMaster)
                        {
                            lastMaster = newMaster;
                            _connectionManager.UpdateConnection(masterHost, masterPort);
                            _logger.LogInformation($"✅ Redis master updated to {newMaster}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"❌ Sentinel returned unknown endpoint type: {master}");
                    }

                    break; // Stop checking after successful resolution
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"❌ Failed to query Sentinel {host}:{port}: {ex.Message}");
                    continue;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
        }
    }
}
