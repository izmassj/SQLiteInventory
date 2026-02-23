using UnityEngine;
using TMPro;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMPFakeBold : MonoBehaviour
{
    [Range(1, 8)]
    public int thickness = 2;

    public float offsetAmount = 0.5f;

    TMP_Text original;
    List<TMP_Text> copies = new List<TMP_Text>();

    void OnEnable()
    {
        original = GetComponent<TMP_Text>();
        CreateCopies();
        UpdateCopies();
    }

    void OnValidate()
    {
        UpdateCopies();
    }

    void CreateCopies()
    {
        // delete old copies
        foreach (var c in copies)
            if (c) DestroyImmediate(c.gameObject);

        copies.Clear();

        for (int i = 0; i < thickness; i++)
        {
            GameObject obj = new GameObject(name + "_BoldCopy");

            obj.transform.SetParent(transform);
            obj.transform.localScale = Vector3.one;
            obj.transform.localRotation = Quaternion.identity;

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null)
                rt = obj.AddComponent<RectTransform>();

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            TMP_Text copy = obj.AddComponent<TextMeshProUGUI>();

            copy.font = original.font;
            copy.fontSize = original.fontSize;
            copy.alignment = original.alignment;
            copy.raycastTarget = false;
            copy.text = original.text;
            copy.color = original.color;
            copy.lineSpacing = original.lineSpacing;
            copy.wordSpacing = original.wordSpacing;


            copies.Add(copy);
        }
    }

    void UpdateCopies()
    {
        if (original == null) return;

        Vector2[] directions = {
            new Vector2(1,0),
            new Vector2(-1,0),
            new Vector2(0,1),
            new Vector2(0,-1),
            new Vector2(1,1),
            new Vector2(-1,1),
            new Vector2(1,-1),
            new Vector2(-1,-1),
        };

        for (int i = 0; i < copies.Count; i++)
        {
            copies[i].text = original.text;
            copies[i].color = original.color;

            Vector2 offset = directions[i % directions.Length] * offsetAmount;

            copies[i].rectTransform.localPosition = offset;
        }
    }

    void LateUpdate()
    {
        UpdateCopies();
    }

    private void SetMiddleCenterLikeAlt(RectTransform rt)
    {
        RectTransform parent = rt.parent as RectTransform;
        if (parent == null) return;

        Vector2 parentSize = parent.rect.size;

        Vector2 absolutePosition = rt.anchoredPosition + new Vector2(parentSize.x * (rt.anchorMin.x + rt.anchorMax.x - 1f) * 0.5f, parentSize.y * (rt.anchorMin.y + rt.anchorMax.y - 1f) * 0.5f);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = absolutePosition;
    }
}
