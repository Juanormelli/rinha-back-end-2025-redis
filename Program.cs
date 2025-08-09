using rinha_back_end_2025;
using rinha_back_end_2025.Endpoints;
using rinha_back_end_2025.Services;
using System.Runtime;

var builder = WebApplication.CreateSlimBuilder(args);

var udsPath = "/tmp/other-api.sock";
if (System.IO.File.Exists(udsPath))
  System.IO.File.Delete(udsPath);

// Configura��es de serializa��o otimizadas
builder.Services.ConfigureHttpJsonOptions(options => {
  options.SerializerOptions.TypeInfoResolver = rinha_back_end_2025.SourceGeneration.PaymentsSerializerContext.Default;
  options.SerializerOptions.IncludeFields = false;
  options.SerializerOptions.DefaultBufferSize = 256; // Buffer menor para respostas pequenas
});

// Configura��es Kestrel para alta performance
builder.WebHost.ConfigureKestrel(serverOptions => {
  serverOptions.AddServerHeader = false;
  serverOptions.Limits.MaxConcurrentConnections = 100_000;
  serverOptions.Limits.MaxConcurrentUpgradedConnections = 100_000;
  serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
  serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
  serverOptions.ListenUnixSocket(udsPath);
});
builder.Logging.ClearProviders();

GCSettings.LatencyMode = GCLatencyMode.LowLatency;
var services = builder.Services;

services.AddSingleton<Processor>();
services.AddSingleton<Repository>();

// HttpClient otimizado
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

// Minimiza middlewares para lat�ncia m�nima
var app = builder.Build();

app.UseRouting(); // Apenas roteamento, sem middlewares extras

app.RegisterEndpoints();
app.Run();
