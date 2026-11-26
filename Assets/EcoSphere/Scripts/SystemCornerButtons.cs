using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    // Builds the System screen's corner icon buttons from SystemMenuSettings.
    // Destinations aren't built yet: Selected reports the entry id so a screen can be attached later.
    public sealed class SystemCornerButtons : MonoBehaviour
    {
        readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        public event Action<string> Selected;
        public IReadOnlyDictionary<string, Button> Buttons => buttons;

        public static SystemCornerButtons Create(Transform canvasRoot)
        {
            var settings = SystemMenuSettings.Current; settings.Validate();
            var root = new GameObject("Corner Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasRoot, false);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            var menu = root.gameObject.AddComponent<SystemCornerButtons>();
            var perCorner = new Dictionary<ScreenCorner, int>();
            foreach (var entry in settings.entries)
            {
                perCorner.TryGetValue(entry.corner, out int slot); perCorner[entry.corner] = slot + 1;
                menu.buttons[entry.id] = menu.Build(entry, slot, settings);
            }
            return menu;
        }

        Button Build(SystemMenuEntry entry, int slot, SystemMenuSettings settings)
        {
            var rect = new GameObject(entry.label + " Button", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            bool right = entry.corner == ScreenCorner.TopRight || entry.corner == ScreenCorner.BottomRight;
            bool top = entry.corner == ScreenCorner.TopLeft || entry.corner == ScreenCorner.TopRight;
            var anchor = new Vector2(right ? 1 : 0, top ? 1 : 0);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = Vector2.one * settings.buttonSize;
            float inward = settings.margin.x + slot * (settings.buttonSize + settings.spacing);
            rect.anchoredPosition = new Vector2(right ? -inward : inward, top ? -settings.margin.y : settings.margin.y);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = entry.icon; image.preserveAspect = true;
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = settings.normalColor; colors.highlightedColor = colors.selectedColor = settings.highlightedColor;
            colors.pressedColor = settings.pressedColor; button.colors = colors;
            ButtonClickSound.Attach(button);
            string id = entry.id;
            button.onClick.AddListener(() => Selected?.Invoke(id));
            return button;
        }
    }
}
