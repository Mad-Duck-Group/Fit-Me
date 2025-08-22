using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Challenges;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Managers
{
    public enum SaveLocation
    {
        PersistentDataPath,
        DataPath
    }
    
    public struct TestSaveData
    {
        public string message;
        public DateTime date;
        public TestSaveData(string message)
        {
            this.message = message;
            this.date = DateTime.Now;
        }
    }

    public record SavableData
    {
        public T GetData<T>()
        {
            if (this is SavableData<T> savableData)
            {
                return savableData.data;
            }
            return default;
        }
        
        public void SetData<T>(T data)
        {
            if (this is SavableData<T> savableData)
            {
                savableData.data = data;
            }
        }
    }

    public interface ISavableDataDeserializer
    {
        public void DeserializeSavableData(dynamic savableData);
    }
    public record SavableData<T> : SavableData
    {
        public T data;
        public SavableData(T data)
        {
            this.data = data;
        }
    }
    public class JsonSaveManager : MonoBehaviour
    {
        [SerializeField] private SaveLocation saveLocation = SaveLocation.DataPath;
        [SerializeField] private string saveDirectory = "TestSave/";
        [SerializeField] private string saveFileName = "testSave";
        
        private Dictionary<string, SavableData> _saveDataDictionary = new();

        [Button("Test Save")]
        public void TestSave()
        {
            ValidateSaveDirectory();
            ValidateSaveFile();
            var data = new TestSaveData("Hello, World!");
            _saveDataDictionary.Clear(); // Clear previous data
            _saveDataDictionary["testData"] = CreateSavableData(data);
            string jsonData = JsonConvert.SerializeObject(_saveDataDictionary, Formatting.Indented, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            });
            SaveToFile(jsonData).Forget();
        }

        [Button("Test Load")]
        public void TestLoad()
        {
            ValidateSaveDirectory();
            ValidateSaveFile();
            LoadFromFile().Forget();
        }
   
        private void ValidateSaveDirectory()
        {
            var directoryPath = saveLocation == SaveLocation.PersistentDataPath 
                ? Application.persistentDataPath 
                : Application.dataPath;
            string fullPath = Path.Combine(directoryPath, saveDirectory);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Debug.Log($"Created save directory: {fullPath}");
            }
        }
    
        private void ValidateSaveFile()
        {
            var directoryPath = saveLocation == SaveLocation.PersistentDataPath 
                ? Application.persistentDataPath 
                : Application.dataPath;
            var fileName = saveFileName.EndsWith(".json") ? saveFileName : saveFileName + ".json";
            string fullPath = Path.Combine(directoryPath, saveDirectory, fileName);
            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, "{}"); // Create an empty JSON file
                Debug.Log($"Created save file: {fullPath}");
            }
        }

        private async UniTask SaveToFile(string data)
        {
            var directoryPath = saveLocation == SaveLocation.PersistentDataPath 
                ? Application.persistentDataPath 
                : Application.dataPath;
            var fileName = saveFileName.EndsWith(".json") ? saveFileName : saveFileName + ".json";
            string fullPath = Path.Combine(directoryPath, saveDirectory, fileName);
            var stream = File.Open(fullPath, FileMode.OpenOrCreate);
            await using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(data);
            }
            stream.Close();
            Debug.Log($"Saved data to: {fullPath}");
        }

        private async UniTask LoadFromFile()
        {
            var directoryPath = saveLocation == SaveLocation.PersistentDataPath 
                ? Application.persistentDataPath 
                : Application.dataPath;
            var fileName = saveFileName.EndsWith(".json") ? saveFileName : saveFileName + ".json";
            string fullPath = Path.Combine(directoryPath, saveDirectory, fileName);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Save file does not exist: {fullPath}");
                return;
            }
            var stream = File.Open(fullPath, FileMode.Open);
            using (var reader = new StreamReader(stream))
            {
                string jsonData = await reader.ReadToEndAsync();
                _saveDataDictionary = JsonConvert.DeserializeObject<Dictionary<string, SavableData>>(jsonData);
                Debug.Log($"Loaded data from: {fullPath}");
            }
            stream.Close();
        }
        
        private SavableData<T> CreateSavableData<T>(T data)
        {
            return new SavableData<T>(data);
        }
    }
}