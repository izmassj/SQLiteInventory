using UnityEngine;
using UnityEditor;

[CreateAssetMenu(menuName = "Scriptable Object/Item")]
public class Item : ScriptableObject
{
    [Header("Gameplay")]
    public ItemType type;
    public string displayName;
    public string description;

    [Header("UI")]
    public Sprite sprite;

    [Header("Parameters")]
    public bool stackable;
    public int maxStack;

    [Header("Shop")]
    public int buyPrice = 100;
    public int sellPrice = 50;

    [SerializeField, HideInInspector]
    private string typeSearch;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        string label = type.ToString();

        var labels = AssetDatabase.GetLabels(this);
        AssetDatabase.SetLabels(this, new[] { label });
    }
    #endif
}
