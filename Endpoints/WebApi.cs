using Microsoft.AspNetCore.Mvc;
using rinha_back_end_2025.Model;
using rinha_back_end_2025.Services;
using rinha_back_end_2025.SourceGeneration;
using StackExchange.Redis;
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

    app.MapGet("/payments-summary", async ([FromQuery] string? from, [FromQuery] string? to) => {
      DateTime? fromDate = string.IsNullOrEmpty(from) ? DateTime.MinValue : DateTime.Parse(from, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
      DateTime? toDate = string.IsNullOrEmpty(to) ? DateTime.MaxValue : DateTime.Parse(to, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
      try {
        var redisValues = await RedisConnection.Database.ListRangeAsync("payments");
        var stream = DeserializePayments(redisValues, options, fromDate, toDate);
        return Results.File(stream.ToArray(), "application/json");
      } catch {
        var summaryDefault = new PaymentSummaryModel();
        var summaryFallback = new PaymentSummaryModel();
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
      }


    });

  }
  public static MemoryStream DeserializePayments (IReadOnlyList<RedisValue> values, JsonSerializerOptions options, DateTime? from, DateTime? to) {
    if (values == null || values.Count == 0)
      return null;

    var summaryDefault = new PaymentSummaryModel();
    var summaryFallback = new PaymentSummaryModel();

    var list = new List<PaymentModel>(values.Count); // capacidade já definida

    for (int i = 0; i < values.Count; i++) {
      var val = (byte[]?)values[i];
      if (val == null)
        continue;

      try {
        var model = JsonSerializer.Deserialize<PaymentModel>((ReadOnlySpan<byte>)val, options);
        var requestedAt = model.RequestedAt;
        if (requestedAt >= from && requestedAt <= to) {
          summaryDefault.AddRequest(model);
        }
      } catch {
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

    return stream;
  }
}
