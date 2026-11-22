using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    [CreateAssetMenu(menuName = "EcoSphere/Sphere Catalog")]
    public sealed class SphereCatalog : ScriptableObject
    {
        public List<SphereDefinition> spheres = new List<SphereDefinition>();
        public ElementRecipeBook recipeBook;
        public int[] openingSpheres = { SphereIds.Fire, SphereIds.Void };
        static SphereCatalog current;
        Dictionary<int,SphereDefinition> byId;
        Dictionary<ElementId,SphereDefinition> byNamesake;
        List<SphereDefinition> indexedSpheres;
        int indexedCount=-1;
        int indexedIdentityRevision=-1;

        public static SphereCatalog Current
        {
            get
            {
                if (!current) current = Resources.Load<SphereCatalog>("SphereCatalog");
                if (!current) throw new InvalidOperationException("Missing SphereCatalog. Run EcoSphere/Configure Sphere Catalog.");
                return current;
            }
        }
        void OnEnable()=>InvalidateIndexes();
        void OnValidate()=>InvalidateIndexes();
        void OnDisable(){if(current==this)current=null;}
        public void InvalidateIndexes(){byId=null;byNamesake=null;indexedSpheres=null;indexedCount=-1;}
        void EnsureIndexes()
        {
            if(spheres==null)throw new InvalidOperationException(name+": missing spheres");
            if(byId!=null && ReferenceEquals(indexedSpheres,spheres) && indexedCount==spheres.Count && indexedIdentityRevision==SphereDefinition.IdentityRevision)return;
            var ids=new Dictionary<int,SphereDefinition>(spheres.Count);
            var namesakes=new Dictionary<ElementId,SphereDefinition>(spheres.Count);
            foreach(var sphere in spheres)
            {
                if(!sphere || !ids.TryAdd(sphere.id,sphere))throw new InvalidOperationException("Missing or duplicate sphere definition");
                if(!Enum.IsDefined(typeof(ElementId),sphere.namesakeElement) || !namesakes.TryAdd(sphere.namesakeElement,sphere))
                    throw new InvalidOperationException(sphere.name+": missing or duplicate namesake element");
            }
            byId=ids;byNamesake=namesakes;indexedSpheres=spheres;indexedCount=spheres.Count;
            indexedIdentityRevision=SphereDefinition.IdentityRevision;
        }
        public bool TryGet(int id,out SphereDefinition sphere){EnsureIndexes();return byId.TryGetValue(id,out sphere);}
        public SphereDefinition Get(int id) => TryGet(id,out var sphere) ? sphere : throw new InvalidOperationException("Unknown sphere ID: "+id);
        public string ElementName(ElementId element)=>element.ToString();
        public SphereDefinition ElementAppearance(ElementId element)
        {
            EnsureIndexes();return byNamesake.TryGetValue(element,out var sphere) ? sphere : null;
        }
        public AudioClip ElementVoice(ElementId element)
        {
            var canonical=ElementAppearance(element);
            if(canonical && canonical.resourceVoice)return canonical.resourceVoice;
            // Until an element gets its own sphere, use the strongest existing representative voice.
            AudioClip voice=null;float bestShare=0;
            foreach(var sphere in spheres)
                foreach(var contribution in sphere.contributions)
                    if(contribution.element==element && sphere.resourceVoice)
                    {
                        float share=(float)contribution.amount/sphere.TotalContributionUnits;
                        if(share>bestShare){bestShare=share;voice=sphere.resourceVoice;}
                    }
            return voice;
        }
        public void Validate()
        {
            InvalidateIndexes();EnsureIndexes();
            foreach(var sphere in spheres)sphere.Validate();
            if(openingSpheres==null || openingSpheres.Length!=2 || openingSpheres[0]==openingSpheres[1])
                throw new InvalidOperationException("Opening level needs two distinct spheres");
            foreach(int id in openingSpheres)
                if(!Get(id).AmmoEligible || !Get(id).availableAtStart)throw new InvalidOperationException("Invalid opening sphere");
            if(!recipeBook)throw new InvalidOperationException("Missing recipe book");
            recipeBook.Validate();
            foreach(var recipe in recipeBook.recipes)
                if(Get(recipe.output.id)!=recipe.output)throw new InvalidOperationException("Recipe output is not the catalog's sphere definition");
        }
    }
}
