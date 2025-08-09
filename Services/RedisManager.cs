using StackExchange.Redis;

namespace rinha_back_end_2025.Services;

public class RedisManager {

  private readonly IDatabase _database;
  public RedisManager (IDatabase database) {
    _database = database;
  }

  public async Task<bool> SetPaymentAsync (string key, RedisValue value) {
    return await _database.SetAddAsync(key, value);
  }
  public async Task<RedisValue[]?> GetPaymentAsync (string key) {
    return await _database.SetMembersAsync(key);
  }

}
