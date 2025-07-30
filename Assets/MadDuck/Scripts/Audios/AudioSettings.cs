using System;
using FMOD.Studio;
using FMODUnity;
using MadDuck.Scripts.Managers;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Audios
{
    [Serializable]
    public record BusData(string Path, bool IsMaster = false, float LinearVolume = 1f, bool IsMuted = false)
    {
        [field: SerializeField, DisableInPlayMode] public string Path { get; private set; } = Path;
        
        [field: SerializeField, DisableInPlayMode] public bool IsMaster { get; private set; } = IsMaster;

        [field: SerializeField, PropertyRange(AudioManagerUtils.MinLinearVolume, AudioManagerUtils.MaxLinearVolume), 
                OnValueChanged(nameof(LinearChanged))]
        public float LinearVolume { get; private set; } = LinearVolume;
        
        [field: SerializeField, PropertyRange(0f, 1f),
                OnValueChanged(nameof(Linear01Changed))]
        public float Linear01Volume { get; private set; }

        [field: SerializeField, PropertyRange(AudioManagerUtils.MinDecibelVolume, AudioManagerUtils.MaxDecibelVolume), 
                OnValueChanged(nameof(DecibelChanged))]
        public float DecibelVolume { get; private set; }
        
        [field: SerializeField, PropertyRange(0f, 1f),
                OnValueChanged(nameof(Decibel01Changed))]
        public float Decibel01Volume { get; private set; }
        [field: SerializeField, OnValueChanged(nameof(MuteChanged))] public bool IsMuted { get; private set; } = IsMuted;
        
        public Bus Bus { get; private set; }

        private float _beforeMuteVolume;

        public void Initialize()
        {
            var bus = RuntimeManager.GetBus(Path);
            if (!bus.isValid())
            {
                Debug.LogError($"Bus with path {Path} not found. Please check the FMOD settings.");
                return;
            }
            Bus = bus;
            Bus.getVolume(out _beforeMuteVolume);
            SetVolume(LinearVolume);
            SetMute(IsMuted);
        }
        
        public void SetMute(bool mute)
        {
            if (!Bus.isValid()) return;
            if (IsMaster)
                RuntimeManager.MuteAllEvents(mute);
            else
            {
                #if UNITY_WEBGL
                var webGLMute = mute ? 0f : _beforeMuteVolume;
                Bus.setVolume(webGLMute);
                #else
                Bus.setMute(mute);
                #endif
            }
                
            IsMuted = mute;
        }

        public void SetVolume(float volume, VolumeUnit unit = VolumeUnit.Linear)
        {
            if (!Bus.isValid()) return;
            switch (unit)
            {
                case VolumeUnit.Linear:
                    LinearVolume = volume;
                    LinearChanged();
                    break;
                case VolumeUnit.Decibel:
                    DecibelVolume = volume;
                    DecibelChanged();
                    break;
                case VolumeUnit.Linear01:
                    Linear01Volume = volume;
                    Linear01Changed();
                    break;
                case VolumeUnit.Decibel01:
                    Decibel01Volume = volume;
                    Decibel01Changed();
                    break;
            }
            var linear = volume.ConvertUnit(unit, VolumeUnit.Linear);
            SetVolumeLinear(linear);
        }

        private void SetVolumeLinear(float linear)
        {
            if (!Bus.isValid()) return;
            Bus.setVolume(linear);
            _beforeMuteVolume = linear;
        }
        private void LinearChanged()
        {
            DecibelVolume = LinearVolume.ConvertUnit(VolumeUnit.Linear, VolumeUnit.Decibel);
            Linear01Volume = LinearVolume.ConvertUnit(VolumeUnit.Linear, VolumeUnit.Linear01);
            Decibel01Volume = LinearVolume.ConvertUnit(VolumeUnit.Linear, VolumeUnit.Decibel01);
            SetVolumeLinear(LinearVolume);
        }
        
        private void DecibelChanged()
        {
            LinearVolume = DecibelVolume.ConvertUnit(VolumeUnit.Decibel, VolumeUnit.Linear);
            Linear01Volume = DecibelVolume.ConvertUnit(VolumeUnit.Decibel, VolumeUnit.Linear01);
            Decibel01Volume = DecibelVolume.ConvertUnit(VolumeUnit.Decibel, VolumeUnit.Decibel01);
            SetVolumeLinear(LinearVolume);
        }
        
        private void Linear01Changed()
        {
            LinearVolume = Linear01Volume.ConvertUnit(VolumeUnit.Linear01, VolumeUnit.Linear);
            DecibelVolume = Linear01Volume.ConvertUnit(VolumeUnit.Linear01, VolumeUnit.Decibel);
            Decibel01Volume = Linear01Volume.ConvertUnit(VolumeUnit.Linear01, VolumeUnit.Decibel01);
            SetVolumeLinear(LinearVolume);
        }
        
        private void Decibel01Changed()
        {
            DecibelVolume = Decibel01Volume.ConvertUnit(VolumeUnit.Decibel01, VolumeUnit.Decibel);
            LinearVolume = Decibel01Volume.ConvertUnit(VolumeUnit.Decibel01, VolumeUnit.Linear);
            Linear01Volume = Decibel01Volume.ConvertUnit(VolumeUnit.Decibel01, VolumeUnit.Linear01);
            SetVolumeLinear(LinearVolume);
        }
        
        private void MuteChanged()
        {
            SetMute(IsMuted);
        }
    }

    [CreateAssetMenu(fileName = "AudioSettings", menuName = "MadDuck/Audio/AudioSettings", order = 1)]
    public class AudioSettings : ScriptableObject
    {
        [field: SerializeField, HideReferenceObjectPicker]
        public SerializableDictionary<BusType, BusData> BusData { get; private set; } = new()
        {
            { BusType.Master, new BusData("bus:/", true) },
            { BusType.BGM, new BusData("bus:/BGM") },
            { BusType.SFX, new BusData("bus:/SFX") },
        };
    }
}