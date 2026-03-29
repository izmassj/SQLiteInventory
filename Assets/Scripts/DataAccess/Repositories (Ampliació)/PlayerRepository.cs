using System;

public sealed class PlayerRepository
{
    public const int DefaultStartMoney = 3000;

    private readonly SqliteDatabase _db;

    public PlayerRepository(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public void EnsurePlayer(int userId, int defaultMoney = DefaultStartMoney)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        _db.ExecuteNonQuery(@"
            INSERT OR IGNORE INTO player(user_id, money, created_at, updated_at)
            VALUES (@u, @m, @c, @c);",
            ("@u", userId),
            ("@m", Math.Max(0, defaultMoney)),
            ("@c", DateTime.UtcNow.ToString("o"))
        );
    }

    public int GetMoney(int userId)
    {
        EnsurePlayer(userId);
        return _db.ExecuteScalar<int>("SELECT money FROM player WHERE user_id = @u;", ("@u", userId));
    }

    public void SetMoney(int userId, int money)
    {
        EnsurePlayer(userId);
        _db.ExecuteNonQuery(
            "UPDATE player SET money = @m, updated_at = @t WHERE user_id = @u;",
            ("@u", userId),
            ("@m", Math.Max(0, money)),
            ("@t", DateTime.UtcNow.ToString("o"))
        );
    }
}
