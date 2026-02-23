using System;
using System.Collections.Generic;

public sealed class ItemsRepository
{
    private readonly SqliteDatabase _db;
    private readonly Dictionary<string, int> _idCache = new Dictionary<string, int>(StringComparer.Ordinal);

    public ItemsRepository(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public int GetOrCreateItemId(string name, string type, string description, int maxStack)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name vacío", nameof(name));

        if (_idCache.TryGetValue(name, out int cached))
            return cached;

        string safeType = string.IsNullOrWhiteSpace(type) ? "Unknown" : type.Trim();
        int safeMaxStack = Math.Max(1, maxStack);

        _db.ExecuteNonQuery(@"
            INSERT OR IGNORE INTO items(name, type, description, max_stack)
            VALUES (@n, @t, @d, @m);",
            ("@n", name),
            ("@t", safeType),
            ("@d", description),
            ("@m", safeMaxStack)
        );

        _db.ExecuteNonQuery(@"
            UPDATE items SET
                type = @t,
                description = @d,
                max_stack = @m
            WHERE name = @n;",
            ("@n", name),
            ("@t", safeType),
            ("@d", description),
            ("@m", safeMaxStack)
        );

        int id = _db.ExecuteScalar<int>("SELECT id FROM items WHERE name = @n;", ("@n", name));
        _idCache[name] = id;
        return id;
    }

    public int? TryGetItemId(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_idCache.TryGetValue(name, out int cached))
            return cached;

        int id = _db.ExecuteScalar<int>("SELECT id FROM items WHERE name = @n;", ("@n", name));
        if (id <= 0)
            return null;

        _idCache[name] = id;
        return id;
    }

    public void WarmCache()
    {
        var rows = _db.Query(
            "SELECT id, name FROM items;",
            r => new { Id = r.GetInt32(0), Name = r.GetString(1) }
        );

        _idCache.Clear();
        foreach (var row in rows)
            _idCache[row.Name] = row.Id;
    }
}
