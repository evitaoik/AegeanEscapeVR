using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Hover / press feedback for the menu buttons (colour fade + small grow).
// Works with the XR ray, the poke interactor and the mouse.
[RequireComponent(typeof(Button))]
public class UIButtonStyle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image fill;
    public Image outline;
    public TMP_Text label;

    [Header("Colours")]
    public Color normalFill = Color.white;
    public Color hoverFill = Color.white;
    public Color pressedFill = Color.white;
    public Color normalText = Color.black;
    public Color hoverText = Color.black;
    public Color normalOutline = Color.white;
    public Color hoverOutline = Color.white;

    [Header("Motion")]
    public float hoverScale = 1.04f;
    public float speed = 14f;

    private bool hovered;
    private bool pressed;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
    }

    private void OnEnable()
    {
        hovered = false;
        pressed = false;
        ApplyInstant();
    }

    private void Update()
    {
        float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        bool active = button.interactable && hovered;

        Color targetFill = pressed ? pressedFill : active ? hoverFill : normalFill;
        float targetScale = pressed ? 0.98f : active ? hoverScale : 1f;

        if (fill != null)
            fill.color = Color.Lerp(fill.color, targetFill, t);

        if (outline != null)
            outline.color = Color.Lerp(outline.color, active ? hoverOutline : normalOutline, t);

        if (label != null)
            label.color = Color.Lerp(label.color, active ? hoverText : normalText, t);

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, t);
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData) => pressed = true;

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;

        // Stops the button staying "selected" after it was clicked
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void ApplyInstant()
    {
        if (fill != null)
            fill.color = normalFill;

        if (outline != null)
            outline.color = normalOutline;

        if (label != null)
            label.color = normalText;

        transform.localScale = Vector3.one;
    }
}
