using System;
using System.Collections.Generic;
using UnityEngine;

public static class ItemCatalog
{
    private static Dictionary<string, Item> _byKey;
    private static Item[] _all;

    public static void Warmup()
    {
        if (_byKey != null)
            return;

        _all = Resources.LoadAll<Item>("");
        _byKey = new Dictionary<string, Item>(StringComparer.Ordinal);

        foreach (var item in _all)
        {
            if (item == null) continue;
            if (_byKey.ContainsKey(item.name))
            {
                continue;
            }
            _byKey[item.name] = item;
        }
    }

    public static IEnumerable<Item> All
    {
        get 
        { 
            Warmup(); 
            return _all; 
        }
    }

    public static Item Get(string itemKey)
    {
        Warmup();
        return (itemKey != null && _byKey.TryGetValue(itemKey, out var item)) ? item : null;
    }
}
