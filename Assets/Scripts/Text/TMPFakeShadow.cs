using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMPFakeShadow : MonoBehaviour
{
    [SerializeField] Color shadowColor;
    [SerializeField] Vector2 offset;

    [SerializeField] int boldThickness = 2;
    [SerializeField] float boldOffset = 0.5f;

    TMP_Text original;
    TMP_Text shadow;

    void Awake()
    {
        SetupShadow();
        UpdateShadow();
    }

    void OnEnable()
    {
        SetupShadow();
        UpdateShadow();
    }

    void OnValidate()
    {
        UpdateShadow();
    }

    void SetupShadow()
    {
        original = GetComponent<TMP_Text>();

        Transform existing = transform.parent.Find(name + "_Shadow");
        if (existing != null)
        {
            shadow = existing.GetComponent<TMP_Text>();
            return;
        }

        GameObject shadowObj = new GameObject(name + "_Shadow");

        // mismo padre (no hijo)
        shadowObj.transform.SetParent(transform.parent);
        

        shadow = shadowObj.AddComponent<TextMeshProUGUI>();

        CopySettings();

        shadow.transform.SetSiblingIndex(transform.GetSiblingIndex());

        AddFakeBold();
    }

    void AddFakeBold()
    {
        TMPFakeBold bold = shadow.GetComponent<TMPFakeBold>();

        if (bold == null)
            bold = shadow.gameObject.AddComponent<TMPFakeBold>();

        bold.thickness = boldThickness;
        bold.offsetAmount = boldOffset;
    }

    void CopySettings()
    {
        if (shadow == null || original == null) return;

        shadow.font = original.font;
        shadow.fontSize = original.fontSize;
        shadow.alignment = original.alignment;
        shadow.enableWordWrapping = original.enableWordWrapping;
        shadow.text = original.text;
        shadow.raycastTarget = false;
        shadow.lineSpacing = original.lineSpacing;
        shadow.wordSpacing = original.wordSpacing;

        RectTransform o = original.rectTransform;
        RectTransform s = shadow.rectTransform;

        s.sizeDelta = o.sizeDelta;
        s.localScale = o.localScale;

        s.anchorMin = o.anchorMin;
        s.anchorMax = o.anchorMax;
        s.pivot = o.pivot;
        s.anchoredPosition = o.anchoredPosition;
    }


    void UpdateShadow()
    {
        if (shadow == null || original == null) return;

        shadow.text = original.text;
        shadow.color = shadowColor;
        shadow.rectTransform.anchoredPosition =
            original.rectTransform.anchoredPosition + offset;
    }

    void LateUpdate()
    {
        if (shadow == null || original == null) return;

        shadow.transform.SetSiblingIndex(0);
        shadow.text = original.text;
    }
}
