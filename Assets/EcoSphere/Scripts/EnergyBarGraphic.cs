using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere
{
    // Vertex data lets all bars share one material while keeping their own tint and state.
    [ExecuteAlways]
    [AddComponentMenu("EcoSphere/Energy Bar")]
    public sealed class EnergyBarGraphic : MaskableGraphic
    {
        [SerializeField] bool leading, selfCombination;
        [SerializeField, Range(0,1)] float phase;
        public bool Leading { get => leading; set { leading=value; SetVerticesDirty(); } }
        public bool SelfCombination { get => selfCombination; set { selfCombination=value; SetVerticesDirty(); } }
        public float Phase { get => phase; set { phase=value; SetVerticesDirty(); } }
        // Negative means gameplay time. A fixed value supports deterministic previews.
        public float PreviewTime { get; set; } = -1;
        protected override void OnEnable()
        {
            base.OnEnable();
            ConfigureCanvas();
            raycastTarget=false;
        }
        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            ConfigureCanvas();
        }
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged(); ConfigureCanvas();
        }
        void ConfigureCanvas()
        {
            var owner=GetComponentInParent<Canvas>();
            if (owner) owner.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
        }
        void Update() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            ConfigureCanvas();
            vh.Clear();
            Rect rect=rectTransform.rect;
            if (rect.width<=0 || rect.height<=0) return;
            const float padding=12;
            for (int i=0;i<4;i++)
            {
                bool right=i==2 || i==3, top=i==1 || i==2;
                var vertex=UIVertex.simpleVert;
                vertex.position=new Vector3(right ? rect.xMax+padding : rect.xMin-padding,top ? rect.yMax+padding : rect.yMin-padding);
                vertex.uv0=new Vector2(right ? 1+padding/rect.width : -padding/rect.width,top ? 1+padding/rect.height : -padding/rect.height);
                vertex.uv1=new Vector4(rect.width,rect.height,PreviewTime>=0 ? PreviewTime : Time.time,phase);
                vertex.uv2=new Vector4(leading ? 1 : 0,leading && selfCombination ? 1 : 0,0,0);
                vertex.color=color; vh.AddVert(vertex);
            }
            vh.AddTriangle(0,1,2); vh.AddTriangle(2,3,0);
        }
    }
}
