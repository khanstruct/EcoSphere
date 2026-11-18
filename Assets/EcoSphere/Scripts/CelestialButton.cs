using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoSphere
{
    [RequireComponent(typeof(Button))]
    public sealed class CelestialButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Image glow, highlight;
        public TMP_Text label;
        public ButtonEdgeHighlight movingHighlight;
        public float phase;
        Button button;
        bool hovered, focused;
        void Awake() { button = GetComponent<Button>(); ButtonClickSound.Attach(button); }
        void Update()
        {
            bool active = button.IsInteractable();
            float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 1.25f + phase);
            float emphasis = hovered || focused ? .2f : 0;
            glow.color = new Color(1, 1, 1, active ? .22f + .16f * pulse + emphasis : 0);
            highlight.color = new Color(1, 1, 1, active ? .3f + .35f * pulse + emphasis : 0);
            if (movingHighlight)
            {
                highlight.enabled = false;
                movingHighlight.enabled = active;
                movingHighlight.phase = phase;
                movingHighlight.intensity = 1 + emphasis;
            }
            label.color = active ? new Color(1, .97f, .88f) : new Color(.48f, .5f, .55f);
        }
        public void OnPointerEnter(PointerEventData data) { hovered = true; }
        public void OnPointerExit(PointerEventData data) { hovered = false; }
        public void OnSelect(BaseEventData data) { focused = true; }
        public void OnDeselect(BaseEventData data) { focused = false; }
    }
}
