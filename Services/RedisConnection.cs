using StackExchange.Redis;

namespace rinha_back_end_2025.Services;

public static class RedisConnection {
  private static readonly Lazy<ConnectionMultiplexer> _lazyConnection =
      new Lazy<ConnectionMultiplexer>(() => {
        var config = new ConfigurationOptions
        {
          EndPoints = { Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost:6379" },
          AbortOnConnectFail = false,
          ConnectRetry = 5,
          ConnectTimeout = 200,
          SyncTimeout = 200,
          KeepAlive = 60,
          AllowAdmin = false,
          DefaultDatabase = 0,
          SocketManager = new SocketManager()
        };
        return ConnectionMultiplexer.Connect(config);
      });

  public static ConnectionMultiplexer Connection => _lazyConnection.Value;
  public static IDatabase Database => Connection.GetDatabase();
}