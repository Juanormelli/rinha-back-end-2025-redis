using rinha_back_end_2025;
using rinha_back_end_2025.Endpoints;
using rinha_back_end_2025.Services;
using StackExchange.Redis;
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

services.AddScoped<IDatabase>(cfg => {
  var connection = ConnectionMultiplexer.Connect("redis:6379,abortConnect=false,connectTimeout=5000,syncTimeout=5000");
  return connection.GetDatabase();

});

services.AddSingleton<RedisManager>();


services.AddSingleton<Processor>();
services.AddSingleton<Repository>();

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
