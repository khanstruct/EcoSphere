using UnityEngine;

namespace EcoSphere
{
    [CreateAssetMenu(menuName = "EcoSphere/World Modifier")]
    public class WorldModifierDefinition : ScriptableObject
    {
        public string id, displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Range(0, 1)] public float proceduralChance = .25f;
        [Min(0),Tooltip("Optional guaranteed system; zero uses ordinary placement only.")]
        public int guaranteedSystem;
        [Min(0),Tooltip("Zero-based world slot for guaranteed placement.")]
        public int guaranteedSlot;
        public virtual void Validate()
        {
            if(string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName) || !icon ||
                float.IsNaN(proceduralChance) || proceduralChance<0 || proceduralChance>1 || guaranteedSystem<0 || guaranteedSlot<0)
                throw new System.InvalidOperationException("Invalid modifier: "+name);
        }
    }
}
