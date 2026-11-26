using UnityEngine;

namespace EcoSphere
{
    // Per-sphere damage lives on the board sphere itself, so it leaves with the sphere however it is removed.
    // Future cracked-crystal art can read Hits from here.
    public sealed class SphereDamage : MonoBehaviour
    {
        public int Hits { get; private set; }
        public static int HitsOn(GameObject sphere) => sphere && sphere.TryGetComponent(out SphereDamage damage) ? damage.Hits : 0;
        public static int Strike(GameObject sphere)
        {
            if (!sphere.TryGetComponent(out SphereDamage damage)) damage = sphere.AddComponent<SphereDamage>();
            return ++damage.Hits;
        }
    }
}
