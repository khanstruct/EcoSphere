using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSphere.Editor
{
    public static class EnergyBarTools
    {
        const string Folder="Assets/EcoSphere/Art/UI/Visualizer/";
        public const string MaterialPath=Folder+"energy-bar-v1.mat";
        [MenuItem("EcoSphere/Build and Validate Energy Bar Material")]
        public static void BuildAndValidate()
        {
            var shader=Shader.Find("EcoSphere/UI/Energy Bar");
            if (!shader || !shader.isSupported) throw new Exception("Energy bar shader missing or unsupported");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"energy-grayscale-v1.png");
            if (!texture || texture.width>256 || texture.height>256) throw new Exception("Energy texture missing or exceeds 256px");
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material) { material=new Material(shader); AssetDatabase.CreateAsset(material,MaterialPath); }
            material.shader=shader; material.SetTexture("_EnergyTex",texture);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            const string stylePath="Assets/EcoSphere/Resources/VisualizerStyle.asset";
            var style=AssetDatabase.LoadAssetAtPath<VisualizerStyle>(stylePath);
            if(!style) { style=ScriptableObject.CreateInstance<VisualizerStyle>(); AssetDatabase.CreateAsset(style,stylePath); }
            style.energyMaterial=material;
            style.frame=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"visualizer-frame-v1.png");
            EditorUtility.SetDirty(style); AssetDatabase.SaveAssets();
            ValidateRender(material);
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    throw new Exception("Energy shader: "+message.message);
            Debug.Log("ECOSPHERE_ENERGY_SHADER_PASSED: shared material, tint, animated surface, leader/self variants, stencil and rectangular masking, 256px texture");
        }

        static void ValidateRender(Material material)
        {
            var root=new GameObject("Temporary energy shader preview");
            var target=new RenderTexture(800,400,24);
            Texture2D first=null,second=null;
            try
            {
                var camera=new GameObject("Preview camera",typeof(Camera)).GetComponent<Camera>();
                camera.transform.SetParent(root.transform); camera.transform.position=new Vector3(0,0,-10);
                camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear;
                camera.cullingMask=1<<29; camera.targetTexture=target;
                var canvas=new GameObject("Preview canvas",typeof(Canvas)).GetComponent<Canvas>();
                canvas.transform.SetParent(root.transform); canvas.gameObject.layer=29;
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                var normal=Bar(canvas.transform,material,-280,new Color(1,.22f,.025f),false,false);
                var leader=Bar(canvas.transform,material,-140,new Color(.05f,.8f,1),true,false);
                var self=Bar(canvas.transform,material,0,new Color(1,.3f,.02f),true,true);
                var rectMask=new GameObject("Rect mask",typeof(RectTransform),typeof(RectMask2D));
                rectMask.layer=29; rectMask.transform.SetParent(canvas.transform,false);
                var rect=rectMask.GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(36,100); rect.anchoredPosition=new Vector2(140,0);
                Bar(rect.transform,material,0,new Color(.2f,1,.3f),true,false);
                var stencilMask=new GameObject("Stencil mask",typeof(RectTransform),typeof(Image),typeof(Mask));
                stencilMask.layer=29; stencilMask.transform.SetParent(canvas.transform,false);
                rect=stencilMask.GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(36,100); rect.anchoredPosition=new Vector2(280,0);
                stencilMask.GetComponent<Mask>().showMaskGraphic=false;
                Bar(rect.transform,material,0,new Color(.8f,.1f,1),true,false);
                first=Capture(camera,target);
                normal.PreviewTime=leader.PreviewTime=self.PreviewTime=2;
                normal.SetVerticesDirty(); leader.SetVerticesDirty(); self.SetVerticesDirty();
                second=Capture(camera,target);
                var orange=first.GetPixel(120,200); var cyan=first.GetPixel(260,200);
                if (orange.a<.9f || orange.r<=orange.b || cyan.b<=cyan.r) throw new Exception("Energy bar surface/tint failed");
                if (first.GetPixel(540,200).a<.9f || first.GetPixel(565,200).a>.05f || first.GetPixel(540,265).a>.05f)
                    throw new Exception("Energy RectMask2D clipping failed");
                if (first.GetPixel(680,200).a<.9f || first.GetPixel(705,200).a>.05f || first.GetPixel(680,265).a>.05f)
                    throw new Exception("Energy stencil clipping failed");
                double difference=0;
                for (int y=120;y<280;y+=4) for(int x=100;x<440;x+=4)
                    difference+=Math.Abs(first.GetPixel(x,y).grayscale-second.GetPixel(x,y).grayscale);
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/Energy-shader-preview.png",first.EncodeToPNG());
                File.WriteAllBytes("Logs/Energy-shader-motion.png",second.EncodeToPNG());
                if (difference<1) throw new Exception("Energy texture animation did not change pixels");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root); target.Release(); UnityEngine.Object.DestroyImmediate(target);
                if(first) UnityEngine.Object.DestroyImmediate(first); if(second) UnityEngine.Object.DestroyImmediate(second);
            }
        }
        static EnergyBarGraphic Bar(Transform parent,Material material,float x,Color color,bool leading,bool self)
        {
            var bar=new GameObject("Energy bar",typeof(RectTransform),typeof(CanvasRenderer),typeof(EnergyBarGraphic)).GetComponent<EnergyBarGraphic>();
            bar.gameObject.layer=29; bar.transform.SetParent(parent,false);
            bar.rectTransform.sizeDelta=new Vector2(64,180); bar.rectTransform.anchoredPosition=new Vector2(x,0);
            bar.material=material; bar.color=color; bar.Leading=leading; bar.SelfCombination=self; bar.PreviewTime=0;
            return bar;
        }
        static Texture2D Capture(Camera camera,RenderTexture target)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var bar in UnityEngine.Object.FindObjectsByType<EnergyBarGraphic>(FindObjectsSortMode.None)) bar.Rebuild(CanvasUpdate.PreRender);
            camera.Render();
            var previous=RenderTexture.active;
            try
            {
                RenderTexture.active=target;
                var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); return image;
            }
            finally { RenderTexture.active=previous; }
        }
    }
}
