using System;
using UnityEngine;

namespace EcoSphere
{
    [CreateAssetMenu(menuName = "EcoSphere/Game Audio Settings")]
    public sealed class GameAudioSettings : ScriptableObject
    {
        public AudioClip music, shoot, ricochet, match, breakaway, miss, laser;
        public AudioClip worldComplete, creationFailed, impressive, ecosphere;
        public AudioClip sphereCombine, starScore, ammoSwap;
        [Range(0,1),Tooltip("Ammo swap loudness relative to the effects channel. The swap clip is mastered ~10 dB above the cannon shot.")]
        public float ammoSwapVolume=.3f;
        [Min(3),Tooltip("Match size setting the voice threshold. Equivalent earned points from matches/breakaways qualify each sphere type; multiple types use EcoSphere. Independent of recipes.")]
        public int minimumMatchedSpheresForVoice=6;
        static GameAudioSettings current;
        public static GameAudioSettings Current
        {
            get
            {
                if (!current) current = Resources.Load<GameAudioSettings>("GameAudioSettings");
                if (!current) throw new InvalidOperationException("Missing shared GameAudioSettings asset");
                return current;
            }
        }

        public void Validate()
        {
            if(minimumMatchedSpheresForVoice<3)throw new InvalidOperationException("Sphere voices require at least three matched spheres");
            if (!music || !shoot || !ricochet || !match || !breakaway || !miss || !laser ||
                !worldComplete || !creationFailed || !impressive || !ecosphere || !sphereCombine || !ammoSwap)
                throw new InvalidOperationException("Shared game audio settings contain an unassigned clip");
        }
    }
}
