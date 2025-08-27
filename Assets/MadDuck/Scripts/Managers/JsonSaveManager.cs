using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
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
        public bool encryptSave;
        [ShowIf(nameof(encryptSave))] public string encryptionKey;
        public string saveDirectory = "TestSave";
        public string saveFileName = "testSave";
        
        public SaveSettings Copy()
        {
            return new SaveSettings
            {
                saveLocation = this.saveLocation,
                encryptSave = this.encryptSave,
                encryptionKey = this.encryptionKey,
                saveDirectory = this.saveDirectory,
                saveFileName = this.saveFileName
            };
        }
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
            //Debug.Log($"Resolving PlayerId conflict. Existing: '{existingId}', Incoming: '{incomingId}'");
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

        private Dictionary<string, JToken> _saveMetadataDictionary = new();
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
            TryGetData(SaveMetadataKey, saveMetadata, _saveMetadataDictionary);
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
            var remoteSaveSettings = CurrentSaveSettings.Copy();
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
            var result1 = await TryLoadFromFile(existing, true);
            var result2 = await TryLoadFromFile(incoming, true);
            if (!result1.Item1 || !result2.Item1)
            {
                Debug.LogError("Failed to load one of the save files for comparison. Retaining existing save.");
                File.Delete(GetSaveFilePath(incoming));
                return;
            }
            var dict1 = result1.Item2[0];
            var dict2 = result2.Item2[0];
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
            Debug.Log($"Final conflict resolution: {finalConflictSettings.resolution}");
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
            await using var stream = File.Open(fullPath, FileMode.OpenOrCreate);
            var jsonSaveMetadata = JsonConvert.SerializeObject(_saveMetadataDictionary, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                });
            var saveMetadataBytes = System.Text.Encoding.UTF8.GetBytes(jsonSaveMetadata);
            var headerLength = BitConverter.GetBytes(saveMetadataBytes.Length);
            var jsonSaveData = JsonConvert.SerializeObject(_saveDataDictionary, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                });
            var saveDataBytes = System.Text.Encoding.UTF8.GetBytes(jsonSaveData);
            if (CurrentSaveSettings.encryptSave)
            {
                var key = Convert.FromBase64String(CurrentSaveSettings.encryptionKey);
                saveDataBytes = await JsonSaveManagerEncryption.Encrypt(saveDataBytes, key);
            }
            await using (var writer = new BinaryWriter(stream))
            {
                // clear the file before writing
                stream.SetLength(0);
                writer.Write(headerLength);
                writer.Write(saveMetadataBytes);
                writer.Write(saveDataBytes);
            }
            Debug.Log($"Saved data to: {fullPath}");
        }
        
        private async UniTask<Tuple<bool, Dictionary<string, JToken>[]>> TryLoadFromFile(SaveSettings saveSettings, bool readOnlyMetadata = false)
        {
            var fullPath = GetSaveFilePath(saveSettings);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Save file does not exist: {fullPath}");
                return new Tuple<bool, Dictionary<string, JToken>[]>(false, null);
            }

            await using var stream = File.Open(fullPath, FileMode.Open);
            Dictionary<string, JToken> metadataDict;
            Dictionary<string, JToken> dataDict;
            using (var reader = new BinaryReader(stream))
            {
                try
                {
                    var headerLength = BitConverter.ToInt32(reader.ReadBytes(4), 0);
                    var metadataBytes = reader.ReadBytes(headerLength);
                    var metaDataJson = System.Text.Encoding.UTF8.GetString(metadataBytes);
                    metadataDict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(metaDataJson,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
                        });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to parse metadata JSON: {ex.Message}");
                    reader.Close();
                    stream.Close();
                    dataDict = new Dictionary<string, JToken>();
                    metadataDict = new Dictionary<string, JToken>();
                    return new Tuple<bool, Dictionary<string, JToken>[]>(true, new[] { metadataDict, dataDict });
                }
                if (readOnlyMetadata)
                {
                    dataDict = new Dictionary<string, JToken>();
                    reader.Close();
                    stream.Close();
                    return new Tuple<bool, Dictionary<string, JToken>[]>(true, new[] { metadataDict, dataDict });
                }
                var dataBytes = reader.ReadBytes((int)(stream.Length - stream.Position));
                if (saveSettings.encryptSave)
                {
                    var key = Convert.FromBase64String(saveSettings.encryptionKey);
                    dataBytes = await JsonSaveManagerEncryption.Decrypt(dataBytes, key);
                }
                var jsonData = System.Text.Encoding.UTF8.GetString(dataBytes);
                if (string.IsNullOrEmpty(jsonData))
                {
                    jsonData = "{}"; // Ensure we have a valid JSON object
                }
                try
                {
                    dataDict = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonData,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
                        });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to parse data JSON: {ex.Message}");
                    dataDict = new Dictionary<string, JToken>();
                    reader.Close();
                    stream.Close();
                    return new Tuple<bool, Dictionary<string, JToken>[]>(true, new[] { metadataDict, dataDict });
                }
                Debug.Log($"Loaded data from: {fullPath}");
                reader.Close();
            }

            stream.Close();
            return new Tuple<bool, Dictionary<string, JToken>[]>(true, new[] { metadataDict, dataDict });
        }

        #endregion

        #region Save Data

        public async UniTask AddOrUpdateData(string key, object data, bool saveImmediately = true, Dictionary<string, JToken> dataSource = null)
        {
            var source = dataSource ?? _saveDataDictionary;
            if (source.ContainsKey(key))
            {
                source[key] = CreateSavableData(data);
            }
            else
            {
                source.Add(key, CreateSavableData(data));
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
            Debug.Log("Player ID: " + playerId);
            saveMetadata.playerId = playerId;
            saveMetadata.versionInfo = VersionInfo.TryParse(Application.version, out var version) ? version : new VersionInfo();
            _timeStampSinceLastSave = Time.time;
            await AddOrUpdateData(SaveMetadataKey, saveMetadata, false, _saveMetadataDictionary);
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
            if (!result.Item1 || result.Item2.Length < 2)
            {
                Debug.LogError("Failed to load save data.");
                return;
            }
            _saveMetadataDictionary = result.Item2[0];
            _saveDataDictionary = result.Item2[1];
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

    public static class JsonSaveManagerEncryption
    {
        public static async UniTask<byte[]> Encrypt(byte[] plain, byte[] key, byte[] associatedData = null)
        {
            const int nonceLen   = 12;  // 96-bit nonce
            const int tagLenBits = 128; // 128-bit auth tag

            var nonce = new byte[nonceLen];
            new Org.BouncyCastle.Security.SecureRandom().NextBytes(nonce);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key), tagLenBits, nonce, associatedData));

            var output = new byte[cipher.GetOutputSize(plain.Length)];
            int len = cipher.ProcessBytes(plain, 0, plain.Length, output, 0);
            cipher.DoFinal(output, len);

            // output now = ciphertext || tag
            await using var ms = new MemoryStream();
            ms.Write(nonce); // 12-byte IV
            ms.Write(output);// ciphertext + 16-byte tag
            return ms.ToArray(); 
        }
        
        public static async UniTask<byte[]> Decrypt(byte[] blob, byte[] key, byte[] associatedData = null)
        {
            await using var ms = new MemoryStream(blob);
            using var br = new BinaryReader(ms);
            
            byte[] nonce  = br.ReadBytes(12);        // 12-byte IV
            byte[] cipherAndTag = br.ReadBytes((int)(ms.Length - ms.Position));

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key), 128, nonce, associatedData));

            var plain = new byte[cipher.GetOutputSize(cipherAndTag.Length)];
            int len = cipher.ProcessBytes(cipherAndTag, 0, cipherAndTag.Length, plain, 0);
            cipher.DoFinal(plain, len);
            return plain;
        }
    }
}