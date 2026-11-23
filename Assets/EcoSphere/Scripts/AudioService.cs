using System;
using UnityEngine;
namespace EcoSphere
{
    public enum AudioChannel { Music, Effects, Voice, UI }
    [Serializable] public sealed class AudioPreferences
    {
        [Range(0,1)] public float master=1,music=1,effects=1,voice=1,ui=1;
    }
    public sealed class AudioService
    {
        public float Gain(AudioChannel channel,float localVolume=1)
        {
            var settings=GameServices.Profile.Active.audio;
            float gain=channel==AudioChannel.Music ? settings.music : channel==AudioChannel.Voice ? settings.voice : channel==AudioChannel.UI ? settings.ui : settings.effects;
            return Mathf.Clamp01(localVolume)*Mathf.Clamp01(settings.master)*Mathf.Clamp01(gain);
        }
        public AudioSource CreateSource(GameObject owner,string name,AudioChannel channel,float volume=1,bool ignorePause=false)
        {
            var child=new GameObject(name);child.transform.SetParent(owner.transform,false);
            var source=child.AddComponent<AudioSource>();Configure(source,channel,volume,ignorePause);return source;
        }
        public void Configure(AudioSource source,AudioChannel channel,float volume=1,bool ignorePause=false)
        {source.playOnAwake=false;source.spatialBlend=0;source.volume=Gain(channel,volume);source.ignoreListenerPause=ignorePause;}
    }
}
