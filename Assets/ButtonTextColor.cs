using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text text;         // <-- This should appear as "Text" in the Inspector
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;

    void Start()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (text != null)
            text.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (text != null)
            text.color = normalColor;
    }
}
