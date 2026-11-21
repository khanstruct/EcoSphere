using System;
using UnityEngine;

namespace EcoSphere
{
    [CreateAssetMenu(menuName = "EcoSphere/Game Audio Settings")]
    public sealed class GameAudioSettings : ScriptableObject
    {
        public AudioClip music, shoot, ricochet, match, breakaway, miss, laser;
        public AudioClip worldComplete, creationFailed, impressive, ecosphere;
        public AudioClip sphereCombine;
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
            if (!music || !shoot || !ricochet || !match || !breakaway || !miss || !laser ||
                !worldComplete || !creationFailed || !impressive || !ecosphere || !sphereCombine)
                throw new InvalidOperationException("Shared game audio settings contain an unassigned clip");
        }
    }
}
