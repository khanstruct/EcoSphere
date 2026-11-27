using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // One notice per limiting element; a repeat shortage restarts it instead of stacking copies.
        // Different limiting elements at once stack upward, in the order they appeared.
        readonly Dictionary<ElementId, TMP_Text> shortageNotices = new Dictionary<ElementId, TMP_Text>();
        readonly List<TMP_Text> shortageOrder = new List<TMP_Text>();
        internal IReadOnlyDictionary<ElementId, TMP_Text> ShortageNotices => shortageNotices;
        Material shortageMaterial;

        void ShowShortage(ElementShortage shortage)
        {
            if (!resourcePanel || !visualizerStyle) return;
            if (shortageNotices.TryGetValue(shortage.limitedBy, out var existing) && existing) { shortageOrder.Remove(existing); Destroy(existing.gameObject); }
            var label = Label("Shortage notice", resourcePanel, new Vector2(.5f, 1), Vector2.zero, Vector2.zero,
                string.Format(visualizerStyle.shortageFormat, Catalog.ElementName(shortage.limitedBy)), Mathf.RoundToInt(visualizerStyle.shortageFontSize));
            label.color = Catalog.ElementColor(shortage.limitedBy);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true; label.fontSizeMin = 14; label.fontSizeMax = visualizerStyle.shortageFontSize;
            label.fontSharedMaterial = ShortageMaterial(label);
            label.rectTransform.pivot = new Vector2(.5f, 0);
            // Its own sorting layer keeps it above the HUD text that shares the space above the visualizer.
            var layer = label.gameObject.AddComponent<Canvas>(); layer.overrideSorting = true;
            layer.sortingOrder = resourcePanel.GetComponentInParent<Canvas>().sortingOrder + 10;
            shortageNotices[shortage.limitedBy] = label; shortageOrder.Add(label);
            StartCoroutine(RiseShortage(label));
        }

        // A thick dark outline plus a soft drop shadow keep element-colored text readable over any background. Shared by all notices.
        // Prefers the saved style material (so builds include its shader variant); a runtime copy is the fallback for another font.
        Material ShortageMaterial(TMP_Text label)
        {
            if (shortageMaterial) return shortageMaterial;
            var saved = visualizerStyle.shortageMaterial;
            if (saved && label.font && saved.mainTexture == label.font.atlasTexture) return shortageMaterial = saved;
            shortageMaterial = new Material(label.fontSharedMaterial) { name = "Shortage notice outline" };
            visualizerStyle.ApplyShortageLook(shortageMaterial);
            materials.Add(shortageMaterial);
            return shortageMaterial;
        }

        // Draws a practically invisible notice for a moment at level start, so the first real "Not enough" message
        // never waits on its shader (the editor shows a cyan placeholder while a shader variant compiles).
        internal bool ShortagePrewarmed { get; private set; }
        IEnumerator PrewarmShortageNotice()
        {
            if (!resourcePanel || !visualizerStyle) yield break;
            var label = Label("Shortage notice prewarm", resourcePanel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(200, 40), "Not enough", 24);
            label.fontSharedMaterial = ShortageMaterial(label); label.color = new Color(1, 1, 1, .004f);
            for (int frame = 0; frame < 3; frame++) yield return null;
            Destroy(label.gameObject); ShortagePrewarmed = true;
        }

        // Rises from just above the visualizer's top edge while fading; spans the visualizer's full width.
        // Size and position are read every frame so the notice follows responsive relayouts.
        IEnumerator RiseShortage(TMP_Text label)
        {
            var rect = label.rectTransform;
            Color color = label.color;
            float duration = visualizerStyle.shortageDuration;
            for (float t = 0; t < duration && label; t += Time.deltaTime)
            {
                float p = t / duration;
                int slot = Mathf.Max(0, shortageOrder.IndexOf(label));
                rect.sizeDelta = new Vector2(resourcePanel.rect.width, visualizerStyle.shortageHeight);
                rect.anchoredPosition = new Vector2(0, visualizerStyle.shortageOffset + slot * visualizerStyle.shortageHeight
                    + visualizerStyle.shortageRise * Mathf.SmoothStep(0, 1, p));
                color.a = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.6f, 1, p));
                label.color = color;
                yield return null;
            }
            if (label) { shortageOrder.Remove(label); Destroy(label.gameObject); }
        }
    }
}
