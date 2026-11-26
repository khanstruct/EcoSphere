using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        // One notice per limiting element; a repeat shortage restarts it instead of stacking copies.
        readonly Dictionary<ElementId, TMP_Text> shortageNotices = new Dictionary<ElementId, TMP_Text>();
        internal IReadOnlyDictionary<ElementId, TMP_Text> ShortageNotices => shortageNotices;
        Material shortageMaterial;

        void ShowShortage(ElementShortage shortage)
        {
            if (!resourcePanel || !visualizerStyle) return;
            if (shortageNotices.TryGetValue(shortage.limitedBy, out var existing) && existing) Destroy(existing.gameObject);
            var label = Label("Shortage notice", resourcePanel, new Vector2(.5f, 0), Vector2.zero, new Vector2(240, 28),
                string.Format(visualizerStyle.shortageFormat, Catalog.ElementName(shortage.limitedBy)), Mathf.RoundToInt(visualizerStyle.shortageFontSize));
            label.color = Catalog.ElementColor(shortage.limitedBy);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontSharedMaterial = ShortageMaterial(label);
            label.rectTransform.sizeDelta = new Vector2(label.preferredWidth + 8, label.rectTransform.sizeDelta.y);
            shortageNotices[shortage.limitedBy] = label;
            StartCoroutine(RiseShortage(label, shortage.element));
        }

        // A dark outline keeps element-colored text readable over bars and the dark frame. Shared by all notices.
        Material ShortageMaterial(TMP_Text label)
        {
            if (shortageMaterial) return shortageMaterial;
            shortageMaterial = new Material(label.fontSharedMaterial) { name = "Shortage notice outline" };
            shortageMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            shortageMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, visualizerStyle.shortageOutline);
            shortageMaterial.SetColor(ShaderUtilities.ID_OutlineColor, visualizerStyle.shortageOutlineColor);
            materials.Add(shortageMaterial);
            return shortageMaterial;
        }

        // Lowest the notice may sit this frame: above the capped bar and its value label (mid-frame if it has no bar).
        // Read every frame because bars ease toward their values and re-lay out when the screen changes.
        Vector2 ShortageAnchor(ElementId capped)
        {
            int bar = displayedElements.IndexOf(capped);
            if (bar < 0 || bar >= energyBars.Count || !energyBars[bar]) return new Vector2(0, barBaseline + maximumBarHeight * .5f);
            var barRect = energyBars[bar].rectTransform;
            return new Vector2(barRect.anchoredPosition.x, barRect.anchoredPosition.y + barRect.sizeDelta.y + visualizerStyle.shortageOffset);
        }
        // Highest it may rise: just under the star score strip.
        float ShortageCeiling => barBaseline + maximumBarHeight + 24;

        // Floats up from the capped element's bar while fading, kept inside the visualizer frame.
        IEnumerator RiseShortage(TMP_Text label, ElementId capped)
        {
            var rect = label.rectTransform;
            Color color = label.color;
            float duration = visualizerStyle.shortageDuration;
            for (float t = 0; t < duration && label; t += Time.deltaTime)
            {
                float p = t / duration, half = rect.sizeDelta.x * .5f;
                var position = ShortageAnchor(capped) + Vector2.up * visualizerStyle.shortageRise * Mathf.SmoothStep(0, 1, p);
                position.x = guideRight - guideLeft > half * 2 ? Mathf.Clamp(position.x, guideLeft + half, guideRight - half) : 0;
                position.y = Mathf.Min(position.y, ShortageCeiling);
                rect.anchoredPosition = position;
                color.a = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.6f, 1, p));
                label.color = color;
                yield return null;
            }
            if (label) Destroy(label.gameObject);
        }
    }
}
