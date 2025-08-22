using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Challenges;
using MadDuck.Scripts.Units;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Managers
{
    public enum SaveLocation
    {
        PersistentDataPath,
        DataPath
    }
    
    public interface IJTokenDeserializer
    {
        public void DeserializeJToken(JToken jToken);
    }
    
    [Serializable]
    public record TestSaveData : IJTokenDeserializer
    {
        [Serializable]
        public record TestSaveDataChild : IJTokenDeserializer
        {
            public string message;
            public DateTime date;
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly, DisplayAsString] private string DebugDateTime => date.ToString("yyyy-MM-dd HH:mm:ss");
            
            public TestSaveDataChild() { } // Parameterless constructor for deserialization
            public TestSaveDataChild(string message)
            {
                this.message = message;
                this.date = DateTime.Now;
            }
            public void DeserializeJToken(JToken jToken)
            {
                jToken.TryGetAndConvertToValue(nameof(message), out message);
                jToken.TryGetAndConvertToValue(nameof(date), out date);
            }
        }
        public string message;
        public DateTime date;
        public List<TestSaveDataChild> children = new();
        [SerializeField] public SerializableDictionary<string, TestSaveDataChild> childrenDictionary = new();
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly, DisplayAsString] private string DebugDateTime => date.ToString("yyyy-MM-dd HH:mm:ss");
        public TestSaveData(string message)
        {
            this.message = message;
            this.date = DateTime.Now;
        }

        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertToList(nameof(children), out IEnumerable<TestSaveDataChild> childrenList);
            children = childrenList?.ToList() ?? new List<TestSaveDataChild>();
            jToken.TryGetAndConvertToDictionary(nameof(childrenDictionary), out IDictionary<string, TestSaveDataChild> d);
            childrenDictionary = d != null ? new SerializableDictionary<string, TestSaveDataChild>(d) : new SerializableDictionary<string, TestSaveDataChild>();
            jToken.TryGetAndConvertToValue(nameof(message), out message);
            jToken.TryGetAndConvertToValue(nameof(date), out date);
        }
    }
    
    [Serializable]
    public record SaveVersionData : IJTokenDeserializer
    {
        public string version;

        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertToValue(nameof(version), out version);
        }
    }
    
    [Serializable]
    public record SaveSettings
    {
        public SaveLocation saveLocation = SaveLocation.DataPath;
        public string saveDirectory = "/TestSave";
        public string saveFileName = "testSave";
    }
    
    
    public class JsonSaveManager : PersistentMonoSingleton<JsonSaveManager>
    {
        [SerializeField] private SaveSettings debugSaveSettings = new();
        [SerializeField] private SaveSettings releaseSaveSettings = new();
        [SerializeField] private bool testReleaseMode = false;
        
        [SerializeField] private TestSaveData testSaveData;
        
        private Dictionary<string, JObject> _saveDataDictionary = new();
        public bool Saving { get; private set; }
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
        
        private const string SaveVersionKey = "SaveVersion";

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
            LoadOnStart().Forget();
        }

        private async UniTaskVoid LoadOnStart()
        {
            await Load();
            await AddOrUpdateData(SaveVersionKey, new SaveVersionData { version = Application.version });
            OnSaveReady?.Invoke();
        }
        #endregion
   
        #region Save/Load Validation
        private bool TryValidate()
        {
            var directoryPath = CurrentSaveSettings.saveLocation == SaveLocation.PersistentDataPath 
                ? Application.persistentDataPath 
                : Application.dataPath;
            try
            {
                
                string fullPath = Path.Combine(directoryPath, CurrentSaveSettings.saveDirectory);
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
                var fileName = CurrentSaveSettings.saveFileName.EndsWith(".json") ? CurrentSaveSettings.saveFileName 
                    : CurrentSaveSettings.saveFileName + ".json";
                string fullPath = Path.Combine(directoryPath, CurrentSaveSettings.saveDirectory, fileName);
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
        #endregion

        #region File Operations
        private async UniTask SaveToFile()
        {
            var directoryPath = CurrentSaveSettings.saveLocation == SaveLocation.PersistentDataPath 
                ? Application.persistentDataPath 
                : Application.dataPath;
            var fileName = CurrentSaveSettings.saveFileName.EndsWith(".json") ? CurrentSaveSettings.saveFileName 
                : CurrentSaveSettings.saveFileName + ".json";
            string fullPath = Path.Combine(directoryPath, CurrentSaveSettings.saveDirectory, fileName);
            var stream = File.Open(fullPath, FileMode.OpenOrCreate);
            var jsonData = JsonConvert.SerializeObject(_saveDataDictionary, Formatting.Indented, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            });
            Debug.Log($"JSON Data: {jsonData}");
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

        private async UniTask LoadFromFile()
        {
            var directoryPath = CurrentSaveSettings.saveLocation == SaveLocation.PersistentDataPath 
                ? Application.persistentDataPath 
                : Application.dataPath;
            var fileName = CurrentSaveSettings.saveFileName.EndsWith(".json") ? CurrentSaveSettings.saveFileName 
                : CurrentSaveSettings.saveFileName + ".json";
            string fullPath = Path.Combine(directoryPath, CurrentSaveSettings.saveDirectory, fileName);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Save file does not exist: {fullPath}");
                return;
            }
            var stream = File.Open(fullPath, FileMode.Open);
            using (var reader = new StreamReader(stream))
            {
                string jsonData = await reader.ReadToEndAsync();
                if (string.IsNullOrEmpty(jsonData))
                {
                    jsonData = "{}"; // Ensure we have a valid JSON object
                }
                _saveDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(jsonData, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
                });
                Debug.Log($"Loaded data from: {fullPath}");
                reader.Close();
            }
            stream.Close();
            await stream.DisposeAsync();
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
        
        public bool TryGetData(string key, IJTokenDeserializer deserializer)
        {
            if (_saveDataDictionary.TryGetValue(key, out var jToken))
            {
                deserializer.DeserializeJToken(jToken);
                return true;
            }
            Debug.LogWarning($"Key '{key}' not found in save data.");
            return false;
        }
        
        public bool TryGetData<T>(string key, out T data, T defaultValue = default)
        {
            data = defaultValue;
            if (_saveDataDictionary.TryGetValue(key, out var jToken))
            {
                return jToken.TryConvertTo(out data);
            }
            Debug.LogWarning($"Key '{key}' not found in save data.");
            return false;
        }
        #endregion
        
        #region Save
        public async UniTask Save()
        {
            if (!TryValidate())
            {
                Saving = false;
                return;
            }
            Saving = true;
            await SaveToFile();
            Saving = false;
            OnSaveCompleted?.Invoke();
            OnSaveReady?.Invoke();
        }
        #endregion
        
        #region Load
        public async UniTask Load()
        {
            if (!TryValidate()) return;
            await LoadFromFile();
            OnLoadCompleted?.Invoke();
        }
        #endregion
        
        
        #region Utils
        private static JObject CreateSavableData(object data)
        {
            return (JObject)JToken.FromObject(data);
        }
        #endregion
    }

    public static class JsonSaveManagerExtensions
    {
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
        
        public static bool TryGetAndConvertToList<T>(this JToken jToken, string key, out IEnumerable<T> result) where T : IJTokenDeserializer, new()
        {
            result = null;
            if (jToken is not JObject jObject) return false;
            if (!jObject.TryGetValue(key, out var token))
            {
                return false;
            }
            var temp = new List<T>();
            foreach (var t in (JArray)token)
            {
                var tempChild = new T();
                tempChild.DeserializeJToken(t);
                temp.Add(tempChild);
            }
            result = temp;
            return true;
        }
        
        public static bool TryGetAndConvertToDictionary<T>(this JToken jToken, string key, out IDictionary<string, T> result) where T : IJTokenDeserializer, new()
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
                tempChild.DeserializeJToken(property.Value);
                temp.Add(property.Name, tempChild);
            }
            result = temp;
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