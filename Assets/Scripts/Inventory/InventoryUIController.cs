using UnityEngine;
using System.Collections.Generic;

public class InventoryUIController : MonoBehaviour
{
    public Transform contentParent;
    public GameObject slotPrefab;

    private ItemType currentPocket;

    public void OpenPocket(ItemType type)
    {
        currentPocket = type;
        RefreshUI();
    }

    void RefreshUI()
    {
        List<Transform> toRemove = new List<Transform>();
        foreach (Transform child in contentParent)
            toRemove.Add(child);

        foreach (Transform child in toRemove)
        {
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        List<InventorySlot> items = Inventory.Instance.GetPocket(currentPocket);

        foreach (var slot in items)
        {
            GameObject obj = Instantiate(slotPrefab, contentParent);
            obj.GetComponent<ItemSlotUI>().Setup(slot);
        }
    }
}