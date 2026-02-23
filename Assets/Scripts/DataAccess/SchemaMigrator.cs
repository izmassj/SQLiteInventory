using UnityEngine;

/// <summary>
/// Migraciones simples usando PRAGMA user_version.
/// v2 = esquema simplificado (users/items/inventory/inventory_log).
/// </summary>
public static class SchemaMigrator
{
    private const int CURRENT_VERSION = 2;

    public static void ApplyMigrations(SqliteDatabase db)
    {
        db.ExecuteNonQuery("PRAGMA foreign_keys = ON;");

        int version = db.ExecuteScalar<int>("PRAGMA user_version;");

        if (version <= 0)
        {
            CreateSimplifiedSchema(db);
            db.ExecuteNonQuery($"PRAGMA user_version = {CURRENT_VERSION};");
            return;
        }
    }

    private static void CreateSimplifiedSchema(SqliteDatabase db)
    {
        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                password TEXT NOT NULL,
                created_at TEXT
            );");

        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                type TEXT NOT NULL,
                description TEXT,
                max_stack INTEGER DEFAULT 99
            );");

        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS inventory (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                item_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE,
                UNIQUE (user_id, item_id)
            );");

        db.ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS inventory_log (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                item_id INTEGER NOT NULL,
                action TEXT NOT NULL,
                quantity_change INTEGER NOT NULL,
                created_at TEXT,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE
            );");
    }
}
