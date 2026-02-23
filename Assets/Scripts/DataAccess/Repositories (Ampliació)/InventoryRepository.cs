using System;
using System.Collections.Generic;

public sealed class InventoryRepository
{
    private readonly SqliteDatabase _db;

    public InventoryRepository(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public List<(string itemName, int quantity)> GetInventoryByUser(int userId)
    {
        return _db.Query(
            @"SELECT i.name, inv.quantity
              FROM inventory inv
              JOIN items i ON i.id = inv.item_id
              WHERE inv.user_id = @u
              ORDER BY i.type ASC, i.name ASC;",
            r => (r.GetString(0), r.GetInt32(1)),
            ("@u", userId)
        );
    }

    public void UpsertInventoryItem(int userId, int itemId, int quantity)
    {
        if (quantity <= 0)
        {
            DeleteInventoryItem(userId, itemId);
            return;
        }

        _db.ExecuteNonQuery(@"
            INSERT OR REPLACE INTO inventory(user_id, item_id, quantity)
            VALUES (@u, @i, @q);",
            ("@u", userId),
            ("@i", itemId),
            ("@q", quantity)
        );
    }

    public void DeleteInventoryItem(int userId, int itemId)
    {
        _db.ExecuteNonQuery(
            "DELETE FROM inventory WHERE user_id = @u AND item_id = @i;",
            ("@u", userId),
            ("@i", itemId)
        );
    }

    public void ClearInventory(int userId)
    {
        _db.ExecuteNonQuery("DELETE FROM inventory WHERE user_id = @u;", ("@u", userId));
    }
}
