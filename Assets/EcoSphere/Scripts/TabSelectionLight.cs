using UnityEngine;
using UnityEngine.UI;
namespace EcoSphere
{
    // A soft UI light built from vertex colors: no sprite texture or extra material required.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TabSelectionLight : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=GetPixelAdjustedRect();Vector2 center=rect.center;
            float radius=Mathf.Min(rect.width,rect.height)*.5f;
            const int segments=32;
            mesh.AddVert(center,new Color(1,1,.9f,1),Vector2.zero);
            for(int ring=0;ring<4;ring++)
            {
                float extent=ring==0 ? .16f : ring==1 ? .32f : ring==2 ? .6f : 1;
                Color tint=ring==0 ? new Color(1,.95f,.72f,1) : ring==1 ? new Color(1,.8f,.35f,.55f) :
                    ring==2 ? new Color(1,.67f,.2f,.16f) : new Color(1,.6f,.15f,0);
                for(int i=0;i<segments;i++)
                {
                    float angle=i*Mathf.PI*2/segments;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*extent,tint,Vector2.zero);
                }
            }
            for(int i=0;i<segments;i++)
            {
                int next=(i+1)%segments;mesh.AddTriangle(0,1+i,1+next);
                for(int ring=0;ring<3;ring++)
                {
                    int a=1+ring*segments+i,b=1+ring*segments+next,c=a+segments,d=b+segments;
                    mesh.AddTriangle(a,c,b);mesh.AddTriangle(b,c,d);
                }
            }
        }
    }
}
