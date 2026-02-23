using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Shop")]
public class ShopData : ScriptableObject
{
    [Header("Shop Items")]
    public List<Item> items = new List<Item>();
}