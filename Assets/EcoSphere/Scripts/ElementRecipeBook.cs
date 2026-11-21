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
        public bool TryCombine(ElementId first, ElementId second, out SphereDefinition result)
        {
            foreach (var recipe in recipes)
                if ((recipe.first == first && recipe.second == second) || (recipe.first == second && recipe.second == first))
                {
                    result = recipe.output;
                    return true;
                }
            result = default;
            return false;
        }

        public static bool Qualifies(ElementRecipe recipe, ElementStandings standings, int minimum)
        {
            return standings.MeetsRecipeMinimum(recipe.first,minimum) && standings.MeetsRecipeMinimum(recipe.second,minimum) &&
                (recipe.first != recipe.second || standings.CanSelfCombine(recipe.first));
        }

        public void Validate()
        {
            var pairs = new HashSet<string>();
            foreach (var recipe in recipes)
            {
                if (recipe == null || !Enum.IsDefined(typeof(ElementId), recipe.first) ||
                    !Enum.IsDefined(typeof(ElementId), recipe.second) || !recipe.output || !recipe.output.selectable)
                    throw new InvalidOperationException(name + ": invalid recipe");
                string key = Mathf.Min((int)recipe.first, (int)recipe.second) + ":" + Mathf.Max((int)recipe.first, (int)recipe.second);
                if (!pairs.Add(key)) throw new InvalidOperationException(name + ": duplicate recipe for " + key);
            }
        }
    }
}
