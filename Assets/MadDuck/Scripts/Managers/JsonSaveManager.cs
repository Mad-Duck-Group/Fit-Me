using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using MadDuck.Scripts.GPGS;
using MessagePipe;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Spine.Collections;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    #region Enums
    public enum SaveLocation
    {
        PersistentDataPath,
        DataPath
    }

    public enum SaveConflictResolution
    {
        UseNewerSave,
        UseOlderSave,
        UseNewerVersion,
        UseOlderVersion,
        UseLocal,
        UseRemote,
        UseLongerPlaytime,
        UseShorterPlaytime,
        Merge,
        Custom
    }
    
    public enum ConflictType
    {
        None, // If no conflicts detected
        Version, // Conflict in VersionInfo
        PlayerId // Conflict in PlayerId
    }
    #endregion

    #region Interfaces
    public interface IJTokenDeserializer
    {
        public void DeserializeJToken(JToken jToken);
    }

    public interface ISaveConflictResolver
    {
        public bool Resolve(SaveMetadata existing, SaveMetadata incoming);
    }
    #endregion

    #region Data Structures
    [Serializable]
    public record TestSaveData : IJTokenDeserializer
    {
        [Serializable]
        public record TestSaveDataChild : IJTokenDeserializer
        {
            public string message;
            public DateTime date;

            [ShowInInspector, Sirenix.OdinInspector.ReadOnly, DisplayAsString]
            private string DebugDateTime => date.ToString("yyyy-MM-dd HH:mm:ss");

            public TestSaveDataChild()
            {
            } // Parameterless constructor for deserialization

            public TestSaveDataChild(string message)
            {
                this.message = message;
                this.date = DateTime.Now;
            }

            public void DeserializeJToken(JToken jToken)
            {
                jToken.TryGetAndConvertTo(nameof(message), out message);
                jToken.TryGetAndConvertTo(nameof(date), out date);
            }
        }

        public string message;
        public DateTime date;
        public List<TestSaveDataChild> children = new();
        [SerializeField] public SerializableDictionary<string, TestSaveDataChild> childrenDictionary = new();

        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, DisplayAsString]
        private string DebugDateTime => date.ToString("yyyy-MM-dd HH:mm:ss");

        public TestSaveData(string message)
        {
            this.message = message;
            this.date = DateTime.Now;
        }

        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(children), out children);
            jToken.TryGetAndConvertTo(nameof(childrenDictionary), out IDictionary<string, TestSaveDataChild> tempDict);
            childrenDictionary = tempDict != null
                ? new SerializableDictionary<string, TestSaveDataChild>(tempDict)
                : new SerializableDictionary<string, TestSaveDataChild>();
            jToken.TryGetAndConvertTo(nameof(message), out message);
            jToken.TryGetAndConvertTo(nameof(date), out date);
        }
    }

    [Serializable]
    public record VersionInfo : IJTokenDeserializer, IComparable<VersionInfo>
    {
        public uint major = 0u;
        public uint minor = 0u;
        public uint patch = 0u;
        public string releaseEnvironment = "Unknown";
        public uint adjustment = 1u;
        public string platform = "Unknown";

        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(major), out major);
            jToken.TryGetAndConvertTo(nameof(minor), out minor);
            jToken.TryGetAndConvertTo(nameof(patch), out patch);
            jToken.TryGetAndConvertTo(nameof(releaseEnvironment), out releaseEnvironment);
            jToken.TryGetAndConvertTo(nameof(adjustment), out adjustment);
            jToken.TryGetAndConvertTo(nameof(platform), out platform);
        }

        public static bool TryParse(string versionString, out VersionInfo versionInfo)
        {
            versionInfo = new VersionInfo();
            if (string.IsNullOrEmpty(versionString)) return false;
            // Example version string: "1.0.0-release.adjustment-platform"
            var parts = versionString.Split('-');
            if (parts.Length == 0) return false;
            var versionParts = parts[0].Split('.');
            var releaseParts = parts.Length > 1 ? parts[1].Split('.') : Array.Empty<string>();
            var majorValue = 0u;
            var minorValue = 0u;
            var patchValue = 0u;
            var releaseEnvironment = "Unknown";
            var adjustmentValue = 1u;
            var platform = "Unknown";
            if (versionParts.Length >= 1) uint.TryParse(versionParts[0], out majorValue);
            if (versionParts.Length >= 2) uint.TryParse(versionParts[1], out minorValue);
            if (versionParts.Length >= 3) uint.TryParse(versionParts[2], out patchValue);
            if (releaseParts.Length >= 1) releaseEnvironment = releaseParts[0];
            if (releaseParts.Length >= 2)
                uint.TryParse(releaseParts[1], out adjustmentValue);
            if (parts.Length >= 3) platform = parts[2];
            versionInfo = new VersionInfo
            {
                major = majorValue,
                minor = minorValue,
                patch = patchValue,
                releaseEnvironment = releaseEnvironment,
                adjustment = adjustmentValue,
                platform = platform
            };
            return true;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{patch}-{releaseEnvironment}.{adjustment}-{platform}";
        }

        public int CompareTo(VersionInfo other)
        {
            if (other == null) return 1;
            int result = major.CompareTo(other.major);
            if (result != 0) return result;
            result = minor.CompareTo(other.minor);
            if (result != 0) return result;
            result = patch.CompareTo(other.patch);
            if (result != 0) return result;
            result = adjustment.CompareTo(other.adjustment);
            return result;
        }

        public static bool operator >(VersionInfo left, VersionInfo right) => left.CompareTo(right) > 0;
        public static bool operator <(VersionInfo left, VersionInfo right) => left.CompareTo(right) < 0;
        public static bool operator >=(VersionInfo left, VersionInfo right) => left.CompareTo(right) >= 0;
        public static bool operator <=(VersionInfo left, VersionInfo right) => left.CompareTo(right) <= 0;
    }

    [Serializable]
    public record SaveMetadata : IJTokenDeserializer
    {
        public VersionInfo versionInfo;
        public string playerId;
        public DateTime lastModified = DateTime.MinValue;
        public TimeSpan playtime = TimeSpan.Zero;

        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(versionInfo), out versionInfo);
            jToken.TryGetAndConvertTo(nameof(playerId), out playerId);
            jToken.TryGetAndConvertTo(nameof(lastModified), out lastModified);
            jToken.TryGetAndConvertTo(nameof(playtime), out playtime);
        }
    }
    #endregion
    
    [Serializable]
    public record SaveSettings
    {
        public SaveLocation saveLocation = SaveLocation.DataPath;
        public string saveDirectory = "TestSave";
        public string saveFileName = "testSave";
    }

    public struct SaveToServiceEvent
    {
        public readonly byte[] data;
        public readonly TimeSpan? totalPlaytime;
        public readonly Texture2D savedImage;

        public SaveToServiceEvent(byte[] data, TimeSpan? totalPlaytime = null, Texture2D savedImage = null)
        {
            this.data = data;
            this.totalPlaytime = totalPlaytime;
            this.savedImage = savedImage;
        }
    }

    [Serializable]
    public struct ConflictSettings
    {
        public SaveConflictResolution resolution;
        public int priority;
        [OdinSerialize]
        [ShowIf(nameof(resolution), SaveConflictResolution.Custom)] 
        public ISaveConflictResolver customResolver;
        
        public static ConflictSettings Default => new()
        {
            resolution = SaveConflictResolution.UseLongerPlaytime,
            priority = int.MinValue
        };
    }

    [Serializable]
    public class GPGSPlayerIdResolver : ISaveConflictResolver
    {
        public bool Resolve(SaveMetadata existing, SaveMetadata incoming)
        {
            var existingId = existing.playerId ?? string.Empty;
            var incomingId = incoming.playerId ?? string.Empty;
            //if existing is empty, prefer incoming
            if (string.IsNullOrEmpty(existingId) && !string.IsNullOrEmpty(incomingId)) return true;
            //if incoming is empty, prefer existing
            if (!string.IsNullOrEmpty(existingId) && string.IsNullOrEmpty(incomingId)) return false;
            //if both are empty, prefer existing
            if (string.IsNullOrEmpty(existingId) && string.IsNullOrEmpty(incomingId)) return false;
            //if both exist, prefer incoming
            return true;
        }
    }


    [ShowOdinSerializedPropertiesInInspector]
    public class JsonSaveManager : PersistentMonoSingleton<JsonSaveManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        [SerializeField] private SaveSettings debugSaveSettings = new();
        [SerializeField] private SaveSettings releaseSaveSettings = new();
        [SerializeField] private bool testReleaseMode = false;
        [SerializeField] private float saveToServiceCooldown = 1f;
        [OdinSerialize] private SerializableDictionary<ConflictType, ConflictSettings> conflictSettings = new();
        [SerializeField] private SaveMetadata saveMetadata = new();
        [SerializeField] private TestSaveData testSaveData;

        private Dictionary<string, JToken> _saveDataDictionary = new();
        private IPublisher<SaveToServiceEvent> _saveToServicePublisher;
        private IDisposable _loadFromServiceSubscription;
        private CancellationTokenSource _saveCts;
        public bool Saving { get; private set; }
        private float _timeStampSinceLastSave;

        public SaveSettings CurrentSaveSettings
        {
            get
            {
#if UNITY_EDITOR
                return testReleaseMode ? releaseSaveSettings : debugSaveSettings;
#else
                return releaseSaveSettings;
#endif
            }
        }

        public event Action OnSaveCompleted;
        public static event Action OnLoadCompleted;
        public static event Action OnSaveReady;

        private const string SaveMetadataKey = "SaveMetadata";

        [Button("Test Save")]
        public async UniTaskVoid TestSave()
        {
            await AddOrUpdateData("testData", testSaveData);
        }

        [Button("Test Load")]
        public async UniTaskVoid TestLoad()
        {
            await Load();
            TryGetData("testData", testSaveData);
        }

        #region Initialization

        private void Start()
        {
            _timeStampSinceLastSave = Time.time;
#if !UNITY_ANDROID
            LoadOnStart().Forget();
#endif
        }

        private async UniTaskVoid LoadOnStart()
        {
            await Load();
            TryGetData(SaveMetadataKey, saveMetadata);
            OnSaveReady?.Invoke();
        }

        #endregion

        #region Events

        private void OnEnable()
        {
            _saveToServicePublisher = GlobalMessagePipe.GetPublisher<SaveToServiceEvent>();
            _loadFromServiceSubscription = GlobalMessagePipe.GetSubscriber<LoadFromServiceEvent>()
                .Subscribe(x => LoadFromService(x).Forget());
#if UNITY_ANDROID
            GPGSManager.OnFinishedAuthentication += OnFinishAuthentication;
#endif
        }

#if UNITY_ANDROID
        private void OnFinishAuthentication(SignInStatus signInStatus)
        {
            if (signInStatus == SignInStatus.Success) return;
            // If authentication failed or user signed out, we can still load local save
            LoadOnStart().Forget();
        }
#endif

        private void OnDisable()
        {
#if UNITY_ANDROID
            GPGSManager.OnFinishedAuthentication -= OnFinishAuthentication;
#endif
            _loadFromServiceSubscription?.Dispose();
        }

        private async UniTaskVoid LoadFromService(LoadFromServiceEvent eventData)
        {
            Debug.Log("Received LoadFromServiceEvent");
            var remoteSaveSettings = new SaveSettings
            {
                saveLocation = CurrentSaveSettings.saveLocation,
                saveDirectory = CurrentSaveSettings.saveDirectory,
                saveFileName = CurrentSaveSettings.saveFileName
            };
            if (remoteSaveSettings.saveFileName.EndsWith(".json"))
            {
                remoteSaveSettings.saveFileName = remoteSaveSettings.saveFileName[..^5];
            }
            remoteSaveSettings.saveFileName += "_remote.json";
            var remoteFilePath = GetSaveFilePath(remoteSaveSettings);
            TryValidate(remoteSaveSettings);
            TryValidate(CurrentSaveSettings);
            await File.WriteAllBytesAsync(remoteFilePath, eventData.data);
            await ResolveSave(CurrentSaveSettings, remoteSaveSettings);
            Load().Forget();
        }

        #endregion

        #region Save/Load Validation

        private bool TryValidate(SaveSettings saveSettings)
        {
            var directoryPath = saveSettings.saveLocation == SaveLocation.PersistentDataPath
                ? Application.persistentDataPath
                : Application.dataPath;
            try
            {
                string fullPath = Path.Combine(directoryPath, saveSettings.saveDirectory);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    Debug.Log($"Created save directory: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to validate save directory: {ex.Message}");
                return false;
            }

            try
            {
                var fileName = saveSettings.saveFileName.EndsWith(".json")
                    ? saveSettings.saveFileName
                    : saveSettings.saveFileName + ".json";
                string fullPath = Path.Combine(directoryPath, saveSettings.saveDirectory, fileName);
                if (!File.Exists(fullPath))
                {
                    File.WriteAllText(fullPath, "{}"); // Create an empty JSON file
                    Debug.Log($"Created save file: {fullPath}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to validate save file: {ex.Message}");
                return false;
            }
        }

        private async UniTask ResolveSave(SaveSettings existing, SaveSettings incoming)
        {
            var result1 = await TryLoadFromFile(existing);
            var result2 = await TryLoadFromFile(incoming);
            if (!result1.Item1 || !result2.Item1)
            {
                Debug.LogError("Failed to load one of the save files for comparison. Retaining existing save.");
                File.Delete(GetSaveFilePath(incoming));
                return;
            }
            var dict1 = result1.Item2 ?? new Dictionary<string, JToken>();
            var dict2 = result2.Item2 ?? new Dictionary<string, JToken>();
            var metadata1 = new SaveMetadata();
            var metadata2 = new SaveMetadata();
            TryGetData(SaveMetadataKey, metadata1, dict1);
            TryGetData(SaveMetadataKey, metadata2, dict2);
            var versionInfo1 = metadata1.versionInfo ?? new VersionInfo();
            var versionInfo2 = metadata2.versionInfo ?? new VersionInfo();
            var playerId1 = metadata1.playerId ?? string.Empty;
            var playerId2 = metadata2.playerId ?? string.Empty;
            var shouldOverwrite = true;
            var newer = metadata1.lastModified >= metadata2.lastModified ? existing : incoming;
            var newerVersion = versionInfo1 >= versionInfo2 ? existing : incoming;
            var longerPlaytime = metadata1.playtime >= metadata2.playtime ? existing : incoming;
            var versionConflict = !versionInfo1.Equals(versionInfo2);
            var playerIdConflict = !playerId1.Equals(playerId2);
            var finalConflictSettings = ConflictSettings.Default;
            if (conflictSettings.TryGetValue(ConflictType.Version, out var versionConflictSettings))
            {
                if (versionConflictSettings.resolution is SaveConflictResolution.Custom || versionConflict)
                    finalConflictSettings = versionConflictSettings.priority > finalConflictSettings.priority
                    ? versionConflictSettings
                    : finalConflictSettings;
            }
            if (conflictSettings.TryGetValue(ConflictType.PlayerId, out var playerIdConflictSettings))
            {
                if (playerIdConflictSettings.resolution is SaveConflictResolution.Custom || playerIdConflict)
                    finalConflictSettings = playerIdConflictSettings.priority > finalConflictSettings.priority
                    ? playerIdConflictSettings
                    : finalConflictSettings;
            }
            if (conflictSettings.TryGetValue(ConflictType.None, out var noConflictSettings))
            {
                finalConflictSettings = noConflictSettings.priority > finalConflictSettings.priority
                    ? noConflictSettings
                    : finalConflictSettings;
            }
            switch (finalConflictSettings.resolution)
            {
                case SaveConflictResolution.UseNewerSave:
                    shouldOverwrite = newer == incoming;
                    break;
                case SaveConflictResolution.UseOlderSave:
                    shouldOverwrite = newer == existing;
                    break;
                case SaveConflictResolution.UseNewerVersion:
                    shouldOverwrite = newerVersion == incoming;
                    break;
                case SaveConflictResolution.UseOlderVersion:
                    shouldOverwrite = newerVersion == existing;
                    break;
                case SaveConflictResolution.UseLocal:
                    shouldOverwrite = false;
                    break;
                case SaveConflictResolution.UseRemote:
                    shouldOverwrite = true;
                    break;
                case SaveConflictResolution.UseLongerPlaytime:
                    shouldOverwrite = longerPlaytime == incoming;
                    break;
                case SaveConflictResolution.UseShorterPlaytime:
                    shouldOverwrite = longerPlaytime == existing;
                    break;
                case SaveConflictResolution.Merge:
                    // Merging not implemented
                    Debug.LogWarning("Merge conflict resolution is not implemented. No action taken.");
                    break;
                case SaveConflictResolution.Custom:
                    if (finalConflictSettings.customResolver == null)
                    {
                        Debug.LogWarning("Custom resolver is null. No action taken.");
                        break;
                    }
                    shouldOverwrite = finalConflictSettings.customResolver.Resolve(metadata1, metadata2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (shouldOverwrite)
            {
                await File.WriteAllBytesAsync(GetSaveFilePath(existing), await ConvertToBytes(incoming));
                Debug.Log("Overwrote existing save with incoming save.");
            }
            else
            {
                Debug.Log("Retained existing save.");
            }
            File.Delete(GetSaveFilePath(incoming));
        }

        #endregion

        #region File Operations

        private async UniTask SaveToFile()
        {
            var fullPath = GetSaveFilePath(CurrentSaveSettings);
            var stream = File.Open(fullPath, FileMode.OpenOrCreate);
            var jsonData = JsonConvert.SerializeObject(_saveDataDictionary, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                });
            await using (var writer = new StreamWriter(stream))
            {
                // clear the file before writing
                stream.SetLength(0);
                await writer.WriteAsync(jsonData);
                await writer.FlushAsync();
                writer.Close();
            }

            stream.Close();
            await stream.DisposeAsync();
            Debug.Log($"Saved data to: {fullPath}");
        }

        private async UniTask<Tuple<bool, Dictionary<string, JToken>>> TryLoadFromFile(SaveSettings saveSettings)
        {
            var fullPath = GetSaveFilePath(saveSettings);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Save file does not exist: {fullPath}");
                return new Tuple<bool, Dictionary<string, JToken>>(false, null);
            }

            var stream = File.Open(fullPath, FileMode.Open);
            Dictionary<string, JToken> dictionary;
            using (var reader = new StreamReader(stream))
            {
                string jsonData = await reader.ReadToEndAsync();
                if (string.IsNullOrEmpty(jsonData))
                {
                    jsonData = "{}"; // Ensure we have a valid JSON object
                }

                dictionary = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonData,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
                    });
                Debug.Log($"Loaded data from: {fullPath}");
                reader.Close();
            }

            stream.Close();
            await stream.DisposeAsync();
            return new Tuple<bool, Dictionary<string, JToken>>(true, dictionary);
        }

        #endregion

        #region Save Data

        public async UniTask AddOrUpdateData(string key, object data, bool saveImmediately = true)
        {
            if (_saveDataDictionary.ContainsKey(key))
            {
                _saveDataDictionary[key] = CreateSavableData(data);
            }
            else
            {
                _saveDataDictionary.Add(key, CreateSavableData(data));
            }

            if (saveImmediately)
                await Save();
        }

        public async UniTask RemoveData(string key, bool saveImmediately = true)
        {
            if (!_saveDataDictionary.Remove(key))
            {
                Debug.LogWarning($"Key '{key}' not found in save data.");
            }

            if (saveImmediately)
                await Save();
        }

        public void ClearSaveData()
        {
            _saveDataDictionary.Clear();
            Debug.Log("All save data cleared.");
        }

        public bool TryGetData(string key, IJTokenDeserializer deserializer, Dictionary<string, JToken> sourceData = null)
        {
            var source = sourceData ?? _saveDataDictionary;
            if (source.TryGetValue(key, out var jToken))
            {
                deserializer.DeserializeJToken(jToken);
                return true;
            }

            Debug.LogWarning($"Key '{key}' not found in save data.");
            return false;
        }

        public bool TryGetData<T>(string key, out T data, T defaultValue = default, Dictionary<string, JToken> sourceData = null)
        {
            data = defaultValue;
            var source = sourceData ?? _saveDataDictionary;
            if (source.TryGetValue(key, out var jToken))
            {
                return jToken.TryConvertTo(out data);
            }

            Debug.LogWarning($"Key '{key}' not found in save data.");
            return false;
        }

        #endregion

        #region Save

        public async UniTask Save(bool saveToService = false)
        {
            if (!TryValidate(CurrentSaveSettings))
            {
                Saving = false;
                return;
            }

            Saving = true;
            var playerId = string.Empty;
#if UNITY_ANDROID
            playerId = PlayGamesPlatform.Instance.IsAuthenticated()
                ? PlayGamesPlatform.Instance.localUser.id
                : string.Empty;
#endif
            var durationSinceLastSave = TimeSpan.FromSeconds(Time.time - _timeStampSinceLastSave);
            saveMetadata.playtime += durationSinceLastSave;
            saveMetadata.lastModified = DateTime.Now;
            saveMetadata.playerId = playerId;
            saveMetadata.versionInfo = VersionInfo.TryParse(Application.version, out var version) ? version : new VersionInfo();
            _timeStampSinceLastSave = Time.time;
            await AddOrUpdateData(SaveMetadataKey, saveMetadata, false);
            await SaveToFile();
            Saving = false;
            OnSaveCompleted?.Invoke();
            OnSaveReady?.Invoke();
            if (saveToService) await SaveToService();
        }

        #endregion

        #region Load

        public async UniTask Load()
        {
            if (!TryValidate(CurrentSaveSettings)) return;
            var result =  await TryLoadFromFile(CurrentSaveSettings);
            if (!result.Item1)
            {
                Debug.LogError("Failed to load save data.");
                return;
            }
            _saveDataDictionary = result.Item2 ?? new Dictionary<string, JToken>();
            OnLoadCompleted?.Invoke();
        }

        #endregion
        
        #region Service Operations
        public async UniTask SaveToService()
        {
            if (_saveCts is { IsCancellationRequested: false }) _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            await UniTask.WaitForSeconds(saveToServiceCooldown, cancellationToken: _saveCts.Token);
            _saveCts = null;
            var dataBytes = await ConvertToBytes(CurrentSaveSettings);
            _saveToServicePublisher.Publish(new SaveToServiceEvent(dataBytes, saveMetadata.playtime));
            Debug.Log("Save data published to service.");
        }
        #endregion

        #region Utils

        private static JToken CreateSavableData(object data)
        {
            return JToken.FromObject(data);
        }

        public async UniTask<byte[]> ConvertToBytes(SaveSettings saveSettings = null)
        {
            saveSettings ??= CurrentSaveSettings;
            var filePath = GetSaveFilePath(saveSettings);
            return File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath) : null;
        }

        private string GetSaveFilePath(SaveSettings saveSettings)
        {
            var directoryPath = saveSettings.saveLocation == SaveLocation.PersistentDataPath
                ? Application.persistentDataPath
                : Application.dataPath;
            var fileName = saveSettings.saveFileName.EndsWith(".json")
                ? saveSettings.saveFileName
                : saveSettings.saveFileName + ".json";
            return Path.Combine(directoryPath, saveSettings.saveDirectory, fileName);
        }

        #endregion
        
        #region Serialization
        public void OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
        }

        public void OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
        }

        [SerializeField, HideInInspector]
        private SerializationData serializationData;
        public SerializationData SerializationData 
        { 
            get => serializationData;
            set => serializationData = value;
        }
        #endregion
    }

    public static class JsonSaveManagerExtensions
    {
        /// <summary>
        /// Wrapper method to handle both value types and collections (IList, IDictionary).
        /// </summary>
        /// <param name="jToken"></param>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool TryGetAndConvertTo<T>(this JToken jToken, string key, out T result)
        {
            result = default;
            var type = typeof(T);
            if (!type.IsGenericType || type == typeof(string))
            {
                return jToken.TryGetAndConvertToValue(key, out result);
            }

            var isGenericType = type.IsGenericType;
            var genericTypeDefinition = isGenericType ? type.GetGenericTypeDefinition() : null;
            switch (isGenericType)
            {
                case true when genericTypeDefinition == typeof(IList<>):
                {
                    var elementType = type.GetGenericArguments()[0];
                    if (!jToken.TryGetAndConvertToList(key, elementType, out var list)) return false;
                    result = (T)list;
                    return true;
                }
                case true when genericTypeDefinition == typeof(IDictionary<,>) &&
                               type.GetGenericArguments()[0] == typeof(string):
                {
                    var valueType = type.GetGenericArguments()[1];
                    if (!jToken.TryGetAndConvertToDictionary(key, valueType, out var dict)) return false;
                    result = (T)dict;
                    return true;
                }
                case true:
                    return jToken.TryGetAndConvertToValue(key, out result);
                default:
                    Debug.LogError($"Type '{type}' is not supported for this method.");
                    return false;
            }
        }

        public static bool TryGetAndConvertToValue<T>(this JToken jToken, string key, out T result)
        {
            result = default;
            if (jToken is not JObject jObject) return false;
            if (jObject.TryGetValue(key, out var token))
            {
                return token.TryConvertTo(out result);
            }

            Debug.LogError($"Key '{key}' not found in JObject.");
            return false;
        }

        public static bool TryGetAndConvertToList<T>(this JToken jToken, string key, out IList<T> result)
            where T : new()
        {
            result = null;
            if (jToken is not JObject jObject) return false;
            if (!jObject.TryGetValue(key, out var token))
            {
                return false;
            }

            if (token.Type is not JTokenType.Array) return false;
            var temp = new List<T>();
            foreach (var item in (JArray)token)
            {
                var tempChild = new T();
                if (tempChild is IJTokenDeserializer deserializer)
                    deserializer.DeserializeJToken(item);
                else
                {
                    item.TryConvertTo(out tempChild);
                }

                if (tempChild != null && tempChild.GetType().IsAssignableFrom(tempChild.GetType()))
                    temp.Add(tempChild);
            }

            result = temp;
            return true;
        }

        private static bool TryGetAndConvertToList(this JToken jToken, string key, Type elementType, out IList result)
        {
            result = null;
            if (jToken is not JObject jObject) return false;
            if (!jObject.TryGetValue(key, out var token))
            {
                return false;
            }

            if (token.Type is not JTokenType.Array) return false;
            var temp = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            foreach (var item in (JArray)token)
            {
                var tempChild = Activator.CreateInstance(elementType);
                if (tempChild is IJTokenDeserializer deserializer)
                    deserializer.DeserializeJToken(item);
                else
                {
                    item.TryConvertTo(elementType, out tempChild);
                }

                if (tempChild != null && elementType.IsAssignableFrom(tempChild.GetType()))
                    temp.Add(tempChild);
            }

            result = temp;
            return true;
        }

        public static bool TryGetAndConvertToDictionary<T>(this JToken jToken, string key,
            out IDictionary<string, T> result) where T : new()
        {
            result = null;
            if (jToken is not JObject jObject) return false;
            if (!jObject.TryGetValue(key, out var token))
            {
                return false;
            }

            if (token.Type is not JTokenType.Object) return false;
            var temp = new Dictionary<string, T>();
            foreach (var property in ((JObject)token).Properties())
            {
                var tempChild = new T();
                if (tempChild is IJTokenDeserializer deserializer)
                    deserializer.DeserializeJToken(property.Value);
                else
                {
                    property.TryConvertTo(out tempChild);
                }

                if (tempChild != null && tempChild.GetType().IsAssignableFrom(tempChild.GetType()))
                    temp.Add(property.Name, tempChild);
            }

            result = temp;
            return true;
        }

        private static bool TryGetAndConvertToDictionary(this JToken jToken, string key, Type valueType,
            out IDictionary result)
        {
            result = null;
            if (jToken is not JObject jObject) return false;
            if (!jObject.TryGetValue(key, out var token))
            {
                return false;
            }

            if (token.Type is not JTokenType.Object) return false;
            var temp = (IDictionary)Activator.CreateInstance(
                typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType));
            foreach (var property in ((JObject)token).Properties())
            {
                var tempChild = Activator.CreateInstance(valueType);
                if (tempChild is IJTokenDeserializer deserializer)
                    deserializer.DeserializeJToken(property.Value);
                else
                {
                    property.TryConvertTo(valueType, out tempChild);
                }

                if (tempChild != null && valueType.IsAssignableFrom(tempChild.GetType()))
                    temp.Add(property.Name, tempChild);
            }

            result = temp;
            return true;
        }

        public static bool TryConvertTo(this JToken jToken, Type targetType, out object result)
        {
            result = null;
            try
            {
                result = jToken.ToObject(targetType);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to convert JToken to {targetType}: {ex.Message}");
                return false;
            }

            return true;
        }

        public static bool TryConvertTo<T>(this JToken jToken, out T result)
        {
            result = default;
            try
            {
                result = jToken.ToObject<T>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to convert JToken to {typeof(T)}: {ex.Message}");
                return false;
            }

            return true;
        }
    }
}