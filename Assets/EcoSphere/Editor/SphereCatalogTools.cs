using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EcoSphere.Editor
{
    public static class SphereCatalogTools
    {
        const string Folder = "Assets/EcoSphere/Data/Spheres";
        const string CatalogPath = "Assets/EcoSphere/Resources/SphereCatalog.asset";

        [MenuItem("EcoSphere/Configure Sphere Catalog")]
        public static void Configure()
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory("Assets/EcoSphere/Resources");
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<SphereCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<SphereCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            Add(catalog, SphereIds.Ice, "Ice", ElementId.Ice, new Color32(55, 198, 255, 255), SphereRole.Element, false);
            Add(catalog, SphereIds.Sunlight, "Sunlight", ElementId.Sunlight, new Color32(255, 194, 40, 255), SphereRole.Element, false);
            Add(catalog, SphereIds.Laser, "Laser", ElementId.Laser, Color.red, SphereRole.Laser, false);
            Add(catalog, SphereIds.Stone, "Stone", ElementId.Stone, new Color32(208, 211, 216, 255), SphereRole.Element, false);
            Add(catalog, SphereIds.Fire, "Fire", ElementId.Fire, new Color32(255, 86, 24, 255), SphereRole.Element, true);
            Add(catalog, SphereIds.Void, "Void", ElementId.Void, new Color32(232, 238, 255, 255), SphereRole.Element, true);
            Add(catalog, SphereIds.Water, "Water", ElementId.Water, new Color32(20,100,225,255), SphereRole.Element, false);
            Add(catalog, SphereIds.Plant, "Plant", ElementId.Plant, new Color32(62,160,74,255), SphereRole.Element, false);
            Add(catalog, SphereIds.Life, "Life", ElementId.Life, new Color32(246,183,205,255), SphereRole.Element, false);
            Add(catalog, SphereIds.Mud, "Mud", ElementId.Mud, new Color32(202,156,107,255), SphereRole.Element, false);
            Add(catalog, SphereIds.Core, "Core", ElementId.Core, new Color32(163, 64, 255, 255), SphereRole.Core, false);
            if (!catalog.recipeBook)
            {
                var book = AssetDatabase.LoadAssetAtPath<ElementRecipeBook>("Assets/EcoSphere/Data/ElementRecipes.asset");
                if (!book)
                {
                    book = ScriptableObject.CreateInstance<ElementRecipeBook>();
                    // Existing Sunlight artwork represents the solar discovery; no duplicate Sun type.
                    book.recipes.Add(new ElementRecipe { first = ElementId.Fire, second = ElementId.Void, output = catalog.Get(SphereIds.Sunlight) });
                    AssetDatabase.CreateAsset(book, "Assets/EcoSphere/Data/ElementRecipes.asset");
                }
                catalog.recipeBook = book;
            }
            SeedRecipe(catalog,ElementId.Ice,ElementId.Fire,SphereIds.Water);
            SeedRecipe(catalog,ElementId.Stone,ElementId.Water,SphereIds.Mud);
            SeedRecipe(catalog,ElementId.Water,ElementId.Mud,SphereIds.Life);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            catalog.Validate();
            Debug.Log($"ECOSPHERE_SPHERE_CATALOG_CONFIGURED: {catalog.spheres.Count} definitions, shared artwork, contributions and colors");
        }

        static void SeedRecipe(SphereCatalog catalog,ElementId first,ElementId second,int output)
        {
            if(catalog.recipeBook.TryCombine(first,second,out _))return;
            catalog.recipeBook.recipes.Add(new ElementRecipe{first=first,second=second,output=catalog.Get(output)});
            catalog.recipeBook.InvalidateIndex();EditorUtility.SetDirty(catalog.recipeBook);
        }

        static void Add(SphereCatalog catalog, int id, string label, ElementId element, Color color, SphereRole role, bool starter)
        {
            string path = Folder + "/" + label + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<SphereDefinition>(path);
            if (!definition)
            {
                definition = ScriptableObject.CreateInstance<SphereDefinition>();
                definition.id = id; definition.namesakeElement=element; definition.displayName = label; definition.color = color;
                definition.category=id==SphereIds.Plant || id==SphereIds.Life ? SphereCategory.Life :
                    id==SphereIds.Void || role==SphereRole.Core ? SphereCategory.Forces :
                    role==SphereRole.Laser ? SphereCategory.Technology : SphereCategory.Materials;
                if(id==SphereIds.Plant)definition.displayName="Plant Life";
                definition.role = role; definition.selectable = role == SphereRole.Element;
                definition.availableAtStart = starter;
                definition.unlit = role == SphereRole.Core || id == SphereIds.Void;
                if(id==SphereIds.Void) { definition.glowColor=Color.white; definition.glowStrength=.42f; }
                SeedContributions(definition,element);
                string art = "Assets/EcoSphere/Art/";
                string stem = "Spheres/" + label + "/" + label.ToLowerInvariant();
                string version=id==SphereIds.Stone ? "v2" : "v1";
                definition.surface = AssetDatabase.LoadAssetAtPath<Texture2D>(art + (role == SphereRole.Core ? "Core/core-surface-v2.png" : stem + "-surface-"+version+".png"));
                definition.icon = AssetDatabase.LoadAssetAtPath<Sprite>(art + (role == SphereRole.Core ? "Core/core-halo-v1.png" : stem + "-icon-"+version+".png"));
                AssetDatabase.CreateAsset(definition, path);
            }
            if (!catalog.spheres.Contains(definition)) catalog.spheres.Add(definition);
            // Seed missing assignments without replacing designer-authored clips.
            if (!definition.resourceVoice)
            {
                string voice = id == SphereIds.Ice ? "cryosphere" : id == SphereIds.Sunlight ? "heliosphere" :
                    id == SphereIds.Fire ? "pyrosphere" : id == SphereIds.Void ? "shadowsphere" : id == SphereIds.Stone ? "lithosphere" : id == SphereIds.Water ? "hydrosphere" : id == SphereIds.Plant ? "florasphere" : id == SphereIds.Life ? "biosphere" : id == SphereIds.Mud ? "pelitosphere" : null;
                if (voice != null)
                {
                    definition.resourceVoice = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/Voice/" + voice + ".mp3");
                    EditorUtility.SetDirty(definition);
                }
            }
        }

        static double TotalWeight(SphereDefinition sphere)
        { double sum=sphere.unallocatedWeight; foreach(var entry in sphere.contributions) sum+=entry.amount; return sum; }

        static void SeedContributions(SphereDefinition sphere,ElementId fallback)
        {
            sphere.contributions.Clear();
            switch(sphere.id)
            {
                case SphereIds.Fire:case SphereIds.Water:case SphereIds.Stone:case SphereIds.Life:
                    sphere.contributions.Add(new ElementContribution(fallback,3));sphere.unallocatedWeight=1;break;
                case SphereIds.Void:
                    sphere.contributions.Add(new ElementContribution(ElementId.Void,2));sphere.unallocatedWeight=2;break;
                case SphereIds.Sunlight:
                    sphere.contributions.Add(new ElementContribution(ElementId.Fire,1));sphere.contributions.Add(new ElementContribution(ElementId.Sunlight,2));sphere.unallocatedWeight=1;break;
                case SphereIds.Ice:
                    sphere.contributions.Add(new ElementContribution(ElementId.Water,1));sphere.contributions.Add(new ElementContribution(ElementId.Ice,2));sphere.unallocatedWeight=1;break;
                case SphereIds.Plant:
                    sphere.contributions.Add(new ElementContribution(ElementId.Life,1));sphere.contributions.Add(new ElementContribution(ElementId.Plant,2));sphere.unallocatedWeight=1;break;
                case SphereIds.Mud:
                    sphere.contributions.Add(new ElementContribution(ElementId.Water,1));sphere.contributions.Add(new ElementContribution(ElementId.Stone,1));sphere.contributions.Add(new ElementContribution(ElementId.Mud,2));break;
                default:sphere.contributions.Add(new ElementContribution(fallback,1));break;
            }
        }

        [MenuItem("EcoSphere/Validate Sphere Catalog")]
        public static void Validate()
        {
            var catalog = SphereCatalog.Current;
            catalog.Validate();
            void Expect(bool value, string message) { if (!value) throw new Exception("Sphere catalog: " + message); }
            var audio=GameAudioSettings.Current;audio.Validate();
            int voiceMinimum=audio.minimumMatchedSpheresForVoice;
            try
            {
                foreach(int minimum in new[]{voiceMinimum,voiceMinimum+2})
                {
                    audio.minimumMatchedSpheresForVoice=minimum;
                    foreach(var sphere in catalog.spheres)if(sphere.AmmoEligible && sphere.resourceVoice)
                    {
                        Expect(!LevelAudio.MatchVoice(sphere.id,minimum-1),sphere.displayName+" voice below minimum");
                        Expect(LevelAudio.MatchVoice(sphere.id,minimum)==sphere.resourceVoice,sphere.displayName+" voice at adjustable minimum");
                    }
                }
            }
            finally{audio.minimumMatchedSpheresForVoice=voiceMinimum;}
            int voicePoints=LevelAudio.SphereVoiceMinimumPoints;
            var candidates=new Dictionary<int,int>{{SphereIds.Fire,voicePoints},{SphereIds.Void,voicePoints-1}};
            Expect(LevelAudio.ResolutionVoice(candidates)==catalog.Get(SphereIds.Fire).resourceVoice,"one qualifying type must keep its voice");
            candidates[SphereIds.Void]=voicePoints;
            Expect(LevelAudio.ResolutionVoice(candidates)==audio.ecosphere,"two qualifying types must replace individual voices with EcoSphere");
            candidates.Clear();candidates[SphereIds.Mud]=voicePoints*3;
            Expect(LevelAudio.ResolutionVoice(candidates)==catalog.Get(SphereIds.Mud).resourceVoice,"mixed contributions must not invent multiple qualifying sphere types");
            foreach (var sphere in catalog.spheres)
            {
                Expect(sphere.surface.width <= 256 && sphere.surface.height <= 256 && sphere.icon.texture.width <= 256 && sphere.icon.texture.height <= 256,
                    sphere.displayName + " exceeds the imported 256px limit");
                var totals = new double[Enum.GetValues(typeof(ElementId)).Length];
                sphere.AddContributions(totals, ElementScoring.Breakaway);
                foreach (var entry in sphere.contributions)
                    Expect(totals[(int)entry.element] == (sphere.role == SphereRole.Core ? 0 : ElementScoring.Breakaway * (entry.amount / TotalWeight(sphere))), sphere.displayName + " breakaway amount");
            }
            var constrained=UnityEngine.Object.Instantiate(catalog.Get(SphereIds.Fire));
            try
            {
                constrained.contributions=new List<ElementContribution>{new ElementContribution(ElementId.Fire,4)};
                constrained.unallocatedWeight=0; constrained.Validate();
                constrained.contributions[0]=new ElementContribution(ElementId.Fire,1);
                constrained.unallocatedWeight=3; constrained.Validate();
                constrained.contributions[0]=new ElementContribution(ElementId.Fire,2);
                bool rejected=false;
                try { constrained.Validate(); } catch(InvalidOperationException) { rejected=true; }
                Expect(rejected,"real and unused units must share the four-unit limit");
                constrained.unallocatedWeight=0; constrained.contributions[0]=new ElementContribution(ElementId.Fire,5);
                rejected=false;
                try { constrained.Validate(); } catch(InvalidOperationException) { rejected=true; }
                Expect(rejected,"five repeated units must be rejected");
            }
            finally { UnityEngine.Object.DestroyImmediate(constrained); }
            var mixed = ScriptableObject.CreateInstance<SphereDefinition>();
            mixed.contributions = new List<ElementContribution> {
                new ElementContribution(ElementId.Fire, 1), new ElementContribution(ElementId.Water, 1),
                new ElementContribution(ElementId.Stone, 1), new ElementContribution(ElementId.Void, 1) };
            var gains = new double[Enum.GetValues(typeof(ElementId)).Length];
            mixed.AddContributions(gains, 16);
            Expect(gains[(int)ElementId.Fire] == 4 && gains[(int)ElementId.Water] == 4 && gains[(int)ElementId.Stone] == 4 && gains[(int)ElementId.Void] == 4,
                "four distinct contributions must share the sphere score");
            mixed.contributions = new List<ElementContribution> {
                new ElementContribution(ElementId.Void,1), new ElementContribution(ElementId.Fire,3) };
            Array.Clear(gains,0,gains.Length);
            mixed.AddContributions(gains,12);
            Expect(gains[(int)ElementId.Void] == 3 && gains[(int)ElementId.Fire] == 9, "12-point weighted match");
            mixed.AddContributions(gains,50);
            Expect(gains[(int)ElementId.Void] == 15.5 && gains[(int)ElementId.Fire] == 46.5, "fractional breakaway accumulation");
            Expect(Math.Truncate(gains[(int)ElementId.Void]) == 15 && gains[(int)ElementId.Void] == 15.5, "display truncation must preserve stored fractions");
            mixed.contributions = new List<ElementContribution> { new ElementContribution(ElementId.Fire,1) };
            mixed.unallocatedWeight = 1;
            Array.Clear(gains,0,gains.Length); mixed.AddContributions(gains,12);
            Expect(gains[(int)ElementId.Fire] == 6, "unallocated weight must consume half a 12-point match");
            mixed.AddContributions(gains,50);
            Expect(gains[(int)ElementId.Fire] == 31, "unallocated weight applies to breakaways too");
            mixed.contributions[0] = new ElementContribution(ElementId.Fire,2); mixed.unallocatedWeight = 0;
            Array.Clear(gains,0,gains.Length); mixed.AddContributions(gains,12);
            Expect(gains[(int)ElementId.Fire] == 12, "upgraded sphere must deliver twice the elemental yield");
            UnityEngine.Object.DestroyImmediate(mixed);
            Expect(catalog.recipeBook.TryCombine(ElementId.Fire, ElementId.Void, out var forward) && forward.id == SphereIds.Sunlight, "opening recipe");
            Expect(catalog.recipeBook.TryCombine(ElementId.Void, ElementId.Fire, out var reverse) && reverse == forward, "recipe order independence");
            Expect(!catalog.recipeBook.TryCombine(ElementId.Ice, ElementId.Void, out _), "unknown recipe must not create an element");
            Debug.Log("ECOSPHERE_SPHERE_CATALOG_CHECKS_PASSED");
        }

        public static void ConfigureAndValidate()
        {
            Configure(); Validate(); TestLevelTools.Validate();
        }
        public static void FirstTutorialSmoke()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(FirstTutorialTools.ScenePath);
            EditorApplication.isPlaying = true;
        }

        public static void TutorialTwoNavigationSmoke()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SystemSceneTools.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
