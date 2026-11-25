using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    // Shows a sun type on a UI Image: the shared animated material tinted by the type's appearance,
    // or the type's static sprite when no animated material is configured.
    [RequireComponent(typeof(Image))]
    public sealed class SunView : MonoBehaviour
    {
        Material instance;
        public SunTypeDefinition Current { get; private set; }

        public void Show(SunTypeDefinition sun, Material template)
        {
            if (sun == null) return;
            Current = sun;
            var image = GetComponent<Image>();
            if (sun.sprite) image.sprite = sun.sprite;
            // Scaling keeps the layout's slot size; a scaled-up sun may overlap planets, so it never takes clicks.
            transform.localScale = Vector3.one * sun.displayScale;
            image.raycastTarget = false;
            if (!template) { image.material = null; return; }
            if (!instance || instance.shader != template.shader)
            {
                if (instance) Destroy(instance);
                instance = new Material(template) { name = template.name + " (" + sun.id + ")" };
            }
            sun.appearance.Apply(instance);
            image.material = instance;
        }

        void OnDestroy() { if (instance) Destroy(instance); }
    }
}
