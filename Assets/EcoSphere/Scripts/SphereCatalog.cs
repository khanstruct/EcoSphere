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
        public static SphereCatalog Current
        {
            get
            {
                if (!current) current = Resources.Load<SphereCatalog>("SphereCatalog");
                if (!current) throw new InvalidOperationException("Missing SphereCatalog. Run EcoSphere/Configure Sphere Catalog.");
                return current;
            }
        }
        public SphereDefinition Get(int id)
        {
            foreach (var sphere in spheres) if (sphere && sphere.id == id) return sphere;
            throw new InvalidOperationException("Unknown sphere ID: " + id);
        }
        public string ElementName(ElementId element) => element.ToString();
        public SphereDefinition ElementAppearance(ElementId element)
        {
            foreach(var sphere in spheres)
                if(sphere && sphere.contributions.Count==1 && sphere.contributions[0].element==element)return sphere;
            return null;
        }
        public AudioClip ElementVoice(ElementId element)
        {
            foreach (var sphere in spheres)
                if (sphere.contributions.Count > 0 && sphere.contributions[0].element == element) return sphere.resourceVoice;
            return null;
        }
        public void Validate()
        {
            var ids = new HashSet<int>();
            foreach (var sphere in spheres)
            {
                if (!sphere || !ids.Add(sphere.id)) throw new InvalidOperationException("Missing or duplicate sphere definition");
                sphere.Validate();
            }
            if (openingSpheres == null || openingSpheres.Length != 2 || openingSpheres[0] == openingSpheres[1])
                throw new InvalidOperationException("Opening level needs two distinct spheres");
            foreach (int id in openingSpheres)
                if (!Get(id).AmmoEligible || !Get(id).availableAtStart) throw new InvalidOperationException("Invalid opening sphere");
            if (!recipeBook) throw new InvalidOperationException("Missing recipe book");
            recipeBook.Validate();
            foreach (var recipe in recipeBook.recipes)
                if (Get(recipe.output.id) != recipe.output) throw new InvalidOperationException("Recipe output is not the catalog's sphere definition");
        }
    }
}
