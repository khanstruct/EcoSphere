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
            foreach (var seed in SphereSeeds.All) Add(catalog, seed);
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
            // Plant + Spirit stood in for Forest + Spirit while Forest was not an element.
            RetireRecipe(catalog,ElementId.Plant,ElementId.Spirit);
            SeedRecipe(catalog,ElementId.Forest,ElementId.Spirit,SphereIds.Magic);
            RetireRecipe(catalog,ElementId.Air,ElementId.Life);   // the earlier Spirit recipe; Spirit now occurs naturally (SphereDefinition.naturalWeight)
            SeedRecipe(catalog,ElementId.Life,ElementId.Air,SphereIds.Bird);
            SeedRecipe(catalog,ElementId.Life,ElementId.Wind,SphereIds.Bird);
            SeedRecipe(catalog,ElementId.Lava,ElementId.Stone,SphereIds.Metal);
            SeedRecipe(catalog,ElementId.Air,ElementId.Air,SphereIds.Wind);
            SeedRecipe(catalog,ElementId.Sunlight,ElementId.Air,SphereIds.Wind);
            SeedRecipe(catalog,ElementId.Fire,ElementId.Stone,SphereIds.Lava);
            SeedRecipe(catalog,ElementId.Plant,ElementId.Plant,SphereIds.Forest);
            SeedRecipe(catalog,ElementId.Life,ElementId.Water,SphereIds.Fish);
            RetireRecipe(catalog,ElementId.Stone,ElementId.Sunlight);   // Sand moved to Wind + Stone
            SeedRecipe(catalog,ElementId.Wind,ElementId.Stone,SphereIds.Sand);
            // Stand-in visualizer colors are only for elements without a sphere; drop any whose element has gained one.
            catalog.InvalidateIndexes();catalog.elementColors.RemoveAll(entry=>catalog.ElementAppearance(entry.element));
            // Bursts when lost without points: sand crumbles in its own color, crystal shatters in purple.
            SeedLostBurst(catalog,SphereIds.Sand,SphereBurst.Crumble,Color.clear);
            SeedLostBurst(catalog,SphereIds.Crystal,SphereBurst.Shatter,new Color32(155,90,235,255));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            catalog.Validate();
            Debug.Log($"ECOSPHERE_SPHERE_CATALOG_CONFIGURED: {catalog.spheres.Count} definitions, shared artwork, contributions and colors");
        }

        // Shared import settings for a sphere's art folder: the icon is a transparent sprite, the surface a repeating texture.
        // Sphere art that does not follow Art/Spheres/<Label>/<label>-{surface,icon}-v1.png (paths relative to Art/).
        static readonly Dictionary<int,(string surface,string icon)> ArtOverrides=new Dictionary<int,(string,string)>
        {
            {SphereIds.Stone,("Spheres/Stone/stone-surface-v2.png","Spheres/Stone/stone-icon-v3.png")},
            {SphereIds.Fish,("Spheres/Water/water-surface-v1.png","Spheres/Fish/fish-icon-v2.png")},   // Fish swims in Water's surface
            {SphereIds.Civilization,("Spheres/Civilization/civilization-surface-v1.png","Spheres/Civilization/civilization-icon-v2.png")},   // house icon, distinct from Sand
            {SphereIds.Bird,("Spheres/Bird/bird-surface-v1.png","Spheres/Bird/bird-icon-v2.png")},   // the red bird (v2) icon
        };

        // The one place that picks a sphere's surface and icon: the default folder files, Core's art, or an ArtOverrides entry.
        static void AssignArt(SphereDefinition definition,int id,string label,SphereRole role)
        {
            string stem="Spheres/"+label+"/"+label.ToLowerInvariant();
            string surfacePath=stem+"-surface-v1.png",iconPath=stem+"-icon-v1.png";
            if(role==SphereRole.Core){surfacePath="Core/core-surface-v2.png";iconPath="Core/core-halo-v1.png";}
            else if(ArtOverrides.TryGetValue(id,out var overrideArt)){surfacePath=overrideArt.surface;iconPath=overrideArt.icon;}
            definition.surface=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EcoSphere/Art/"+surfacePath);
            definition.icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EcoSphere/Art/"+iconPath);
        }
        internal const string CrystalCracksArt="Assets/EcoSphere/Art/Spheres/Crystal/crystal-cracks-v1.png";
        // Crystal cracks on its first hit (showing its cracks overlay) and shatters on the next.
        public static void ConfigureCrystalDamage()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(CrystalCracksArt) ?? throw new Exception("Missing crystal cracks art");
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.maxTextureSize=256;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var crystal=SphereCatalog.Current.Get(SphereIds.Crystal);
            crystal.deflectedHits=1;crystal.damagedOverlay=AssetDatabase.LoadAssetAtPath<Sprite>(CrystalCracksArt);
            EditorUtility.SetDirty(crystal);AssetDatabase.SaveAssets();
        }
        // Re-applies the art table to an existing sphere asset (seeding only fills new ones), e.g. after its icon changes.
        public static void ReapplyArt(int id)
        {
            var sphere=SphereCatalog.Current.Get(id);
            AssignArt(sphere,id,sphere.name,sphere.role);
            if(!sphere.surface || !sphere.icon)throw new Exception(sphere.name+": art missing after re-applying the art table");
            EditorUtility.SetDirty(sphere);AssetDatabase.SaveAssets();
        }

        // A folder may hold only some of the art (Fish has an icon and borrows Water's surface); at least one file must exist.
        public static void ImportSphereArt(string label,string version="v1")
        {
            AssetDatabase.Refresh();
            string stem="Assets/EcoSphere/Art/Spheres/"+label+"/"+label.ToLowerInvariant()+"-";
            int imported=0;
            foreach(string kind in new[]{"surface","icon"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(stem+kind+"-"+version+".png");
                if(!importer)continue;
                imported++;
                importer.textureType=kind=="icon" ? TextureImporterType.Sprite : TextureImporterType.Default;
                importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=256;
                importer.alphaIsTransparency=kind=="icon";importer.mipmapEnabled=kind=="surface";
                importer.wrapMode=kind=="surface" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            if(imported==0)throw new Exception("Missing "+label+" art ("+version+")");
        }

        // A stand-in visualizer color for an element without a sphere. Skipped once the element has a namesake sphere.
        static void SeedElementColor(SphereCatalog catalog,ElementId element,Color color)
        {
            catalog.InvalidateIndexes();
            if(catalog.ElementAppearance(element) || catalog.elementColors.Exists(entry=>entry.element==element))return;
            catalog.elementColors.Add(new ElementColorEntry{element=element,color=color});
        }

        // Gives an existing sphere asset its authored natural weight while it has none (later Inspector edits stay).
        public static void SeedNaturalWeight(int id)
        {
            var sphere=SphereCatalog.Current.Get(id);float weight=SphereSeeds.Get(id).naturalWeight;
            if(sphere.naturalWeight>0 || weight<=0)return;
            sphere.naturalWeight=weight;EditorUtility.SetDirty(sphere);AssetDatabase.SaveAssets();
        }
        // Re-applies the authored namesake and contributions to an existing sphere asset (seeding only fills new ones).
        public static void ReseedSphere(int id,ElementId namesake)
        {
            var sphere=SphereCatalog.Current.Get(id);
            sphere.namesakeElement=namesake;SphereSeeds.Get(id).ApplyContributions(sphere);
            SphereCatalog.Current.InvalidateIndexes();EditorUtility.SetDirty(sphere);AssetDatabase.SaveAssets();
        }

        // Sets an authored lost-sphere burst on an existing sphere only while it is still unset, so later Inspector edits stay.
        static void SeedLostBurst(SphereCatalog catalog,int id,SphereBurst burst,Color color)
        {
            var sphere=catalog.Get(id);if(sphere.lostBurst!=SphereBurst.None)return;
            sphere.lostBurst=burst;sphere.lostBurstColor=color;EditorUtility.SetDirty(sphere);
        }

        static void RetireRecipe(SphereCatalog catalog,ElementId first,ElementId second)
        {
            var pair=new ElementPair(first,second);
            if(catalog.recipeBook.recipes.RemoveAll(recipe=>new ElementPair(recipe.first,recipe.second).Equals(pair))==0)return;
            catalog.recipeBook.InvalidateIndex();EditorUtility.SetDirty(catalog.recipeBook);
        }

        static void SeedRecipe(SphereCatalog catalog,ElementId first,ElementId second,int output)
        {
            if(catalog.recipeBook.TryCombine(first,second,out _))return;
            catalog.recipeBook.recipes.Add(new ElementRecipe{first=first,second=second,output=catalog.Get(output)});
            catalog.recipeBook.InvalidateIndex();EditorUtility.SetDirty(catalog.recipeBook);
        }

        // Seeds a new sphere asset from its SphereSeeds entry; an existing asset keeps its Inspector edits.
        static void Add(SphereCatalog catalog, SphereSeed seed)
        {
            string path = Folder + "/" + seed.label + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<SphereDefinition>(path);
            if (!definition)
            {
                definition = ScriptableObject.CreateInstance<SphereDefinition>();
                definition.id = seed.id; definition.namesakeElement = seed.element; definition.displayName = seed.displayName ?? seed.label; definition.color = seed.color;
                definition.category = seed.category; definition.description = seed.description; definition.naturalWeight = seed.naturalWeight;
                definition.role = seed.role; definition.selectable = seed.role == SphereRole.Element;
                definition.availableAtStart = seed.starter;
                seed.extra?.Invoke(definition);
                seed.ApplyContributions(definition);
                AssignArt(definition, seed.id, seed.label, seed.role);
                AssetDatabase.CreateAsset(definition, path);
            }
            if (!catalog.spheres.Contains(definition)) catalog.spheres.Add(definition);
            // Seed missing assignments without replacing designer-authored clips.
            if (!definition.resourceVoice && seed.voice != null)
            {
                definition.resourceVoice = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/EcoSphere/Audio/Voice/" + seed.voice + ".mp3");
                EditorUtility.SetDirty(definition);
            }
        }

        static double TotalWeight(SphereDefinition sphere)
        { double sum=sphere.unallocatedWeight; foreach(var entry in sphere.contributions) sum+=entry.amount; return sum; }

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
            Expect(crystal.deflectedHits==1 && crystal.damagedOverlay && crystal.damagedOverlay.texture.name=="crystal-cracks-v1" && crystal.deflectSound && crystal.deflectSound.name=="crystal-hit" && crystal.breakSound && crystal.breakSound.name=="crystal-breaking","Crystal deflects (and cracks on) one hit with crystal-hit, then breaks with crystal-breaking");
            Expect(ShotDeflection.Resolve(crystal,SphereIds.Fire,0)==ShotImpact.Deflect && ShotDeflection.Resolve(crystal,SphereIds.Fire,1)==ShotImpact.Break &&
                ShotDeflection.Resolve(crystal,SphereIds.Crystal,0)==ShotImpact.Attach && ShotDeflection.Resolve(catalog.Get(SphereIds.Stone),SphereIds.Fire,5)==ShotImpact.Attach,"deflection rule: one deflection, the next hit breaks, same type and ordinary spheres attach");
            Expect(catalog.recipeBook.TryCombine(ElementId.Magic,ElementId.Stone,out var crystalRecipe) && crystalRecipe==crystal,"Stone + Magic must unlock Crystal");
            var magic=catalog.Get(SphereIds.Magic);
            Expect(catalog.ElementAppearance(ElementId.Magic)==magic && catalog.ElementColor(ElementId.Magic)==magic.color && !catalog.elementColors.Exists(entry=>entry.element==ElementId.Magic) &&
                catalog.ElementColor(ElementId.Crystal)==crystal.color,"element colors: namesake Magic and Crystal, no stale stand-in color");
            var civilization=catalog.Get(SphereIds.Civilization);
            Expect(civilization.namesakeElement==ElementId.Civilization && civilization.color==(Color)new Color32(232,183,114,255) && civilization.category==SphereCategory.Technology &&
                civilization.AmmoEligible && !civilization.availableAtStart && civilization.surface && civilization.icon &&
                civilization.ContributionSummary()=="Civilization 50% · Human 25%" && civilization.unallocatedWeight==1 && civilization.TotalContributionUnits==4,
                "Civilization identity: sandstone, Technology category, 1 Human / 2 Civilization / 1 Null, art");
            Expect(civilization.icon.texture.name=="civilization-icon-v2" && civilization.surface.name=="civilization-surface-v1","Civilization must use its house icon (v2) and v1 surface");
            Expect(civilization.resourceVoice && civilization.resourceVoice.name=="politosphere" && LevelAudio.MatchVoice(SphereIds.Civilization,voiceMinimum)==civilization.resourceVoice,"Civilization must use politosphere");
            var civilizationLine=civilization.contributions.Find(entry=>entry.element==ElementId.Civilization);
            Expect(civilizationLine.limited && civilizationLine.limitedBy==ElementId.Human && Mathf.Approximately(civilizationLine.limitRatio,1),"Civilization's Civilization must be capped by the planet's Human");
            var stone=catalog.Get(SphereIds.Stone);Expect(stone.icon.texture.name=="stone-icon-v3" && stone.surface.name=="stone-surface-v2","Stone must use its v3 icon");
            foreach(var recipe in catalog.recipeBook.recipes)Expect(recipe.output!=civilization,"Civilization recipe should remain unset for now");
            Expect(magic.namesakeElement==ElementId.Magic && magic.color==(Color)new Color32(53,203,182,255) && magic.category==SphereCategory.Beyond && magic.AmmoEligible && !magic.availableAtStart &&
                magic.ContributionSummary()=="Magic 75% · Void 25%" && magic.unallocatedWeight==0 && magic.resourceVoice && magic.resourceVoice.name=="arcanosphere","Magic identity: turquoise, Beyond, 3 Magic / 1 Void, arcanosphere");
            Expect(catalog.recipeBook.TryCombine(ElementId.Void,ElementId.Human,out var magicRecipe) && magicRecipe==magic &&
                catalog.recipeBook.TryCombine(ElementId.Spirit,ElementId.Forest,out var spiritRecipe) && spiritRecipe==magic && !catalog.recipeBook.TryCombine(ElementId.Plant,ElementId.Spirit,out _),
                "Human + Void and Forest + Spirit must both unlock Magic; the Plant + Spirit stand-in is retired");
            var technology=catalog.Get(SphereIds.Technology);
            Expect(technology.namesakeElement==ElementId.Technology && technology.color==(Color)new Color32(13,152,237,255) && technology.category==SphereCategory.Technology && technology.AmmoEligible && !technology.availableAtStart &&
                technology.ContributionSummary()=="Technology 50% · Civilization 25%" && technology.unallocatedWeight==1 && technology.resourceVoice && technology.resourceVoice.name=="technosphere","Technology identity: electric blue, Technology, 1 Civilization / 2 Technology / 1 Null, technosphere");
            foreach(var recipe in catalog.recipeBook.recipes)Expect(recipe.output!=technology,"Technology recipe should remain unset for now");
            var spirit=catalog.Get(SphereIds.Spirit);
            Expect(spirit.namesakeElement==ElementId.Spirit && spirit.color==(Color)new Color32(168,230,194,255) && spirit.category==SphereCategory.Beyond && spirit.AmmoEligible && !spirit.availableAtStart &&
                spirit.ContributionSummary()=="Spirit 50% · Magic 25% · Void 25%" && spirit.unallocatedWeight==0 && spirit.resourceVoice && spirit.resourceVoice.name=="ectosphere","Spirit identity: mint, Beyond, 1 Magic / 2 Spirit / 1 Void, ectosphere");
            foreach(var recipe in catalog.recipeBook.recipes)Expect(recipe.output!=spirit,"Spirit has no recipe for now (it will occur naturally)");
            Expect(!catalog.recipeBook.TryCombine(ElementId.Life,ElementId.Air,out var lifeAir) || lifeAir!=spirit,"Air + Life no longer unlocks Spirit");
            Expect(spirit.OccursNaturally,"Spirit must be able to occur naturally");
            foreach(var other in catalog.spheres)Expect(other.naturalWeight==0 || other.AmmoEligible,other.displayName+": only collectable spheres can occur naturally");
            var bird=catalog.Get(SphereIds.Bird);
            Expect(bird.namesakeElement==ElementId.Bird && bird.color==(Color)new Color32(120,190,245,255) && bird.category==SphereCategory.Life && bird.AmmoEligible && !bird.availableAtStart &&
                bird.surface && bird.surface.name=="bird-surface-v1" && bird.icon && bird.icon.texture.name=="bird-icon-v2" &&
                bird.ContributionSummary()=="Bird 50% · Life 25% · Air 25%" && bird.unallocatedWeight==0 && bird.resourceVoice && bird.resourceVoice.name=="biosphere",
                "Bird identity: light blue, Life, v2 bird icon, 2 Bird / 1 Life / 1 Air, biosphere");
            var birdLife=bird.contributions.Find(entry=>entry.element==ElementId.Life);
            Expect(birdLife.limited && birdLife.limitedBy==ElementId.Air && Mathf.Approximately(birdLife.limitRatio,1) &&
                !bird.contributions.Exists(entry=>entry.element!=ElementId.Life && entry.limited),"Bird's Life (only) must be capped by the planet's Air");
            Expect(catalog.recipeBook.TryCombine(ElementId.Air,ElementId.Life,out var birdFromAir) && birdFromAir==bird &&
                catalog.recipeBook.TryCombine(ElementId.Wind,ElementId.Life,out var birdFromWind) && birdFromWind==bird,"Life + Air and Life + Wind must unlock Bird");
            var metal=catalog.Get(SphereIds.Metal);
            Expect(metal.namesakeElement==ElementId.Metal && metal.color==(Color)new Color32(90,97,112,255) && metal.category==SphereCategory.Basic && metal.AmmoEligible && !metal.availableAtStart &&
                metal.surface && metal.surface.name=="metal-surface-v1" && metal.icon && metal.icon.texture.name=="metal-icon-v1" &&
                metal.ContributionSummary()=="Metal 50%" && metal.unallocatedWeight==2 && metal.resourceVoice && metal.resourceVoice.name=="ferrosphere" && metal.OccursNaturally,
                "Metal identity: dark gray, Basic, 2 Metal / 2 Null, ferrosphere, occurs naturally");
            Expect(catalog.recipeBook.TryCombine(ElementId.Stone,ElementId.Lava,out var metalFrom) && metalFrom==metal,"Lava + Stone must unlock Metal");
            var wind=catalog.Get(SphereIds.Wind);
            Expect(wind.namesakeElement==ElementId.Wind && wind.color==(Color)new Color32(220,214,245,255) && wind.category==SphereCategory.Weather && wind.AmmoEligible && !wind.availableAtStart &&
                wind.surface && wind.surface.name=="wind-surface-v1" && wind.icon && wind.icon.texture.name=="wind-icon-v1" &&
                wind.ContributionSummary()=="Air 50% · Wind 25%" && wind.unallocatedWeight==1 && wind.resourceVoice && wind.resourceVoice.name=="atmosphere" && !wind.OccursNaturally,
                "Wind identity: pale lavender-white, Weather, 1 Wind / 2 Air / 1 Null, atmosphere");
            Expect(catalog.recipeBook.TryCombine(ElementId.Air,ElementId.Air,out var windFrom) && windFrom==wind && catalog.recipeBook.TryCombine(ElementId.Air,ElementId.Sunlight,out var windFromSun) && windFromSun==wind,"Air + Air and Sunlight + Air must unlock Wind");
            Expect(catalog.ElementAppearance(ElementId.Wind)==wind && !catalog.elementColors.Exists(entry=>entry.element==ElementId.Wind),"Wind's sphere must supply its element color");
            var lava=catalog.Get(SphereIds.Lava);
            Expect(lava.namesakeElement==ElementId.Lava && lava.color==(Color)new Color32(204,42,20,255) && lava.category==SphereCategory.Basic && lava.AmmoEligible && !lava.availableAtStart &&
                lava.surface && lava.surface.name=="lava-surface-v1" && lava.icon && lava.icon.texture.name=="lava-icon-v1" && lava.unlit && lava.glowStrength>0 &&
                lava.ContributionSummary()=="Lava 50% · Fire 25%" && lava.unallocatedWeight==1 && lava.resourceVoice && lava.resourceVoice.name=="pyrosphere",
                "Lava identity: deep red-orange, Basic, self-lit with an orange glow, 2 Lava / 1 Fire / 1 Null, pyrosphere");
            Expect(catalog.recipeBook.TryCombine(ElementId.Stone,ElementId.Fire,out var lavaFrom) && lavaFrom==lava,"Fire + Stone must unlock Lava");
            Expect(catalog.ElementAppearance(ElementId.Lava)==lava && !catalog.elementColors.Exists(entry=>entry.element==ElementId.Lava),"Lava's sphere must supply its element color");
            var forest=catalog.Get(SphereIds.Forest);
            Expect(forest.namesakeElement==ElementId.Forest && forest.color==(Color)new Color32(31,107,42,255) && forest.category==SphereCategory.Life && forest.AmmoEligible && !forest.availableAtStart &&
                forest.ContributionSummary()=="Forest 50% · Plant 25% · Water 25%" && forest.unallocatedWeight==0 && forest.resourceVoice && forest.resourceVoice.name=="florasphere",
                "Forest identity: Forest element, forest green, Life, 2 Forest / 1 Plant / 1 Water, florasphere");
            Expect(catalog.ElementColor(ElementId.Forest)==forest.color,"the Forest element must use the Forest sphere color");
            var fish=catalog.Get(SphereIds.Fish);var water=catalog.Get(SphereIds.Water);
            Expect(fish.namesakeElement==ElementId.Fish && fish.color==(Color)new Color32(247,107,13,255) && fish.category==SphereCategory.Life && fish.AmmoEligible && !fish.availableAtStart &&
                fish.surface==water.surface && fish.icon && fish.icon.texture.name=="fish-icon-v2" &&
                fish.ContributionSummary()=="Life 50% · Water 25% · Fish 25%" && fish.unallocatedWeight==0 && fish.resourceVoice && fish.resourceVoice.name=="biosphere",
                "Fish identity: orange, Life, Water's surface with the v2 fish icon, 2 Life / 1 Water / 1 Fish, biosphere");
            var fishLife=fish.contributions.Find(entry=>entry.element==ElementId.Life);
            Expect(fishLife.limited && fishLife.limitedBy==ElementId.Water && Mathf.Approximately(fishLife.limitRatio,1) &&
                !fish.contributions.Exists(entry=>entry.element!=ElementId.Life && entry.limited),"Fish's Life (only) must be capped by the planet's Water");
            Expect(catalog.recipeBook.TryCombine(ElementId.Water,ElementId.Life,out var fishFrom) && fishFrom==fish,"Life + Water must unlock Fish");
            var sand=catalog.Get(SphereIds.Sand);
            Expect(sand.namesakeElement==ElementId.Sand && sand.color==(Color)new Color32(250,222,155,255) && sand.category==SphereCategory.Basic && sand.AmmoEligible && !sand.availableAtStart &&
                sand.ContributionSummary()=="Sand 50% · Sunlight 25%" && sand.unallocatedWeight==1 && sand.resourceVoice && sand.resourceVoice.name=="lithosphere",
                "Sand identity: pale yellow, Basic, 1 Sunlight / 2 Sand / 1 Null, lithosphere");
            Expect(sand.erodesOnMiss && sand.erodeSound && sand.erodeSound.name=="sandfall" && sand.deflectedHits==0,"Sand must erode on a missed shot with sandfall");
            Expect(sand.lostBurst==SphereBurst.Crumble && sand.LostBurstColor==sand.color && crystal.lostBurst==SphereBurst.Shatter && crystal.LostBurstColor==(Color)new Color32(155,90,235,255),
                "Sand must crumble in its own color; Crystal must shatter in purple");
            foreach(var other in catalog.spheres)Expect(other==sand || !other.erodesOnMiss,"only Sand erodes for now: "+other.displayName);
            Expect(catalog.recipeBook.TryCombine(ElementId.Stone,ElementId.Wind,out var sandFrom) && sandFrom==sand && !catalog.recipeBook.TryCombine(ElementId.Stone,ElementId.Sunlight,out _),"Wind + Stone (not Stone + Sunlight) must unlock Sand");
            Expect(catalog.recipeBook.TryCombine(ElementId.Plant,ElementId.Plant,out var forestFrom) && forestFrom==forest,"Plant + Plant must unlock Forest");
            Expect(catalog.ElementAppearance(ElementId.Plant)==catalog.Get(SphereIds.Plant) && catalog.ElementColor(ElementId.Plant)==catalog.Get(SphereIds.Plant).color,"Forest must not take over Plant's color or art");
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
