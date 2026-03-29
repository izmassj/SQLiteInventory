using System;
using UnityEngine;

public sealed class ShopTransactionService
{
    private readonly SqliteDatabase _db;
    private readonly ItemsRepository _itemsRepository;
    private readonly InventoryRepository _inventoryRepository;
    private readonly InventoryAuditRepository _auditRepository;
    private readonly PlayerRepository _playerRepository;

    public ShopTransactionService(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _itemsRepository = new ItemsRepository(db);
        _inventoryRepository = new InventoryRepository(db);
        _auditRepository = new InventoryAuditRepository(db);
        _playerRepository = new PlayerRepository(db);
    }

    public (bool ok, string message) BuyItem(int userId, Item item, int amount, bool simulateDbError = false)
    {
        return ExecuteTransaction(
            userId,
            item,
            amount,
            true,
            simulateDbError,
            () => $"Comprat: {amount} x {item.displayName}."
        );
    }

    public (bool ok, string message) SellItem(int userId, Item item, int amount, bool simulateDbError = false)
    {
        return ExecuteTransaction(
            userId,
            item,
            amount,
            false,
            simulateDbError,
            () => $"Venut: {amount} x {item.displayName}."
        );
    }

    private (bool ok, string message) ExecuteTransaction(int userId, Item item, int amount, bool buying, bool simulateDbError, Func<string> successMessage)
    {
        if (userId <= 0)
            return (false, "No hi ha usuari.");

        if (item == null)
            return (false, "Objecte no valid.");

        if (amount <= 0)
            return (false, "Quantitat no valida.");

        try
        {
            _db.WithTransaction(_ =>
            {
                _playerRepository.EnsurePlayer(userId);

                int itemId = _itemsRepository.GetOrCreateItemId(
                    name: item.name,
                    type: item.type.ToString(),
                    description: item.description,
                    maxStack: Mathf.Max(1, item.maxStack)
                );

                int currentMoney = _playerRepository.GetMoney(userId);
                int currentQuantity = _inventoryRepository.GetQuantity(userId, itemId);
                int buyPrice = ShopPricing.GetBuyPrice(item);
                int sellPrice = ShopPricing.GetSellPrice(item);
                int maxStack = item.stackable ? Mathf.Max(1, item.maxStack) : 1;

                if (buying)
                {
                    int totalCost = buyPrice * amount;
                    if (currentMoney < totalCost)
                        throw new InvalidOperationException("No tens diners suficients.");

                    if (currentQuantity + amount > maxStack)
                        throw new InvalidOperationException("No hi cap mes quantitat.");

                    _playerRepository.SetMoney(userId, currentMoney - totalCost);

                    if (simulateDbError)
                        _db.ExecuteNonQuery("INSERT INTO inventory_missing_table(test) VALUES (1);");

                    _inventoryRepository.UpsertInventoryItem(userId, itemId, currentQuantity + amount);
                    _auditRepository.Add(userId, itemId, "shop_buy", amount);
                }
                else
                {
                    if (currentQuantity < amount)
                        throw new InvalidOperationException("No en tens prou.");

                    _playerRepository.SetMoney(userId, currentMoney + (sellPrice * amount));

                    if (simulateDbError)
                        _db.ExecuteNonQuery("INSERT INTO inventory_missing_table(test) VALUES (1);");

                    _inventoryRepository.UpsertInventoryItem(userId, itemId, currentQuantity - amount);
                    _auditRepository.Add(userId, itemId, "shop_sell", -amount);
                }
            });

            return (true, successMessage());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ShopTransactionService] {ex.Message}");
            return (false, ex.Message);
        }
    }
}
