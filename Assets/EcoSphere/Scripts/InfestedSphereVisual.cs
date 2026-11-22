using UnityEngine;
namespace EcoSphere
{
    public sealed class InfestedSphereVisual : MonoBehaviour
    {
        public Camera view;
        public LineRenderer marker;
        public Color markerColor;
        Sprite firstFrame,secondFrame;
        SpriteRenderer spriteRenderer;
        float frameDuration,elapsed;
        bool animating;
        MaterialPropertyBlock frameProperties;
        public int CurrentFrame {get;private set;}
        public float FramesPerSecond {get;private set;}
        public void InitializeAnimation(InfestationModifierDefinition definition,System.Random random)
        {
            spriteRenderer=GetComponent<SpriteRenderer>();firstFrame=definition.alienSprite;secondFrame=definition.alienAlternateSprite;
            FramesPerSecond=definition.animationFramesPerSecond*Mathf.Lerp(1-definition.animationSpeedVariation,1+definition.animationSpeedVariation,(float)random.NextDouble());
            frameDuration=1/FramesPerSecond;
            CurrentFrame=random.Next(2);elapsed=(float)random.NextDouble()*frameDuration;
            ApplyFrame();animating=true;
        }
        void ApplyFrame()
        {
            var frame=CurrentFrame==0 ? firstFrame : secondFrame;spriteRenderer.sprite=frame;
            // Explicitly bind the current texture for the custom URP sprite material.
            if(frameProperties==null)frameProperties=new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(frameProperties);frameProperties.SetTexture("_MainTex",frame.texture);spriteRenderer.SetPropertyBlock(frameProperties);
        }
        public void StopAnimation()=>animating=false;
        internal void AdvanceAnimation(float deltaTime)
        {
            if(!animating)return;
            elapsed+=deltaTime;
            int steps=Mathf.FloorToInt(elapsed/frameDuration);
            if(steps<=0)return;
            elapsed-=steps*frameDuration;
            if((steps&1)!=0){CurrentFrame=1-CurrentFrame;ApplyFrame();}
        }
        void LateUpdate()
        {
            transform.rotation=view ? view.transform.rotation : Quaternion.identity;
            AdvanceAnimation(Time.deltaTime);
            if(marker)
            {
                var color=markerColor;color.a*=.78f+.22f*Mathf.Sin(Time.unscaledTime*3);
                marker.startColor=marker.endColor=color;
            }
        }
        public void ClearMarker(){if(marker){marker.gameObject.SetActive(false);Destroy(marker.gameObject);marker=null;}}
        void OnDestroy()=>ClearMarker();
    }
}
