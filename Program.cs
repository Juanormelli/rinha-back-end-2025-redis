using rinha_back_end_2025;
using rinha_back_end_2025.Endpoints;
using rinha_back_end_2025.Services;
using System.Runtime;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => {
  options.SerializerOptions.TypeInfoResolver = rinha_back_end_2025.SourceGeneration.PaymentsSerializerContext.Default;
  options.SerializerOptions.IncludeFields = false;
  options.SerializerOptions.DefaultBufferSize = 256;
});

builder.WebHost.ConfigureKestrel(serverOptions => {
  serverOptions.AddServerHeader = false;
  serverOptions.Limits.MaxConcurrentConnections = 100_000;
  serverOptions.Limits.MaxConcurrentUpgradedConnections = 100_000;
  serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
  serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
});

builder.Logging.ClearProviders();

GCSettings.LatencyMode = GCLatencyMode.LowLatency;

var services = builder.Services;

_ = RedisConnection.Connection;

services.AddSingleton<Processor>();
services.AddSingleton<Repository>();
ThreadPool.SetMinThreads(workerThreads: 1024, completionPortThreads: 512);
services.AddHttpClient("default", c => {
  c.BaseAddress = new System.Uri("http://payment-processor-default:8080");

})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
  PooledConnectionLifetime = TimeSpan.FromMinutes(15),
  PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
  MaxConnectionsPerServer = 10_000,
  EnableMultipleHttp2Connections = true,
  UseCookies = false
});

services.AddHttpClient("fallback", c => {
  c.BaseAddress = new System.Uri("http://payment-processor-fallback:8080");
  c.Timeout = TimeSpan.FromMilliseconds(500);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
  PooledConnectionLifetime = TimeSpan.FromMinutes(15),
  PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
  MaxConnectionsPerServer = 50000,
  EnableMultipleHttp2Connections = true,
  UseCookies = false
});


var app = builder.Build();

app.RegisterEndpoints();

app.Run();
