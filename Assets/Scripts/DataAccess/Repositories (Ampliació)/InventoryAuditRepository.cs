using System;

public sealed class InventoryAuditRepository
{
    private readonly SqliteDatabase _db;

    public InventoryAuditRepository(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public void Add(int userId, int itemId, string action, int delta, string note = null)
    {
        _db.ExecuteNonQuery(
            "INSERT INTO inventory_log(user_id, item_id, action, quantity_change, created_at) VALUES (@u, @i, @a, @d, @t);",
            ("@u", userId),
            ("@i", itemId),
            ("@a", action),
            ("@d", delta),
            ("@t", DateTime.UtcNow.ToString("o"))
        );
    }
}
