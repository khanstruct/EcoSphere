using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    [DisallowMultipleComponent]
    public sealed class LevelAudio : MonoBehaviour
    {
        public GameAudioSettings Settings => GameAudioSettings.Current;
        public AudioClip music => Settings.music;
        public AudioClip shoot => Settings.shoot;
        public AudioClip ricochet => Settings.ricochet;
        public AudioClip match => Settings.match;
        public AudioClip breakaway => Settings.breakaway;
        public AudioClip miss => Settings.miss;
        public AudioClip laser => Settings.laser;
        public AudioClip worldComplete => Settings.worldComplete;
        public AudioClip creationFailed => Settings.creationFailed;
        public AudioClip impressive => Settings.impressive;
        public AudioClip ecosphere => Settings.ecosphere;
        public const int MixedResourceCallout = -2;
        public static int SphereVoiceMinimumPoints
        {
            get{int count=Mathf.Max(3,GameAudioSettings.Current.minimumMatchedSpheresForVoice);return count*ElementScoring.MatchRate(count);}
        }
        [Range(0, 1)] public float voiceVolume = 1f;
        [Range(0, 1)] public float musicVolume = .18f;
        [Range(0, 1)] public float effectsVolume = .85f;
        [Range(0, 1)] public float shootVolume = 1f;
        [Range(.5f, 1.5f)] public float shootPitch = .9f;
        AudioSource musicSource, effectsSource, resolutionSource, shootSource, voiceSource;
        bool resultAnnounced;
        bool initialized;
        static readonly System.Random calloutRandom = new System.Random();
        readonly Queue<AudioClip> voiceQueue = new Queue<AudioClip>();
        Coroutine voiceRoutine;
        public bool IsAnnouncing => voiceRoutine != null || voiceQueue.Count > 0 || (voiceSource && voiceSource.isPlaying);
        float shootEmphasisUntil, backgroundGain = 1;

        public void Initialize()
        {
            if (initialized) return;
            Settings.Validate();
            initialized = true;
            musicSource = CreateSource("Background music", musicVolume,AudioChannel.Music);
            effectsSource = CreateSource("Shot and ricochet effects", effectsVolume);
            resolutionSource = CreateSource("Match and breakaway effects", effectsVolume);
            shootSource = CreateSource("Cannon shot", shootVolume);
            voiceSource = CreateSource("Level result announcer", voiceVolume,AudioChannel.Voice);
            voiceSource.priority = 0;
            if (worldComplete) worldComplete.LoadAudioData();
            if (creationFailed) creationFailed.LoadAudioData();
            foreach (var definition in SphereCatalog.Current.spheres)
            {
                if (definition.resourceVoice) definition.resourceVoice.LoadAudioData();
                if (definition.deflectSound) definition.deflectSound.LoadAudioData();
                if (definition.breakSound) definition.breakSound.LoadAudioData();
                if (definition.erodeSound) definition.erodeSound.LoadAudioData();
            }
            if (impressive) impressive.LoadAudioData();
            if (ecosphere) ecosphere.LoadAudioData();
            shootSource.priority = 32;
            shootSource.pitch = shootPitch;
            if (shoot) shoot.LoadAudioData();
            if (laser) laser.LoadAudioData();
            if (miss) miss.LoadAudioData();
            musicSource.clip = music;
            musicSource.loop = true;
            musicSource.priority = 160;
            if (music) musicSource.Play();
        }

        AudioSource CreateSource(string label, float volume,AudioChannel channel=AudioChannel.Effects)
        {
            return GameServices.Audio.CreateSource(gameObject,label,channel,volume);
        }
        public void Shoot()
        {
            if (!shoot || !shootSource) return;
            shootSource.volume = GameServices.Audio.Gain(AudioChannel.Effects,shootVolume);
            shootSource.pitch = shootPitch;
            // Briefly make room for the attack; other sounds continue instead of being interrupted.
            shootEmphasisUntil = Time.unscaledTime + .22f;
            backgroundGain = .4f;
            ApplyVolumes();
            shootSource.PlayOneShot(shoot);
        }
        public void Ricochet() => PlayEffect(ricochet);
        public void AmmoSwap() { if (Settings.ammoSwap && effectsSource) effectsSource.PlayOneShot(Settings.ammoSwap, Settings.ammoSwapVolume); }
        public void Miss() => PlayEffect(miss);
        public void AlienHit(AudioClip clip)=>PlayEffect(clip);
        // Every one-shot gameplay effect goes through here; EffectPlayed reports what was actually played.
        public event System.Action<AudioClip> EffectPlayed;
        public void PlayEffect(AudioClip clip){if(!clip || !effectsSource)return;effectsSource.PlayOneShot(clip);EffectPlayed?.Invoke(clip);}

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
            var precise = new double[gains.Count];
            for (int i=0;i<precise.Length;i++) precise[i]=gains[i];
            return ResourceCallout(precise);
        }

        public static int ResourceCallout(IReadOnlyList<double> gains)
        {
            int selected = -1;
            for (int i = 0; i < gains.Count; i++)
            {
                if (gains[i] < SphereVoiceMinimumPoints) continue;
                if (selected >= 0) return MixedResourceCallout;
                selected = i;
            }
            return selected;
        }

        public bool AnnounceResources(int iceAdded, int sunlightAdded)
            => AnnounceResources(new double[] { iceAdded, sunlightAdded });

        public static AudioClip MatchVoice(int sphereType, int matchedCount) =>
            matchedCount >= Mathf.Max(3,GameAudioSettings.Current.minimumMatchedSpheresForVoice)
                ? SphereCatalog.Current.Get(sphereType).resourceVoice : null;

        public bool AnnounceMatch(int sphereType, int matchedCount)
        {
            if (resultAnnounced) return false;
            var clip = MatchVoice(sphereType, matchedCount);
            if (!clip) return false;
            QueueVoice(clip);
            return true;
        }

        public static AudioClip ResolutionVoice(IReadOnlyDictionary<int,int> spherePoints)
        {
            AudioClip selected=null;int minimum=SphereVoiceMinimumPoints;
            foreach(var pair in spherePoints)
            {
                if(pair.Value<minimum)continue;
                var definition=SphereCatalog.Current.Get(pair.Key);
                if(!definition.Matchable || !definition.resourceVoice)continue;
                if(selected)return GameAudioSettings.Current.ecosphere;
                selected=definition.resourceVoice;
            }
            return selected;
        }
        public bool AnnounceResolution(IReadOnlyDictionary<int,int> spherePoints)
        {
            if(resultAnnounced)return false;
            var clip=ResolutionVoice(spherePoints);
            if(!clip)return false;
            QueueVoice(clip);return true;
        }

        public bool AnnounceResources(IReadOnlyList<double> gains)
        {
            if (resultAnnounced) return false;
            int selected = ResourceCallout(gains);
            if (selected == -1) return false;
            AudioClip clip = selected == MixedResourceCallout ? ecosphere : SphereCatalog.Current.ElementVoice((ElementId)selected);
            if (!clip) return false;
            QueueVoice(clip);
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

        // Announcements queue after any voice already playing. VoiceQueued reports each queued clip (used by checks).
        public event System.Action<AudioClip> VoiceQueued;
        public void Announce(AudioClip clip) => QueueVoice(clip);
        void QueueVoice(AudioClip clip)
        {
            if (!clip || !voiceSource) return;
            voiceQueue.Enqueue(clip); VoiceQueued?.Invoke(clip);
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
            if (musicSource) musicSource.volume = GameServices.Audio.Gain(AudioChannel.Music,musicVolume) * backgroundGain * voiceGain;
            if (effectsSource) effectsSource.volume = GameServices.Audio.Gain(AudioChannel.Effects,effectsVolume) * backgroundGain * voiceGain;
            if (resolutionSource) resolutionSource.volume = GameServices.Audio.Gain(AudioChannel.Effects,effectsVolume) * backgroundGain * voiceGain;
            if (shootSource) shootSource.volume = GameServices.Audio.Gain(AudioChannel.Effects,shootVolume);
            if (voiceSource) voiceSource.volume = GameServices.Audio.Gain(AudioChannel.Voice,voiceVolume);
        }
    }
}
