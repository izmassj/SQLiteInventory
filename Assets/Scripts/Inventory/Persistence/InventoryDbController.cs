using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class InventoryDbController : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField] private string _loginSceneName = "Login";

    private ItemsRepository _itemsRepo;
    private InventoryRepository _invRepo;
    private InventoryAuditRepository _auditRepo;

    private bool _ready;

    private void Awake()
    {
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("[InventoryDb] DatabaseManager.Instance no existe.");
            return;
        }

        var db = DatabaseManager.Instance.Db;
        _itemsRepo = new ItemsRepository(db);
        _invRepo = new InventoryRepository(db);
        _auditRepo = new InventoryAuditRepository(db);
    }

    private IEnumerator Start()
    {
        if (_itemsRepo == null || _invRepo == null)
            yield break;

        if (SessionManager.Instance == null || !SessionManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[InventoryDb] No hay usuario logueado. Volviendo a Login.");
            if (!string.IsNullOrWhiteSpace(_loginSceneName))
                SceneManager.LoadScene(_loginSceneName);
            yield break;
        }

        SyncItemsFromResources();

        float timeoutAt = Time.unscaledTime + 5f;
        while (Inventory.Instance == null && Time.unscaledTime < timeoutAt)
            yield return null;

        if (Inventory.Instance == null)
        {
            Debug.LogError("[InventoryDb] Inventory.Instance no existe en la escena (timeout esperando inicialización).");
            yield break;
        }

        LoadInventoryFromDb();

        Inventory.Instance.ItemQuantityChanged += OnInventoryQuantityChanged;

        _ready = true;
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.ItemQuantityChanged -= OnInventoryQuantityChanged;
    }

    private void SyncItemsFromResources()
    {
        ItemCatalog.Warmup();

        foreach (var item in ItemCatalog.All)
        {
            if (item == null) continue;

            _itemsRepo.GetOrCreateItemId(
                name: item.name,                        
                type: item.type.ToString(),            
                description: item.description,
                maxStack: Mathf.Max(1, item.maxStack)
            );
        }

        _itemsRepo.WarmCache();
    }

    private void LoadInventoryFromDb()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogError("[InventoryDb] Inventory.Instance no existe en la escena.");
            return;
        }

        int userId = SessionManager.Instance.UserId;
        var rows = _invRepo.GetInventoryByUser(userId);

        Inventory.Instance.BeginBulkUpdate();
        try
        {
            Inventory.Instance.ClearInventory();

            foreach (var row in rows)
            {
                var item = ItemCatalog.Get(row.itemName);
                if (item == null)
                {
                    Debug.LogWarning($"[InventoryDb] Item asset no encontrado para item name: {row.itemName}");
                    continue;
                }

                Inventory.Instance.SetItemQuantity(item, row.quantity);
            }
        }
        finally
        {
            Inventory.Instance.EndBulkUpdate();
        }

        Debug.Log($"[InventoryDb] Inventario cargado para user {userId}: {rows.Count} items.");
    }

    private void OnInventoryQuantityChanged(Item item, int oldQty, int newQty)
    {
        if (!_ready) return;
        if (SessionManager.Instance == null || !SessionManager.Instance.IsLoggedIn) return;
        if (item == null) return;

        int userId = SessionManager.Instance.UserId;
        int delta = newQty - oldQty;

        int itemId = _itemsRepo.GetOrCreateItemId(
            name: item.name,
            type: item.type.ToString(),
            description: item.description,
            maxStack: Mathf.Max(1, item.maxStack)
        );

        _invRepo.UpsertInventoryItem(userId, itemId, newQty);

        string action;
        if (oldQty == 0 && newQty > 0) action = "add";
        else if (newQty == 0 && oldQty > 0) action = "remove";
        else action = "change";

        _auditRepo?.Add(userId, itemId, action, delta);
    }
}
