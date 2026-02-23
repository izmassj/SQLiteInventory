using System;
using UnityEngine;

/// <summary>
/// Datos iniciales (usuario demo para pruebas/entrega).
/// </summary>
public static class SeedData
{
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

    }
}
