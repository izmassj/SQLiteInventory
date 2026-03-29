using System;
using UnityEngine;

public static class SeedData
{
    private const int DEFAULT_START_MONEY = 3000;

    public static void EnsureSeed(SqliteDatabase db)
    {
        int count = db.ExecuteScalar<int>("SELECT COUNT(1) FROM users;");
        if (count > 0)
            return;

        const string defaultUser = "demo";
        const string defaultPass = "demo123";

        db.ExecuteNonQuery(
            "INSERT INTO users(username, password, created_at) VALUES (@u, @p, @c);",
            ("@u", defaultUser),
            ("@p", defaultPass),
            ("@c", DateTime.UtcNow.ToString("o"))
        );

        int userId = db.ExecuteScalar<int>("SELECT id FROM users WHERE username = @u;", ("@u", defaultUser));

        db.ExecuteNonQuery(
            "INSERT OR IGNORE INTO player(user_id, money, created_at, updated_at) VALUES (@u, @m, @c, @c);",
            ("@u", userId),
            ("@m", DEFAULT_START_MONEY),
            ("@c", DateTime.UtcNow.ToString("o"))
        );
    }
}
