using System;
using System.Collections.Generic;
using System.Linq;
using Esper.ESave;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Managers
{
    #region Data Structures
    [Serializable]
    public record ScoreData
    {
        [Serializable]
        public record RunData
        {
            public DateTime dateTime;
            public uint score;
            [ShowInInspector, DisplayAsString] private string DebugDateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public uint highScore;
        public List<RunData> runData = new();
    }

    [Serializable]
    public record FitMeData
    {
        [Serializable]
        public record RunData
        {
            public DateTime dateTime;
            public uint fitMe;
            [ShowInInspector, DisplayAsString] private string DebugDateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public uint mostFitMe;
        public List<RunData> runData = new();
    }
    
    [Serializable]
    public record AchievementData
    {
        public uint completedCount;
    }
    #endregion
    
    public class PlayerDataManager : PersistentMonoSingleton<PlayerDataManager>
    {
        #region Inspectors
        [Title("Settings")]
        [SerializeField] private uint maxHighScoresEntries = 10;
        [SerializeField] private uint maxFitMeEntries = 10;
        [SerializeField] private uint maxAchievementEntries = 10;
        
        [field: Title("Debug")]
        [field: SerializeField] public ScoreData ScoreData { get; private set; } = new();
        [field: SerializeField] public FitMeData FitMeData { get; private set; } = new();
        [field: SerializeField] public AchievementData AchievementData { get; private set; } = new();
        [Button("Debug Save All Data")]
        private void DebugSaveAllData()
        {
            SaveScore(0, false);
            SaveFitMe(0, false);
            SaveAchievement(0, false);
            FinishSave();
        }
        
        private const string ScoreDataKey = "ScoreData";
        private const string FitMeDataKey = "RecentFitMe";
        private const string AchievementDataKey = "AchievementData";
        #endregion

        #region Fields and Properties
        private SaveFile CurrentSaveFile => SaveManager.Instance.CurrentSaveFile;
        #endregion
        
        #region Events
        private void OnEnable()
        {
            SaveManager.OnLoadCompleted += LoadPlayerData;
        }

        private void OnDisable()
        {
            SaveManager.OnLoadCompleted -= LoadPlayerData;
        }
        #endregion

        #region Save/Load
        private void LoadPlayerData()
        {
            ScoreData = CurrentSaveFile.GetData<ScoreData>(ScoreDataKey) ?? new ScoreData();
            FitMeData = CurrentSaveFile.GetData<FitMeData>(FitMeDataKey) ?? new FitMeData();
            AchievementData = CurrentSaveFile.GetData<AchievementData>(AchievementDataKey) ?? new AchievementData();
        }
        
        public void SaveScore(uint score, bool saveImmediately = true)
        {
            if (score > ScoreData.highScore)
            {
                ScoreData.highScore = score;
            }

            var newEntry = new ScoreData.RunData
            {
                dateTime = DateTime.Now,
                score = score
            };

            ScoreData.runData.Add(newEntry);
            ScoreData.runData = ScoreData.runData.OrderByDescending(s => s.dateTime).Take((int)maxHighScoresEntries).ToList();
            CurrentSaveFile.AddOrUpdateData(ScoreDataKey, ScoreData);
            if (saveImmediately)
            {
                SaveManager.Instance.Save();
            }
        }
        
        public void SaveFitMe(uint fitMe, bool saveImmediately = true)
        {
            if (fitMe > FitMeData.mostFitMe)
            {
                FitMeData.mostFitMe = fitMe;
            }

            var newEntry = new FitMeData.RunData
            {
                dateTime = DateTime.Now,
                fitMe = fitMe
            };

            FitMeData.runData.Add(newEntry);
            FitMeData.runData = FitMeData.runData.OrderByDescending(f => f.dateTime).Take((int)maxFitMeEntries).ToList();
            CurrentSaveFile.AddOrUpdateData(FitMeDataKey, FitMeData);
            if (saveImmediately)
            {
                SaveManager.Instance.Save();
            }
        }
        
        public void SaveAchievement(uint completedCount, bool saveImmediately = true)
        {
            AchievementData.completedCount = completedCount;
            CurrentSaveFile.AddOrUpdateData(AchievementDataKey, AchievementData);
            if (saveImmediately)
            {
                SaveManager.Instance.Save();
            }
        }

        public void FinishSave()
        {
            SaveManager.Instance.Save();
        }
        #endregion
    }
}