using UnityEngine;

public class ShopOpener : MonoBehaviour
{
    [SerializeField] private PlayerUIShopController _shopController;
    [SerializeField] private ShopData _shopData;
    [SerializeField] private DialogueData _storeWorkerDialogue;

    private void Start()
    {
        _storeWorkerDialogue.onDialogueEnd.AddListener(OpenShop);
    }

    public void OpenShop()
    {
        if (_shopController != null && _shopData != null)
            _shopController.OpenShop(_shopData);
    }
}