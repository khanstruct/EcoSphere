using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed class LevelAudio : MonoBehaviour
    {
        public AudioClip music, shoot, ricochet, match, breakaway;
        public AudioClip laser;
        [Header("Announcer")]
        public AudioClip worldComplete, creationFailed;
        public AudioClip cryosphere, heliosphere;
        public AudioClip impressive;
        public AudioClip ecosphere;
        public const int MixedResourceCallout = -2;
        [Range(0, 1)] public float voiceVolume = 1f;
        [Range(0, 1)] public float musicVolume = .18f;
        [Range(0, 1)] public float effectsVolume = .85f;
        [Range(0, 1)] public float shootVolume = 1f;
        [Range(.5f, 1.5f)] public float shootPitch = .9f;
        AudioSource musicSource, effectsSource, resolutionSource, shootSource, voiceSource;
        bool resultAnnounced;
        static readonly System.Random calloutRandom = new System.Random();
        readonly Queue<AudioClip> voiceQueue = new Queue<AudioClip>();
        Coroutine voiceRoutine;
        public bool IsAnnouncing => voiceRoutine != null || voiceQueue.Count > 0 || (voiceSource && voiceSource.isPlaying);
        float shootEmphasisUntil, backgroundGain = 1;

        public void Initialize()
        {
            musicSource = CreateSource("Background music", musicVolume);
            effectsSource = CreateSource("Shot and ricochet effects", effectsVolume);
            resolutionSource = CreateSource("Match and breakaway effects", effectsVolume);
            shootSource = CreateSource("Cannon shot", shootVolume);
            voiceSource = CreateSource("Level result announcer", voiceVolume);
            voiceSource.priority = 0;
            if (worldComplete) worldComplete.LoadAudioData();
            if (creationFailed) creationFailed.LoadAudioData();
            if (cryosphere) cryosphere.LoadAudioData();
            if (heliosphere) heliosphere.LoadAudioData();
            if (impressive) impressive.LoadAudioData();
            if (ecosphere) ecosphere.LoadAudioData();
            shootSource.priority = 32;
            shootSource.pitch = shootPitch;
            if (shoot) shoot.LoadAudioData();
            if (laser) laser.LoadAudioData();
            musicSource.clip = music;
            musicSource.loop = true;
            musicSource.priority = 160;
            if (music) musicSource.Play();
        }

        AudioSource CreateSource(string label, float volume)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = volume;
            return source;
        }

        public void Shoot()
        {
            if (!shoot || !shootSource) return;
            shootSource.volume = shootVolume;
            shootSource.pitch = shootPitch;
            // Briefly make room for the attack; other sounds continue instead of being interrupted.
            shootEmphasisUntil = Time.unscaledTime + .22f;
            backgroundGain = .4f;
            ApplyVolumes();
            shootSource.PlayOneShot(shoot);
        }
        public void Ricochet() { if (ricochet) effectsSource.PlayOneShot(ricochet); }

        public void FireLaser()
        {
            if (laser && effectsSource) effectsSource.PlayOneShot(laser);
        }

        public void Resolve(int matched, int released)
        {
            if (matched > 0) StartCoroutine(MatchThenBreakaway(released > 0));
        }

        public void CancelResolution()
        {
            StopAllCoroutines();
            voiceRoutine = null; voiceQueue.Clear();
            if (voiceSource) voiceSource.Stop();
            if (resolutionSource) resolutionSource.Stop();
        }

        public void AnnounceResult(bool won)
        {
            if (resultAnnounced || !voiceSource) return;
            resultAnnounced = true;
            if (!won)
            {
                if (voiceRoutine != null) StopCoroutine(voiceRoutine);
                voiceRoutine = null; voiceQueue.Clear(); voiceSource.Stop();
            }
            QueueVoice(won ? worldComplete : creationFailed);
        }

        public static int ResourceCallout(IReadOnlyList<int> gains)
        {
            int selected = -1;
            for (int i = 0; i < gains.Count; i++)
            {
                if (gains[i] < ElementScoring.Inclusion) continue;
                if (selected >= 0) return MixedResourceCallout;
                selected = i;
            }
            return selected;
        }

        public bool AnnounceResources(int iceAdded, int sunlightAdded)
        {
            if (resultAnnounced) return false;
            int selected = ResourceCallout(new[] { iceAdded, sunlightAdded });
            if (selected == -1) return false;
            QueueVoice(selected == MixedResourceCallout ? ecosphere : selected == 0 ? cryosphere : heliosphere);
            return true;
        }

        public static float ImpressiveChance(int matched, int ricochets, bool otherVoiceSelected) =>
            matched < 3 || ricochets <= 0 || otherVoiceSelected ? 0 : 1 - Mathf.Pow(.8f, ricochets);

        public bool TryImpressive(int matched, int ricochets, bool otherVoiceSelected)
        {
            if (!impressive || resultAnnounced || !voiceSource || voiceRoutine != null || voiceSource.isPlaying) return false;
            float chance = ImpressiveChance(matched, ricochets, otherVoiceSelected);
            if (chance <= 0 || calloutRandom.NextDouble() >= chance) return false;
            QueueVoice(impressive);
            return true;
        }

        void QueueVoice(AudioClip clip)
        {
            if (!clip || !voiceSource) return;
            voiceQueue.Enqueue(clip);
            if (voiceRoutine == null) voiceRoutine = StartCoroutine(PlayVoices());
        }

        IEnumerator PlayVoices()
        {
            while (voiceQueue.Count > 0)
            {
                voiceSource.clip = voiceQueue.Dequeue(); voiceSource.Play();
                // Allow playback to start, then follow the source rather than a clip-length estimate.
                yield return null;
                while (voiceSource && voiceSource.isPlaying) yield return null;
            }
            voiceRoutine = null;
        }

        public IEnumerator WaitForAnnouncements()
        {
            while (IsAnnouncing) yield return null;
        }

        IEnumerator MatchThenBreakaway(bool hasBreakaway)
        {
            if (match)
            {
                resolutionSource.PlayOneShot(match);
                // Finish the actual match clip before playing the additional release cue.
                yield return new WaitForSeconds(match.length);
            }
            if (hasBreakaway && breakaway) resolutionSource.PlayOneShot(breakaway);
        }

        void Update()
        {
            if (Time.unscaledTime >= shootEmphasisUntil)
                backgroundGain = Mathf.MoveTowards(backgroundGain, 1, Time.unscaledDeltaTime / .12f);
            ApplyVolumes();
        }

        void ApplyVolumes()
        {
            float voiceGain = voiceSource && voiceSource.isPlaying ? .35f : 1;
            if (musicSource) musicSource.volume = musicVolume * backgroundGain * voiceGain;
            if (effectsSource) effectsSource.volume = effectsVolume * backgroundGain * voiceGain;
            if (resolutionSource) resolutionSource.volume = effectsVolume * backgroundGain * voiceGain;
            if (shootSource) shootSource.volume = shootVolume;
            if (voiceSource) voiceSource.volume = voiceVolume;
        }
    }
}
