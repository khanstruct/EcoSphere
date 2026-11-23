using UnityEngine;

namespace EcoSphere
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class UIClickAudio : MonoBehaviour
    {
        static UIClickAudio instance;
        AudioSource source;
        public static AudioSource Source => instance ? instance.source : null;
        public static int PlayCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState() { instance = null; PlayCount = 0; }

        void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this;
            source = GetComponent<AudioSource>();
            GameServices.Audio.Configure(source,AudioChannel.UI,source.volume,true);
            DontDestroyOnLoad(gameObject);
        }

        public static void Play()
        {
            if (!instance)
            {
                var prefab = Resources.Load<GameObject>("UIAudio");
                if (!prefab) return;
                Instantiate(prefab);
            }
            if (!instance || !instance.source.clip) return;
            instance.source.PlayOneShot(instance.source.clip);
            PlayCount++;
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
