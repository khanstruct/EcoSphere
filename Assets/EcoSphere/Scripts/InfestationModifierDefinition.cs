using UnityEngine;
namespace EcoSphere
{
    [CreateAssetMenu(menuName="EcoSphere/Infestation Modifier")]
    public sealed class InfestationModifierDefinition : WorldModifierDefinition
    {
        [Min(1)] public int minimumInfested=8,maximumInfested=12;
        public Sprite alienSprite;
        public Sprite alienAlternateSprite;
        [Min(.1f)] public float animationFramesPerSecond=2f;
        [Range(0,.4f)] public float animationSpeedVariation=.15f;
        public Material overlayMaterial;
        public AudioClip hitAudio;
        [Range(1,1.6f)] public float alienScale=1.25f;
        [Tooltip("Optional host rim; leave disabled when the creature artwork provides sufficient contrast.")]
        public bool showHostMarker;
        public override void Validate()
        {
            base.Validate();
            if(minimumInfested<1 || maximumInfested<minimumInfested || alienScale<1 || alienScale>1.6f || !alienSprite || !alienAlternateSprite ||
                float.IsNaN(animationFramesPerSecond) || float.IsInfinity(animationFramesPerSecond) || animationFramesPerSecond<.1f ||
                float.IsNaN(animationSpeedVariation) || animationSpeedVariation<0 || animationSpeedVariation>.4f || !overlayMaterial || !hitAudio)
                throw new System.InvalidOperationException("Invalid infestation configuration: "+name);
            if(alienSprite.rect.size!=alienAlternateSprite.rect.size || alienSprite.pivot!=alienAlternateSprite.pivot || alienSprite.pixelsPerUnit!=alienAlternateSprite.pixelsPerUnit)
                throw new System.InvalidOperationException("Alien animation frames must share canvas, pivot and pixels per unit");
        }
    }
}
