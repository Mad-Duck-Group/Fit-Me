using System;
using System.Collections.Generic;
using System.Linq;
using Esper.ESave;
using MadDuck.Scripts.Challenges;
using MadDuck.Scripts.Units;
using MessagePipe;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
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
        public RunData highScore = new();
        public List<RunData> runData = new();
        public uint cumulativeScore;
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
        public RunData mostFitMe = new();
        public List<RunData> runData = new();
        public uint cumulativeFitMe;
    }

    [Serializable]
    public record GameData
    {
        public uint cumulativePreInfectBlockDestroyed;
        public uint cumulativeBlockDestroyed;
        [SerializeField] public SerializableDictionary<BlockTypes, uint> cumulativeColorBlastDictionary = new();
    }
    
    [Serializable]
    public record ChallengeData
    {
        [SerializeField] public SerializableDictionary<Guid, ISavable> challenges = new();
    }
    
    [Serializable]
    public record TutorialData
    {
        public bool completedTutorial;
    }
    #endregion
    
    [ShowOdinSerializedPropertiesInInspector]
    public class PlayerDataManager : PersistentMonoSingleton<PlayerDataManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Inspectors
        [Title("Settings")]
        [SerializeField] private uint maxHighScoresEntries = 10;
        [SerializeField] private uint maxFitMeEntries = 10;
        
        [Title("Challenges")]
        [SerializeField, InlineEditor] private List<ChallengePreset> challengePresets = new();
        
        [field: Title("Debug")]
        [field: SerializeField] public ScoreData ScoreData { get; private set; } = new();
        [field: SerializeField] public FitMeData FitMeData { get; private set; } = new();
        [field: SerializeField] public GameData GameData { get; private set; } = new();
        [field: OdinSerialize] public ChallengeData ChallengeData { get; private set; } = new();
        [field: SerializeField] public TutorialData TutorialData { get; private set; } = new();
        [Button("Debug Save All Data")]
        private void DebugSaveAllData()
        {
            SaveScore(0);
            SaveFitMe(0);
            SaveBlockDestroyed(FitType.Combo, null);
            SaveChallenges(Guid.Empty, null);
            SaveTutorialCompletion();
        }

        [Button("Delete All Data")]
        private void DebugDeleteAllData()
        {
            ScoreData = new ScoreData();
            FitMeData = new FitMeData();
            ChallengeData = new ChallengeData();
            GameData = new GameData();
            TutorialData = new TutorialData();
            Action updateDataAction = () =>
            {
                CurrentSaveFile.DeleteData(ScoreDataKey);
                CurrentSaveFile.DeleteData(FitMeDataKey);
                CurrentSaveFile.DeleteData(ChallengeDataKey);
                CurrentSaveFile.DeleteData(TutorialDataKey);
                CurrentSaveFile.DeleteData(GameDataKey);
            };
            if (SaveManager.Instance.Saving)
            {
                _saveDataQueue.Enqueue(updateDataAction);
            }
            else
            {
                updateDataAction.Invoke();
                SaveManager.Instance.Save();
            }
        }
        
        [Button("Delete Save File")]
        private void DebugDeleteSaveFile()
        {
            CurrentSaveFile.DeleteFile();
        }
        
        private const string ScoreDataKey = "ScoreData";
        private const string FitMeDataKey = "FitMeData";
        private const string GameDataKey = "GameData";
        private const string ChallengeDataKey = "ChallengeData";
        private const string TutorialDataKey = "TutorialData";
        #endregion

        #region Fields and Properties
        private SaveFile CurrentSaveFile => SaveManager.Instance.CurrentSaveFile;
        private IPublisher<ChallengeUpdateEvent<CumulativeScoreChallengeData>> _cumulativeScorePublisher;
        private IPublisher<ChallengeUpdateEvent<CumulativeFitMeChallengeData>> _cumulativeFitMePublisher;
        private IPublisher<ChallengeUpdateEvent<CumulativeBlastChallengeData>> _cumulativeBlastPublisher;
        private IPublisher<ChallengeUpdateEvent<CumulativeBlastColorChallengeData>> _cumulativeBlastColorPublisher;
        private IPublisher<ChallengeUpdateEvent<CumulativeBlastSickChallengeData>> _cumulativeBlastSickPublisher;
        private IPublisher<ChallengeUpdateEvent<TutorialChallengeData>> _tutorialChallengePublisher;
        private IPublisher<ChallengeUpdateEvent<FitMasterChallengeData>> _fitMasterChallengePublisher;
        private Dictionary<Guid, IChallenge> _challengeDictionary = new();
        private readonly Queue<Action> _saveDataQueue = new();
        #endregion
        
        #region Events
        private void OnEnable()
        {
            //clone the challenge presets to avoid modifying the original data
            challengePresets = challengePresets.Select(x => x.Clone()).ToList();
            _challengeDictionary = challengePresets
                .SelectMany(x => x.Challenges)
                .ToDictionary(k => k.ChallengeGuid, v => v);
            SaveManager.OnLoadCompleted += LoadPlayerData;
            SaveManager.OnSaveReady += OnSaveReady;
            _cumulativeScorePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeScoreChallengeData>>();
            _tutorialChallengePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<TutorialChallengeData>>();
            _cumulativeFitMePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeFitMeChallengeData>>();
            _cumulativeBlastPublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeBlastChallengeData>>();
            _cumulativeBlastColorPublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeBlastColorChallengeData>>();
            _cumulativeBlastSickPublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeBlastSickChallengeData>>();
            _fitMasterChallengePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<FitMasterChallengeData>>();
        }

        private void OnDisable()
        {
            SaveManager.OnLoadCompleted -= LoadPlayerData;
            SaveManager.OnSaveReady -= OnSaveReady;
        }

        private void OnDestroy()
        {
            _challengeDictionary.Values.ForEach(c => c.Dispose());
        }

        private void OnSaveReady()
        {
            if (_saveDataQueue.Count == 0) return;
            var action = _saveDataQueue.Dequeue();
            action.Invoke();
            SaveManager.Instance.Save();
        }
        #endregion

        #region Save/Load
        private void LoadPlayerData()
        {
            _challengeDictionary.Values.ForEach(c => c.Initialize());
            ScoreData = CurrentSaveFile.GetData<ScoreData>(ScoreDataKey) ?? new ScoreData();
            FitMeData = CurrentSaveFile.GetData<FitMeData>(FitMeDataKey) ?? new FitMeData();
            GameData = CurrentSaveFile.GetData<GameData>(GameDataKey) ?? new GameData();
            ChallengeData = CurrentSaveFile.GetData<ChallengeData>(ChallengeDataKey) ?? new ChallengeData();
            TutorialData = CurrentSaveFile.GetData<TutorialData>(TutorialDataKey) ?? new TutorialData();
            ValidateChallengeLoad();
        }
        
        public void SaveScore(uint score)
        {
            var newHighScore = score > ScoreData.highScore.score;
            var newEntry = new ScoreData.RunData
            {
                dateTime = DateTime.Now,
                score = score
            };

            ScoreData.runData.Add(newEntry);
            ScoreData.runData = ScoreData.runData.OrderByDescending(s => s.dateTime).Take((int)maxHighScoresEntries).ToList();
            if (newHighScore)
            {
                ScoreData.highScore = newEntry;
            }
            ScoreData.cumulativeScore += score;
            UpdateSaveData(ScoreDataKey, ScoreData);
            _cumulativeScorePublisher.Publish(new ChallengeUpdateEvent<CumulativeScoreChallengeData>(
                new CumulativeScoreChallengeData(ScoreData.cumulativeScore)));
        }
        
        public void SaveFitMe(uint fitMe)
        {
            var newMostFitMe = fitMe > FitMeData.mostFitMe.fitMe;
            var newEntry = new FitMeData.RunData
            {
                dateTime = DateTime.Now,
                fitMe = fitMe
            };

            FitMeData.runData.Add(newEntry);
            FitMeData.runData = FitMeData.runData.OrderByDescending(f => f.dateTime).Take((int)maxFitMeEntries).ToList();
            if (newMostFitMe)
            {
                FitMeData.mostFitMe = newEntry;
            }
            FitMeData.cumulativeFitMe += fitMe;
            UpdateSaveData(FitMeDataKey, FitMeData);
            _cumulativeFitMePublisher.Publish(new ChallengeUpdateEvent<CumulativeFitMeChallengeData>(
                new CumulativeFitMeChallengeData(FitMeData.cumulativeFitMe)));
        }

        public void SaveTutorialCompletion()
        {
            TutorialData.completedTutorial = true;
            UpdateSaveData(TutorialDataKey, TutorialData);
            _tutorialChallengePublisher.Publish(new ChallengeUpdateEvent<TutorialChallengeData>(
                new TutorialChallengeData()));
        }

        public void SaveBlockDestroyed(FitType fitType, List<(BlockState beforeExplodeState, BlockTypes blockType)> destroyedBlocks)
        {
            if (destroyedBlocks.Count == 0) return;
            foreach (var block in destroyedBlocks)
            {
                if (block.beforeExplodeState is BlockState.PreInfected)
                    GameData.cumulativePreInfectBlockDestroyed++;
                GameData.cumulativeBlockDestroyed++;
            }
            if (fitType is FitType.Combo)
            {
                var blockType = destroyedBlocks[0].blockType;
                if (GameData.cumulativeColorBlastDictionary.ContainsKey(blockType))
                {
                    GameData.cumulativeColorBlastDictionary[blockType]++;
                }
                else
                {
                    GameData.cumulativeColorBlastDictionary[blockType] = 1;
                }
            }
            UpdateSaveData(GameDataKey, GameData);
            _cumulativeBlastPublisher?.Publish(new ChallengeUpdateEvent<CumulativeBlastChallengeData>(
                new CumulativeBlastChallengeData(GameData.cumulativeBlockDestroyed)));
            foreach (var kvp in GameData.cumulativeColorBlastDictionary)
            {
                _cumulativeBlastColorPublisher?.Publish(new ChallengeUpdateEvent<CumulativeBlastColorChallengeData>(
                    new CumulativeBlastColorChallengeData(kvp.Key, kvp.Value)));
            }
            _cumulativeBlastSickPublisher?.Publish(new ChallengeUpdateEvent<CumulativeBlastSickChallengeData>(
                new CumulativeBlastSickChallengeData(GameData.cumulativePreInfectBlockDestroyed)));
        }

        private void ValidateChallengeLoad()
        {
            foreach (var guid in ChallengeData.challenges.Keys.ToList())
            {
                if (_challengeDictionary.TryGetValue(guid, out var challenge))
                {
                    challenge.SetChallengeData(ChallengeData.challenges[guid]);
                    continue;
                }
                ChallengeData.challenges.Remove(guid);
                Debug.LogWarning($"Challenge with GUID {guid} not found in current challenges. Removing from save data.");
            }
            foreach (var guid in _challengeDictionary.Keys.ToList())
            {
                ChallengeData.challenges.TryAdd(guid, null);
            }
        }
        
        public void SaveChallenges(Guid challengeGuid, ISavable savable)
        {
            if (!_challengeDictionary.ContainsKey(challengeGuid))
            {
                Debug.LogError($"Challenge with GUID {challengeGuid} not found in current challenges.");
                return;
            }
            ChallengeData.challenges[challengeGuid] = savable;
            UpdateSaveData(ChallengeDataKey, ChallengeData);
            var thisChallenge = _challengeDictionary[challengeGuid];
            if (thisChallenge is FitMasterChallenge) return;
            if (_challengeDictionary.Values.Where(x => x is not FitMasterChallenge).All(x => x.Completed))
            {
                _fitMasterChallengePublisher?.Publish(new ChallengeUpdateEvent<FitMasterChallengeData>(
                    new FitMasterChallengeData()));
            }
        }

        private void UpdateSaveData<T>(string id, T data)
        {
            Action updateDataAction = () => CurrentSaveFile.AddOrUpdateData(id, data);
            if (SaveManager.Instance.Saving)
            {
                _saveDataQueue.Enqueue(updateDataAction);
            }
            else
            {
                updateDataAction.Invoke();
                SaveManager.Instance.Save();
            }
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
}