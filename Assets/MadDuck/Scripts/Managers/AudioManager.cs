using System;
using System.Collections.Generic;
using System.Linq;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using MadDuck.Scripts.Audios;
using MessagePipe;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using AudioSettings = MadDuck.Scripts.Audios.AudioSettings;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace MadDuck.Scripts.Managers
{
    #region Data Structures
    public interface IAudioIdentifier
    {
        public Type GetIdentifierType();
        public bool TryGetIdentifier<TId>(out TId identifier);
    }

    public interface IAudioIdentifier<T> : IAudioIdentifier
    {
        public T Identifier { get; set; }
    }
    
    public record AudioIdentifier<T> : IAudioIdentifier<T>
    {
        public T Identifier { get; set; }
        
        public AudioIdentifier(T identifier)
        {
            Identifier = identifier;
        }
        
        public Type GetIdentifierType()
        {
            return typeof(T);
        }
        
        public bool TryGetIdentifier<TId>(out TId identifier)
        {
            if (typeof(TId) == GetIdentifierType())
            {
                identifier = (TId)(object)Identifier;
                return true;
            }
            identifier = default;
            return false;
        }
    }
    
    public record AudioReference
    {
        public EventInstance eventInstance;
        public readonly IAudioIdentifier identifier;
        
        public AudioReference(EventInstance eventInstance, IAudioIdentifier identifier = null)
        {
            this.eventInstance = eventInstance;
            this.identifier = identifier;
        }
    }
    #endregion

    #region Events

    // public enum AudioTargetType
    // {
    //     Identifier,
    //     Indexed,
    //     Wild,
    //     All
    // }
    //
    // public struct AudioPlayEvent
    // {
    //     public EventReference eventReference;
    //     public Vector3 position;
    //     public IAudioIdentifier identifier;
    //
    //     public AudioPlayEvent(EventReference eventReference, Vector3 position, IAudioIdentifier identifier)
    //     {
    //         this.eventReference = eventReference;
    //         this.position = position;
    //         this.identifier = identifier;
    //     }
    // }
    #endregion

    public enum BusType
    {
        Master,
        BGM,
        SFX,
    }
    
    public enum VolumeUnit
    {
        Linear,
        Linear01,
        Decibel,
        Decibel01
    }
    
    public class AudioManager : PersistentMonoSingleton<AudioManager>
    {
        [Title("References")] 
        [Required, SerializeField] private AudioSettings audioSettings;

        [Title("Settings")] 
        [SerializeField] private bool limitAudio = true;
        [SerializeField] private int maxAudioCount = 50;

        private readonly Dictionary<IAudioIdentifier, List<AudioReference>> _indexedAudioReferenceData = new();
        private readonly List<AudioReference> _wildAudioReferenceData = new();

        #region Life Cycle
        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }
        #endregion

        #region Events
        private void Subscribe()
        {
            
        }
        
        private void Unsubscribe()
        {
            
        }
        #endregion
        
        #region Initialization

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
             audioSettings.BusData.Values.ForEach(busData => busData.Initialize());
        }
        #endregion

        #region Play
        public AudioReference PlayAudio(EventReference eventReference, Vector3 position, IAudioIdentifier id = null, Transform parent = null)
        {
            if (limitAudio && _wildAudioReferenceData.Count + 
                _indexedAudioReferenceData.Values.Sum(references => references.Count) >= maxAudioCount)
            {
                Debug.LogWarning("Max audio count reached, not playing new audio.");
                return null;
            }
            EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
            eventInstance.set3DAttributes(position.To3DAttributes());
            eventInstance.start();
            if (parent)
                RuntimeManager.AttachInstanceToGameObject(eventInstance, parent.gameObject);
            var audioReference = new AudioReference(eventInstance, id);
            if (id != null)
            {
                if (_indexedAudioReferenceData.TryGetValue(id, out var audioReferences))
                {
                    audioReferences.Add(audioReference);
                }
                else
                {
                    _indexedAudioReferenceData[id] = new List<AudioReference> { audioReference };
                }
            }
            else
            {
                _wildAudioReferenceData.Add(audioReference);
            }
            return audioReference;
        }
        
        public void PlayAudioOneShot(EventReference eventReference, Vector3 position)
        {
            RuntimeManager.PlayOneShot(eventReference, position);
        }
        #endregion

        #region Pause/Resume
        public void SetPauseAudio(AudioReference audioReference, bool pause)
        {
            if (audioReference == null) return;
            var eventInstance = audioReference.eventInstance;
            if (!eventInstance.isValid()) return;
            eventInstance.setPaused(pause);
        }
        
        public void SetPauseAllAudioInIdentifier(IAudioIdentifier id, bool pause)
        {
            if (!_indexedAudioReferenceData.TryGetValue(id, out var audioReferences)) return;
            foreach (var audioReference in audioReferences)
            {
                SetPauseAudio(audioReference, pause);
            }
        }
        
        public void SetPauseAllIndexedAudio(bool pause)
        {
            var audioReferences = _indexedAudioReferenceData.Values
                .SelectMany(references => references);
            foreach (var audioReference in audioReferences)
            {
                SetPauseAudio(audioReference, pause);
            }
        }
        
        public void SetPauseAllWildAudio(bool pause)
        {
            foreach (var audioReference in _wildAudioReferenceData)
            {
                SetPauseAudio(audioReference, pause);
            }
        }
        
        public void SetPauseAllAudio(bool pause)
        {
            SetPauseAllIndexedAudio(pause);
            SetPauseAllWildAudio(pause);
        }
        #endregion
        
        #region Stop
        public void StopAudio(AudioReference audioReference, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            if (audioReference == null) return;
            var eventInstance = audioReference.eventInstance;
            if (!eventInstance.isValid()) return;
            if (audioReference.identifier != null)
            {
                var key = audioReference.identifier;
                if (_indexedAudioReferenceData.TryGetValue(key, out var audioReferences))
                {
                    audioReferences.Remove(audioReference);
                    if (audioReferences.Count == 0)
                    {
                        _indexedAudioReferenceData.Remove(key);
                    }
                }
            }
            else
            {
                _wildAudioReferenceData.Remove(audioReference);
            }
            eventInstance.stop(stopMode);
            eventInstance.release();
        }

        public void StopAllAudioInIdentifier(IAudioIdentifier id)
        {
            if (!_indexedAudioReferenceData.TryGetValue(id, out var audioReferences)) return;
            foreach (var audioReference in audioReferences)
            {
                StopAudio(audioReference);
            }
        }

        public void StopAllIndexedAudio()
        {
            var audioReferences = _indexedAudioReferenceData.Values
                .SelectMany(references => references);
            foreach (var audioReference in audioReferences)
            {
                StopAudio(audioReference);
            }
        }
        
        public void StopAllWildAudio()
        {
            foreach (var audioReference in _wildAudioReferenceData)
            {
                StopAudio(audioReference);
            }
        }
        
        public void StopAllAudio()
        {
            StopAllIndexedAudio();
            StopAllWildAudio();
        }
        #endregion

        #region Utils
        public bool TryFindAudioReference(IAudioIdentifier id, out AudioReference audioReference)
        {
            if (_indexedAudioReferenceData.TryGetValue(id, out var audioReferences))
            {
                audioReference = audioReferences.FirstOrDefault();
                return audioReference != null;
            }
            audioReference = null;
            return false;
        }
        #endregion

        #region Bus
        public bool GetBusData(BusType busType, out BusData busData)
        {
            if (!audioSettings.BusData.TryGetValue(busType, out busData))
            {
                Debug.LogError($"Bus {busType} not found in bus dictionary.");
                return false;
            }
            if (!busData.Bus.isValid()) return false;
            return true;
        }
        
        public bool GetBusMuteState(BusType busType, out bool isMuted)
        {
            isMuted = false;
            if (!GetBusData(busType, out var busData)) return false;
            if (!busData.Bus.isValid()) return false;
            if (busData.Bus.getMute(out isMuted) is not RESULT.OK) return false;
            return true;
        }
        
        public bool GetBusVolume(BusType busType, out float volume, VolumeUnit outUnit)
        {
            volume = 0f;
            if (!GetBusData(busType, out var busData)) return false;
            if (!busData.Bus.isValid()) return false;
            if (busData.Bus.getVolume(out var linear) is not RESULT.OK) return false;
            volume = linear.ConvertUnit(VolumeUnit.Linear, outUnit);
            return true;
        }
        
        public void SetMuteBus(BusType busType, bool mute)
        {
            if (!GetBusData(busType, out var busData)) return;
            busData.SetMute(mute);
        }
        
        public void ToggleMuteBus(BusType busType)
        {
            if (!GetBusData(busType, out var busData)) return;
            busData.SetMute(!busData.IsMuted);
        }
        
        public void SetVolumeBus(BusType busType, float value, VolumeUnit inUnit)
        {
            if (!GetBusData(busType, out var busData)) return;
            busData.SetVolume(value, inUnit);
        }
        
        public void StopAllAudioInBus(BusType busType, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            if (!GetBusData(busType, out var busData)) return;
            if (!busData.Bus.isValid()) return;
            busData.Bus.stopAllEvents(stopMode);
            _wildAudioReferenceData.RemoveAll(audioReference => !audioReference.IsPlaying());
            foreach (var (key, audioReferences) in _indexedAudioReferenceData.ToList())
            {
                audioReferences.RemoveAll(audioReference => !audioReference.IsPlaying());
                if (audioReferences.Count == 0)
                {
                    _indexedAudioReferenceData.Remove(key);
                }
            } 
        }
        #endregion
    }

    public static class AudioManagerUtils
    {
        public const float MinLinearVolume = 0f;
        public const float MaxLinearVolume = 3.16227766f;
        public const float MinDecibelVolume = -80f;
        public const float MaxDecibelVolume = 10f;
        
        /// <summary>
        /// Extension method to check if the audio reference is playing.
        /// </summary>
        /// <param name="audioReference"></param>
        /// <returns></returns>
        public static bool IsPlaying(this AudioReference audioReference)
        {
            if (audioReference == null) return false;
            if (!audioReference.eventInstance.isValid()) return false;
            var result = audioReference.eventInstance.getPlaybackState(out var state);
            if (result is not RESULT.OK) return false;
            return state != PLAYBACK_STATE.STOPPED;
        }

        /// <summary>
        /// Same as calling AudioManager.Instance.StopAudio(audioReference, stopMode)
        /// </summary>
        /// <param name="audioReference"></param>
        /// <param name="stopMode"></param>
        public static void Stop(this AudioReference audioReference, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            AudioManager.Instance.StopAudio(audioReference, stopMode);
        }
        
        /// <summary>
        /// Same as calling AudioManager.Instance.SetPauseAudio(audioReference, pause)
        /// </summary>
        /// <param name="audioReference"></param>
        /// <param name="pause"></param>
        public static void SetPause(this AudioReference audioReference, bool pause)
        {
            AudioManager.Instance.SetPauseAudio(audioReference, pause);
        }

        private static float Linear01ToDb(float lin01)
        {
            float linear = lin01 * MaxLinearVolume;
            return Mathf.Clamp(Mathf.Log10(Mathf.Max(linear, 1e-10f)) * 20f, MinDecibelVolume, MaxDecibelVolume);
        }
        
        private static float Db01ToLinear(float db01)
        {
            float db = Mathf.Lerp(MinDecibelVolume, MaxDecibelVolume, db01);
            return Mathf.Clamp(Mathf.Pow(10f, db / 20f), MinLinearVolume, MaxLinearVolume);
        }
        
        /// <summary>
        /// Converts a volume value from one unit to another.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="inUnit"></param>
        /// <param name="outUnit"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static float ConvertUnit(this float value, VolumeUnit inUnit, VolumeUnit outUnit)
        {
            if (inUnit == outUnit) return value;

            bool linear = inUnit is VolumeUnit.Linear or VolumeUnit.Linear01;
            float range01 = inUnit switch
            {
                VolumeUnit.Linear   => Mathf.InverseLerp(MinLinearVolume, MaxLinearVolume, value),
                VolumeUnit.Linear01 => Mathf.Clamp01(value),
                VolumeUnit.Decibel  => Mathf.InverseLerp(MinDecibelVolume, MaxDecibelVolume, value),
                VolumeUnit.Decibel01=> Mathf.Clamp01(value),
                _ => throw new ArgumentOutOfRangeException(nameof(inUnit))
            };
            
            if (linear)
            {
                return outUnit switch
                {
                    VolumeUnit.Linear => Mathf.Lerp(MinLinearVolume, MaxLinearVolume, range01),
                    VolumeUnit.Linear01 => range01,
                    VolumeUnit.Decibel  => Linear01ToDb(range01),
                    VolumeUnit.Decibel01=> Mathf.InverseLerp(MinDecibelVolume, MaxDecibelVolume, Linear01ToDb(range01)),
                    _ => throw new ArgumentOutOfRangeException(nameof(outUnit))
                };
            }
            return outUnit switch
            {
                VolumeUnit.Linear => Db01ToLinear(range01),
                VolumeUnit.Linear01 => Mathf.InverseLerp(MinLinearVolume, MaxLinearVolume, Db01ToLinear(range01)),
                VolumeUnit.Decibel  => Mathf.Lerp(MinDecibelVolume, MaxDecibelVolume, range01),
                VolumeUnit.Decibel01=> range01,
                _ => throw new ArgumentOutOfRangeException(nameof(outUnit))
            };
        }
    }
}