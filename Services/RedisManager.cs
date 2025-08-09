using StackExchange.Redis;

namespace rinha_back_end_2025.Services;

public class RedisManager {


  private const string ListaChave = "payments";

  // Escreve um dado individualmente
  public async Task SetData (string valor) {
    var db = RedisConnection.Database;
    await db.ListLeftPushAsync(ListaChave, valor, flags: CommandFlags.FireAndForget);
  }

  // Lê todos os dados como lista
  public async Task<RedisValue[]> ReadData () {
    var db = RedisConnection.Database;
    return await db.ListRangeAsync(ListaChave, 0, -1);

  }

}
