using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed class MenuMusic : MonoBehaviour
    {
        static MenuMusic instance;
        public AudioClip clip;
        [Range(0, 1)] public float volume = .18f;
        public AudioSource Source { get; private set; }

        void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Source = GameServices.Audio.CreateSource(gameObject,"Menu music",AudioChannel.Music,volume);
            Source.playOnAwake = false;
            Source.clip = clip;

            Source.loop = true;
            Source.spatialBlend = 0;
            Source.priority = 160;
            if (clip) Source.Play();
            SceneManager.sceneLoaded += SceneLoaded;
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "System" || scene.name == "LevelPrep") return;
            Source.Stop();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (instance == this) instance = null;
        }
    }
}
