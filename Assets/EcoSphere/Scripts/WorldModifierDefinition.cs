using UnityEngine;

namespace EcoSphere
{
    [CreateAssetMenu(menuName = "EcoSphere/World Modifier")]
    public sealed class WorldModifierDefinition : ScriptableObject
    {
        public string id, displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Range(0, 1)] public float proceduralChance = .25f;
    }
}
