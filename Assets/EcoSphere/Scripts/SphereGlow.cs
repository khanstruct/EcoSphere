using UnityEngine;

namespace EcoSphere
{
    // One shared 256px alpha halo for gameplay spheres and their UI preview stages.
    public static class SphereGlow
    {
        static Texture2D texture;
        static Sprite sprite;
        static Sprite Shared
        {
            get
            {
                if(sprite)return sprite;
                texture=new Texture2D(256,256,TextureFormat.RGBA32,false) { name="Shared sphere halo",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear };
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    float radius=new Vector2((x+.5f)/128-1,(y+.5f)/128-1).magnitude;
                    float distance=(radius-.78f)/.065f;
                    float alpha=Mathf.Exp(-distance*distance*1.5f)*Mathf.Clamp01((1-radius)*20);
                    texture.SetPixel(x,y,new Color(1,1,1,alpha));
                }
                texture.Apply(false,true);
                sprite=Sprite.Create(texture,new Rect(0,0,256,256),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
                sprite.hideFlags=HideFlags.HideAndDontSave; return sprite;
            }
        }
        public static Transform Create(Transform parent,SphereDefinition definition,float sphereDiameter,int layer)
        {
            if(!definition || definition.glowStrength<=0)return null;
            var glow=new GameObject("Sphere glow"); glow.layer=layer;
            glow.transform.SetParent(parent,false); glow.transform.localPosition=new Vector3(0,0,.03f);
            var renderer=glow.AddComponent<SpriteRenderer>(); renderer.sprite=Shared; renderer.sortingOrder=2;
            Color color=definition.glowColor; color.a*=definition.glowStrength; renderer.color=color;
            glow.transform.localScale=Vector3.one*sphereDiameter/(.78f*sprite.bounds.size.x);
            return glow.transform;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if(sprite)Object.Destroy(sprite); if(texture)Object.Destroy(texture);
            sprite=null; texture=null;
        }
    }
}
