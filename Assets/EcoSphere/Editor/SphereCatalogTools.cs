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
            Add(catalog, SphereIds.Air, "Air", ElementId.Air, new Color32(232,245,250,255), SphereRole.Element, false);
            Add(catalog, SphereIds.Crystal, "Crystal", ElementId.Crystal, new Color32(227,108,242,255), SphereRole.Element, false);
            Add(catalog, SphereIds.Civilization, "Civilization", ElementId.Civilization, new Color32(232,183,114,255), SphereRole.Element, false);
            Add(catalog, SphereIds.Magic, "Magic", ElementId.Magic, new Color32(53,203,182,255), SphereRole.Element, false);
            Add(catalog, SphereIds.Technology, "Technology", ElementId.Technology, new Color32(13,152,237,255), SphereRole.Element, false);
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
            SeedRecipe(catalog,ElementId.Stone,ElementId.Magic,SphereIds.Crystal);
            SeedRecipe(catalog,ElementId.Human,ElementId.Void,SphereIds.Magic);
            SeedRecipe(catalog,ElementId.Plant,ElementId.Spirit,SphereIds.Magic);   // Forest (double Plant) + Spirit; revisit once those spheres exist
            // Spirit has no sphere yet; its visualizer color lives here until one exists (then Configure drops it below).
            SeedElementColor(catalog,ElementId.Spirit,new Color32(122,140,170,255));
            // Stand-in visualizer colors are only for elements without a sphere; drop any whose element has gained one.
            catalog.InvalidateIndexes();catalog.elementColors.RemoveAll(entry=>catalog.ElementAppearance(entry.element));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            catalog.Validate();
            Debug.Log($"ECOSPHERE_SPHERE_CATALOG_CONFIGURED: {catalog.spheres.Count} definitions, shared artwork, contributions and colors");
        }

        // Shared import settings for a sphere's art folder: the icon is a transparent sprite, the surface a repeating texture.
        public static void ImportSphereArt(string label,string version="v1")
        {
            AssetDatabase.Refresh();
            string stem="Assets/EcoSphere/Art/Spheres/"+label+"/"+label.ToLowerInvariant()+"-";
            foreach(string kind in new[]{"surface","icon"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(stem+kind+"-"+version+".png") ?? throw new Exception("Missing "+label+" "+kind+" art");
                importer.textureType=kind=="icon" ? TextureImporterType.Sprite : TextureImporterType.Default;
                importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=256;
                importer.alphaIsTransparency=kind=="icon";importer.mipmapEnabled=kind=="surface";
                importer.wrapMode=kind=="surface" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
        }

        // A stand-in visualizer color for an element without a sphere. Skipped once the element has a namesake sphere.
        static void SeedElementColor(SphereCatalog catalog,ElementId element,Color color)
        {
            catalog.InvalidateIndexes();
            if(catalog.ElementAppearance(element) || catalog.elementColors.Exists(entry=>entry.element==element))return;
            catalog.elementColors.Add(new ElementColorEntry{element=element,color=color});
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
                    role==SphereRole.Laser || id==SphereIds.Civilization || id==SphereIds.Technology ? SphereCategory.Technology :
                    id==SphereIds.Magic ? SphereCategory.Beyond : SphereCategory.Basic;
                if(id==SphereIds.Plant)definition.displayName="Plant Life";
                definition.role = role; definition.selectable = role == SphereRole.Element;
                definition.availableAtStart = starter;
                definition.unlit = role == SphereRole.Core || id == SphereIds.Void;
                if(id==SphereIds.Void) { definition.glowColor=Color.white; definition.glowStrength=.42f; }
                if(id==SphereIds.Crystal) { definition.shape=SphereShape.Crystal; definition.deflectedHits=2;
                    definition.deflectSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/crystal-hit.mp3"); definition.breakSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/crystal-breaking.mp3");
                    definition.description="Adds a bit of magic to the world, while reflecting other elements."; }
                if(id==SphereIds.Civilization)definition.description="Spreads settlements across the land. Their lights glow brighter as technology advances.";
                if(id==SphereIds.Magic)definition.description="Fills the world with wonder, tinting its plant life and city lights in strange colors.";
                if(id==SphereIds.Technology)definition.description="Powers civilization, making its city lights shine brighter.";
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
                    id == SphereIds.Fire ? "pyrosphere" : id == SphereIds.Void ? "shadowsphere" : id == SphereIds.Stone ? "lithosphere" : id == SphereIds.Water ? "hydrosphere" : id == SphereIds.Plant ? "florasphere" : id == SphereIds.Life ? "biosphere" : id == SphereIds.Mud ? "pelitosphere" : id == SphereIds.Air ? "atmosphere" : id == SphereIds.Crystal ? "latisphere" : id == SphereIds.Civilization ? "politosphere" : id == SphereIds.Magic ? "arcanosphere" : id == SphereIds.Technology ? "technosphere" : null;
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
                case SphereIds.Fire:case SphereIds.Water:case SphereIds.Stone:case SphereIds.Life:case SphereIds.Air:
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
                case SphereIds.Crystal:
                    sphere.contributions.Add(new ElementContribution(ElementId.Stone,1));sphere.contributions.Add(new ElementContribution(ElementId.Magic,1));sphere.contributions.Add(new ElementContribution(ElementId.Crystal,2));break;
                case SphereIds.Civilization:
                    sphere.contributions.Add(new ElementContribution(ElementId.Human,1));sphere.contributions.Add(new ElementContribution(ElementId.Civilization,2));sphere.unallocatedWeight=1;break;
                case SphereIds.Magic:
                    sphere.contributions.Add(new ElementContribution(ElementId.Magic,3));sphere.contributions.Add(new ElementContribution(ElementId.Void,1));break;
                case SphereIds.Technology:
                    sphere.contributions.Add(new ElementContribution(ElementId.Civilization,1));sphere.contributions.Add(new ElementContribution(ElementId.Technology,2));sphere.unallocatedWeight=1;break;
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
            // Readable yields come from contributions: largest share first, unallocated units omitted, Core empty.
            Expect(catalog.Get(SphereIds.Ice).ContributionSummary()=="Ice 50% · Water 25%","Ice contribution summary");
            Expect(catalog.Get(SphereIds.Mud).ContributionSummary()=="Mud 50% · Water 25% · Stone 25%","Mud contribution summary");
            Expect(catalog.Get(SphereIds.Core).ContributionSummary()=="","Core must not list contributions");
            var crystal=catalog.Get(SphereIds.Crystal);
            Expect(crystal.shape==SphereShape.Crystal && crystal.AmmoEligible && !crystal.availableAtStart && crystal.TotalContributionUnits==4 &&
                crystal.ContributionSummary()=="Crystal 50% · Stone 25% · Magic 25%","Crystal identity, shape and 1 Stone / 1 Magic / 2 Crystal");
            Expect(crystal.resourceVoice && crystal.resourceVoice.name=="latisphere" && LevelAudio.MatchVoice(SphereIds.Crystal,voiceMinimum)==crystal.resourceVoice && !LevelAudio.MatchVoice(SphereIds.Crystal,voiceMinimum-1),"Crystal must use latisphere at the shared large-match threshold");
            Expect(crystal.deflectedHits==2 && crystal.deflectSound && crystal.deflectSound.name=="crystal-hit" && crystal.breakSound && crystal.breakSound.name=="crystal-breaking","Crystal deflects two hits with crystal-hit, breaks with crystal-breaking");
            Expect(ShotDeflection.Resolve(crystal,SphereIds.Fire,0)==ShotImpact.Deflect && ShotDeflection.Resolve(crystal,SphereIds.Fire,1)==ShotImpact.Deflect && ShotDeflection.Resolve(crystal,SphereIds.Fire,2)==ShotImpact.Break &&
                ShotDeflection.Resolve(crystal,SphereIds.Crystal,0)==ShotImpact.Attach && ShotDeflection.Resolve(catalog.Get(SphereIds.Stone),SphereIds.Fire,5)==ShotImpact.Attach,"deflection rule: two deflections, third hit breaks, same type and ordinary spheres attach");
            Expect(catalog.recipeBook.TryCombine(ElementId.Magic,ElementId.Stone,out var crystalRecipe) && crystalRecipe==crystal,"Stone + Magic must unlock Crystal");
            var magic=catalog.Get(SphereIds.Magic);
            Expect(catalog.ElementAppearance(ElementId.Magic)==magic && catalog.ElementColor(ElementId.Magic)==magic.color && !catalog.elementColors.Exists(entry=>entry.element==ElementId.Magic) &&
                catalog.ElementColor(ElementId.Crystal)==crystal.color,"element colors: namesake Magic and Crystal, no stale stand-in color");
            var civilization=catalog.Get(SphereIds.Civilization);
            Expect(civilization.namesakeElement==ElementId.Civilization && civilization.color==(Color)new Color32(232,183,114,255) && civilization.category==SphereCategory.Technology &&
                civilization.AmmoEligible && !civilization.availableAtStart && civilization.surface && civilization.icon &&
                civilization.ContributionSummary()=="Civilization 50% · Human 25%" && civilization.unallocatedWeight==1 && civilization.TotalContributionUnits==4,
                "Civilization identity: sandstone, Technology category, 1 Human / 2 Civilization / 1 Null, art");
            Expect(civilization.resourceVoice && civilization.resourceVoice.name=="politosphere" && LevelAudio.MatchVoice(SphereIds.Civilization,voiceMinimum)==civilization.resourceVoice,"Civilization must use politosphere");
            foreach(var recipe in catalog.recipeBook.recipes)Expect(recipe.output!=civilization,"Civilization recipe should remain unset for now");
            Expect(magic.namesakeElement==ElementId.Magic && magic.color==(Color)new Color32(53,203,182,255) && magic.category==SphereCategory.Beyond && magic.AmmoEligible && !magic.availableAtStart &&
                magic.ContributionSummary()=="Magic 75% · Void 25%" && magic.unallocatedWeight==0 && magic.resourceVoice && magic.resourceVoice.name=="arcanosphere","Magic identity: turquoise, Beyond, 3 Magic / 1 Void, arcanosphere");
            Expect(catalog.recipeBook.TryCombine(ElementId.Void,ElementId.Human,out var magicRecipe) && magicRecipe==magic &&
                catalog.recipeBook.TryCombine(ElementId.Spirit,ElementId.Plant,out var spiritRecipe) && spiritRecipe==magic,"Human + Void and Plant + Spirit must both unlock Magic");
            Expect(catalog.ElementAppearance(ElementId.Spirit)==null && catalog.ElementColor(ElementId.Spirit)==(Color)new Color32(122,140,170,255),"Spirit must use its gray-blue stand-in color until it has a sphere");
            var technology=catalog.Get(SphereIds.Technology);
            Expect(technology.namesakeElement==ElementId.Technology && technology.color==(Color)new Color32(13,152,237,255) && technology.category==SphereCategory.Technology && technology.AmmoEligible && !technology.availableAtStart &&
                technology.ContributionSummary()=="Technology 50% · Civilization 25%" && technology.unallocatedWeight==1 && technology.resourceVoice && technology.resourceVoice.name=="technosphere","Technology identity: electric blue, Technology, 1 Civilization / 2 Technology / 1 Null, technosphere");
            foreach(var recipe in catalog.recipeBook.recipes)Expect(recipe.output!=technology,"Technology recipe should remain unset for now");
            // Generated body: every corner stays inside the round footprint, faces point outward, front table stays behind the icon.
            var gem=SphereShapes.Mesh(SphereShape.Crystal);var gemVertices=gem.vertices;var gemNormals=gem.normals;var gemTriangles=gem.triangles;
            foreach(var vertex in gemVertices)Expect(new Vector2(vertex.x,vertex.y).magnitude<=.5001f && vertex.z>-.47f && vertex.z<.5f,"crystal vertex outside the sphere footprint");
            for(int i=0;i<gemTriangles.Length;i+=3)
            {
                Vector3 a=gemVertices[gemTriangles[i]],b=gemVertices[gemTriangles[i+1]],c=gemVertices[gemTriangles[i+2]];
                Expect(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3)>0 && Vector3.Dot(gemNormals[gemTriangles[i]],(a+b+c)/3)>0,"crystal facet faces inward");
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
