using System;
using System.Collections.Generic;
using System.Linq;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using MessagePipe;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
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
    
    public class AudioManager : PersistentMonoSingleton<AudioManager>
    {
        [Title("References")] 
        [SerializeField]
        private SerializableDictionary<BusType, string> busDictionary = new()
        {
            { BusType.Master, "bus:/" },
            { BusType.BGM, "bus:/BGM" },
            { BusType.SFX, "bus:/SFX" }
        };

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
                RuntimeManager.AttachInstanceToGameObject(eventInstance, parent);
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
        public bool GetBus(BusType busType, out Bus bus)
        {
            if (!busDictionary.TryGetValue(busType, out var busPath))
            {
                Debug.LogError($"Bus {busType} not found in bus dictionary.");
                bus = default;
                return false;
            }
            bus = RuntimeManager.GetBus(busPath);
            if (!bus.isValid())
            {
                Debug.LogError($"Bus {busType} not found.");
                return false;
            }
            return true;
        }
        
        public bool GetBusVolume(BusType busType, out float volume, bool decibel = true)
        {
            volume = 0f;
            if (!GetBus(busType, out var masterBus)) return false;
            if (masterBus.getVolume(out var linear) is not RESULT.OK) return false;
            volume = decibel ? AudioManagerUtils.LinearToDecibel(linear) : linear;
            return true;
        }
        
        public void SetMuteBus(BusType busType, bool mute)
        {
            if (!GetBus(busType, out var bus)) return;
            bus.setMute(mute);
        }
        
        public void SetVolumeBus(BusType busType, float value, bool decibel = true)
        {
            if (!GetBus(busType, out var bus)) return;
            var finalValue = decibel ? AudioManagerUtils.DecibelToLinear(value) : value;
            bus.setVolume(finalValue);
        }
        
        public void StopAllAudioInBus(BusType busType, STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
        {
            if (!GetBus(busType, out var bus)) return;
            bus.stopAllEvents(stopMode);
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
        
        /// <summary>
        /// Converts a linear volume value (0 to 1) to decibels (-80 to 10).
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public static float LinearToDecibel(float percentage)
        {
            var unclampedVolume = Mathf.Log10(percentage) * 20f;
            var clampedVolume = Mathf.Clamp(unclampedVolume, -80f, 10f);
            return clampedVolume;
        }
        
        /// <summary>
        /// Converts a decibel value (-80 to 10) to a linear volume value (0 to 1).
        /// </summary>
        /// <param name="decibel"></param>
        /// <returns></returns>
        public static float DecibelToLinear(float decibel)
        {
            var linear = Mathf.Pow(10f, decibel / 20f);
            return linear;
        }
    }
}