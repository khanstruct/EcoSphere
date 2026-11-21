using UnityEngine;
namespace EcoSphere
{
    [CreateAssetMenu(menuName="EcoSphere/Recipe Reveal Style")]
    public sealed class RecipeRevealStyle : ScriptableObject
    {
        public Material sphereMaterial, flareMaterial;
        [Header("Combination phase weights (scaled to the audio clip length)")]
        [Min(.1f)] public float ingredientHold=.5f, mergeDuration=.9f, volatileDuration=.65f, flashDuration=.22f, revealDuration=.5f;
        [Header("After-reveal timings (seconds)")]
        [Min(.1f)] public float revealHold=.35f, collectDuration=.8f;
        [Range(0,.3f)] public float screenWash=.16f;
        [Range(0,8)] public float shakeDistance=3;
    }
}
