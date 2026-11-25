using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    [Serializable]
    public sealed class ElementRecipe
    {
        public ElementId first, second;
        public SphereDefinition output;
    }

    [CreateAssetMenu(menuName = "EcoSphere/Element Recipe Book")]
    public sealed class ElementRecipeBook : ScriptableObject
    {
        public List<ElementRecipe> recipes = new List<ElementRecipe>();
        Dictionary<ElementPair,ElementRecipe> index;
        List<ElementRecipe> indexedRecipes;
        int indexedCount=-1;
        void OnEnable()=>InvalidateIndex();
        void OnValidate()=>InvalidateIndex();
        public void InvalidateIndex(){index=null;indexedRecipes=null;indexedCount=-1;}
        void EnsureIndex()
        {
            if(recipes==null)throw new InvalidOperationException(name+": missing recipes");
            if(index!=null && ReferenceEquals(indexedRecipes,recipes) && indexedCount==recipes.Count)return;
            var next=new Dictionary<ElementPair,ElementRecipe>(recipes.Count);
            foreach(var recipe in recipes)
            {
                if(recipe==null)throw new InvalidOperationException(name+": null recipe");
                var key=new ElementPair(recipe.first,recipe.second);
                if(!next.TryAdd(key,recipe))throw new InvalidOperationException(name+": duplicate recipe for "+key);
            }
            index=next;indexedRecipes=recipes;indexedCount=recipes.Count;
        }
        public bool TryCombine(ElementId first,ElementId second,out SphereDefinition result)
        {
            EnsureIndex();
            if(index.TryGetValue(new ElementPair(first,second),out var recipe)){result=recipe.output;return true;}
            result=null;return false;
        }
        // Element shares alone decide: completion only happens on a cleared board, so totals are always full-board amounts.
        public static bool Qualifies(ElementRecipe recipe, ElementStandings standings)
        {
            return standings.IsLeading(recipe.first) && standings.IsLeading(recipe.second) &&
                (recipe.first != recipe.second || standings.CanSelfCombine(recipe.first));
        }

        public void Validate()
        {
            InvalidateIndex();
            if(recipes==null)throw new InvalidOperationException(name+": missing recipes");
            foreach (var recipe in recipes)
            {
                if (recipe == null || !Enum.IsDefined(typeof(ElementId), recipe.first) ||
                    !Enum.IsDefined(typeof(ElementId), recipe.second) || !recipe.output || !recipe.output.AmmoEligible)
                    throw new InvalidOperationException(name + ": invalid recipe");

            }
            EnsureIndex();
        }
    }
}
