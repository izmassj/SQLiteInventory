using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text quantityText;

    private InventorySlot slot;

    public void Setup(InventorySlot slot)
    {
        this.slot = slot;

        nameText.text = slot.item.displayName;

        quantityText.text = slot.item.stackable 
            ? slot.quantity.ToString() 
            : "";
    }

    public InventorySlot GetSlot()
    {
        return slot;
    }
}