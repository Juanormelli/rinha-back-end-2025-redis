using Microsoft.AspNetCore.Mvc;
using rinha_back_end_2025.Model;
using rinha_back_end_2025.Services;
using rinha_back_end_2025.SourceGeneration;
using System.Text.Json;

namespace rinha_back_end_2025.Endpoints;

public static class WebApi {
  public static JsonSerializerOptions options = new JsonSerializerOptions()
  {
    TypeInfoResolver = PaymentsSerializerContext.Default
  };

  public static void RegisterEndpoints (this WebApplication app) {
    app.MapPost("/payments", ([FromBody] PaymentModel model, [FromServices] Processor processor) => {
      Results.Ok();
      processor.paymentQueue.OnNext(model);

    });

    app.MapGet("/payments-summary", async ([FromQuery] string? from, [FromQuery] string? to, [FromServices] RedisManager manager) => {
      DateTime? fromDate = string.IsNullOrEmpty(from) ? DateTime.MinValue : DateTime.Parse(from, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
      DateTime? toDate = string.IsNullOrEmpty(to) ? DateTime.MaxValue : DateTime.Parse(to, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

      var summaryDefault = new PaymentSummaryModel();
      var summaryFallback = new PaymentSummaryModel();

      var abc = await manager.ReadData();
      var payments = abc?.Select(x => JsonSerializer.Deserialize<PaymentModel>(x, options)).Where(x => x != null).ToList() ?? new List<PaymentModel>();

      foreach (var payment in payments) {
        var requestedAt = payment.RequestedAt;
        if (requestedAt >= fromDate && requestedAt <= toDate) {
          summaryDefault.AddRequest(payment);
        }
      }
      using var stream = new MemoryStream();
      using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

      writer.WriteStartObject();

      writer.WritePropertyName("default");
      summaryDefault.WriteTo(writer);

      writer.WritePropertyName("fallback");
      summaryFallback.WriteTo(writer);

      writer.WriteEndObject();
      writer.Flush();

      return Results.File(stream.ToArray(), "application/json");

    });

  }
}
