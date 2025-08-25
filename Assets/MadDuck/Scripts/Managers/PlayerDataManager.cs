using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Esper.ESave;
using MadDuck.Scripts.Challenges;
using MadDuck.Scripts.Frameworks.MessagePipe;
using MadDuck.Scripts.Units;
using MessagePipe;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
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
    public record PlayerRecordData : IJTokenDeserializer
    {
        [Serializable]
        public record RunData : IJTokenDeserializer
        {
            public DateTime dateTime;
            public uint score;
            public uint fitMe;
            [ShowInInspector, DisplayAsString] private string DebugDateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            public void DeserializeJToken(JToken jToken)
            {
                jToken.TryGetAndConvertTo(nameof(dateTime), out dateTime);
                jToken.TryGetAndConvertTo(nameof(score), out score);
                jToken.TryGetAndConvertTo(nameof(fitMe), out fitMe);
            }
        }
        public RunData highScore = new();
        public RunData mostFitMe = new();
        public List<RunData> runData = new();
        public uint cumulativeScore;
        public uint cumulativeFitMe;
        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(highScore), out highScore);
            jToken.TryGetAndConvertTo(nameof(runData), out runData);
            jToken.TryGetAndConvertTo(nameof(cumulativeScore), out cumulativeScore);
            jToken.TryGetAndConvertTo(nameof(mostFitMe), out mostFitMe);
            jToken.TryGetAndConvertTo(nameof(cumulativeFitMe), out cumulativeFitMe);
        }
    }
    
    [Serializable]
    public record GameData : IJTokenDeserializer
    {
        public uint cumulativePreInfectBlockDestroyed;
        public uint cumulativeBlockDestroyed;
        [SerializeField] public SerializableDictionary<BlockTypes, uint> cumulativeColorBlastDictionary = new();
        
        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(cumulativePreInfectBlockDestroyed), out cumulativePreInfectBlockDestroyed);
            jToken.TryGetAndConvertTo(nameof(cumulativeBlockDestroyed), out cumulativeBlockDestroyed);
            jToken.TryGetAndConvertTo(nameof(cumulativeColorBlastDictionary), out IDictionary<string, uint> colorBlastDict);
            cumulativeColorBlastDictionary = colorBlastDict != null
                ? new SerializableDictionary<BlockTypes, uint>(colorBlastDict.ToDictionary(
                    kvp => Enum.TryParse(kvp.Key, out BlockTypes blockType) ? blockType : BlockTypes.Red,
                    kvp => kvp.Value))
                : new SerializableDictionary<BlockTypes, uint>();
        }
    }
    
    [Serializable]
    public record ChallengeData : IJTokenDeserializer
    {
        [SerializeField] public SerializableDictionary<Guid, SavableChallengeData> challenges = new();
        
        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(challenges), out IDictionary<string, SavableChallengeData> challengeData);
            if (challengeData != null)
            {
                challenges = new SerializableDictionary<Guid, SavableChallengeData>(challengeData.ToDictionary(
                    kvp => Guid.TryParse(kvp.Key, out var guid) ? guid : Guid.Empty,
                    kvp => kvp.Value));
            }
            else
            {
                challenges = new SerializableDictionary<Guid, SavableChallengeData>();
            }
            
        }
    }
    
    [Serializable]
    public record TutorialData : IJTokenDeserializer
    {
        public bool completedTutorial;
        
        public void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(completedTutorial), out completedTutorial);
        }
    }
    #endregion
}
namespace MadDuck.Scripts.Managers
{
    [ShowOdinSerializedPropertiesInInspector]
    public class PlayerDataManager : PersistentMonoSingleton<PlayerDataManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Inspectors
        [Title("Settings")]
        [SerializeField] private uint maxHighScoresEntries = 10;
        [SerializeField] private uint maxFitMeEntries = 10;

        [field: FormerlySerializedAs("challengePresets")]
        [field: Title("Challenges")]
        [field: SerializeField, InlineEditor]
        public List<ChallengePreset> ChallengePresets { get; private set; } = new();

        [field: Title("Debug")] 
        [field: SerializeField] public PlayerRecordData PlayerRecordData { get; private set; } = new();
        [field: SerializeField] public GameData GameData { get; private set; } = new();
        [field: OdinSerialize] public ChallengeData ChallengeData { get; private set; } = new();
        [field: SerializeField] public TutorialData TutorialData { get; private set; } = new();
        [Button("Debug Save All Data")]
        private void DebugSaveAllData()
        {
            SaveRecord(0, 0);
            //SaveBlockDestroyed(FitType.Combo, null);
            SaveChallenges(Guid.Empty, null);
            SaveTutorialCompletion();
        }

        [Button("Delete All Data")]
        private void DebugDeleteAllData()
        {
            PlayerRecordData = new PlayerRecordData();
            ChallengeData = new ChallengeData();
            GameData = new GameData();
            TutorialData = new TutorialData();
            Action updateDataAction = () =>
            {
                JsonSaveManager.Instance.RemoveData(PlayerRecordKey, false).Forget();
                JsonSaveManager.Instance.RemoveData(GameDataKey, false).Forget();
                JsonSaveManager.Instance.RemoveData(ChallengeDataKey, false).Forget();
                JsonSaveManager.Instance.RemoveData(TutorialDataKey, false).Forget();
                JsonSaveManager.Instance.Save(true).Forget();
            };
            if (JsonSaveManager.Instance.Saving)
            {
                _saveDataQueue.Enqueue(updateDataAction);
            }
            else
            {
                updateDataAction.Invoke();
            }
        }
        
        private const string PlayerRecordKey = "PlayerRecordData";
        private const string GameDataKey = "GameData";
        private const string ChallengeDataKey = "ChallengeData";
        private const string TutorialDataKey = "TutorialData";
        #endregion

        #region Fields and Properties
        private IPublisher<ChallengeUpdateEvent<CumulativeScoreChallengeData>> _cumulativeScorePublisher;
        private IPublisher<ChallengeUpdateEvent<CumulativeFitMeChallengeData>> _cumulativeFitMePublisher;
        private IPublisher<ChallengeUpdateEvent<CumulativeBlastChallengeData>> _cumulativeBlastPublisher;
        private IPublisher<ChallengeUpdateEvent<CumulativeBlastColorChallengeData>> _cumulativeBlastColorPublisher;
        private IPublisher<ChallengeUpdateEvent<CumulativeBlastSickChallengeData>> _cumulativeBlastSickPublisher;
        private IPublisher<ChallengeUpdateEvent<TutorialChallengeData>> _tutorialChallengePublisher;
        private IPublisher<ChallengeUpdateEvent<FitMasterChallengeData>> _fitMasterChallengePublisher;
        public Dictionary<Guid, IChallenge> ChallengeDictionary { get; private set; }= new();
        private readonly Queue<Action> _saveDataQueue = new();
        #endregion
        
        #region Events
        private void OnEnable()
        {
            //clone the challenge presets to avoid modifying the original data
            ChallengePresets = ChallengePresets.Select(x => x.Clone()).ToList();
            ChallengeDictionary = ChallengePresets
                .SelectMany(x => x.Challenges)
                .ToDictionary(k => k.ChallengeGuid, v => v);
            JsonSaveManager.OnLoadCompleted += LoadPlayerData;
            JsonSaveManager.OnSaveReady += OnSaveReady;
            _cumulativeScorePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeScoreChallengeData>>();
            _tutorialChallengePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<TutorialChallengeData>>();
            _cumulativeFitMePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeFitMeChallengeData>>();
            _cumulativeBlastPublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeBlastChallengeData>>();
            _cumulativeBlastColorPublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeBlastColorChallengeData>>();
            _cumulativeBlastSickPublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<CumulativeBlastSickChallengeData>>();
            _fitMasterChallengePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<FitMasterChallengeData>>();
        }

        private void OnGlobalMessagePipeSet()
        {
            
        }

        private void OnDisable()
        {
            MessagePipeLifetimeScope.OnGlobalMessagePipeSet -= OnGlobalMessagePipeSet;
            JsonSaveManager.OnLoadCompleted -= LoadPlayerData;
            JsonSaveManager.OnSaveReady -= OnSaveReady;
        }

        private void OnDestroy()
        {
            ChallengeDictionary.Values.ForEach(c => c.Dispose());
        }

        private void OnSaveReady()
        {
            if (_saveDataQueue.Count == 0) return;
            var action = _saveDataQueue.Dequeue();
            action.Invoke();
        }
        #endregion

        #region Save/Load
        private void LoadPlayerData()
        {
            ChallengeDictionary.Values.ForEach(c => c.Initialize());
            JsonSaveManager.Instance.TryGetData(GameDataKey, GameData);
            JsonSaveManager.Instance.TryGetData(PlayerRecordKey, PlayerRecordData);
            JsonSaveManager.Instance.TryGetData(ChallengeDataKey, ChallengeData);
            JsonSaveManager.Instance.TryGetData(TutorialDataKey, TutorialData);
            ValidateChallengeLoad();
        }

        public void SaveScoreChange(uint scoreChange, bool saveToService = false)
        {
            PlayerRecordData.cumulativeScore += scoreChange;
            UpdateSaveData(PlayerRecordKey, PlayerRecordData, saveToService);
            _cumulativeScorePublisher.Publish(new ChallengeUpdateEvent<CumulativeScoreChallengeData>(
                new CumulativeScoreChallengeData(PlayerRecordData.cumulativeScore)));
        }
        
        public void SaveFitMeChange(uint fitMe, bool saveToService = false)
        {
            PlayerRecordData.cumulativeFitMe += fitMe;
            UpdateSaveData(PlayerRecordKey, PlayerRecordData, saveToService);
            _cumulativeFitMePublisher.Publish(new ChallengeUpdateEvent<CumulativeFitMeChallengeData>(
                new CumulativeFitMeChallengeData(PlayerRecordData.cumulativeFitMe)));
        }
        
        public void SaveRecord(uint score, uint fitMe, bool saveToService = false)
        {
            var newHighScore = score > PlayerRecordData.highScore.score;
            var newMostFitMe = fitMe > PlayerRecordData.mostFitMe.fitMe;
            var newEntry = new PlayerRecordData.RunData
            {
                dateTime = DateTime.Now,
                score = score,
                fitMe = fitMe
            };

            PlayerRecordData.runData.Add(newEntry);
            PlayerRecordData.runData = PlayerRecordData.runData.OrderByDescending(s => s.dateTime).Take((int)maxHighScoresEntries).ToList();
            if (newHighScore)
            {
                PlayerRecordData.highScore = newEntry;
            }
            if (newMostFitMe)
            {
                PlayerRecordData.mostFitMe = newEntry;
            }
            UpdateSaveData(PlayerRecordKey, PlayerRecordData, saveToService);
        }

        public void SaveTutorialCompletion(bool saveToService = false)
        {
            TutorialData.completedTutorial = true;
            UpdateSaveData(TutorialDataKey, TutorialData, saveToService);
            _tutorialChallengePublisher.Publish(new ChallengeUpdateEvent<TutorialChallengeData>(
                new TutorialChallengeData()));
        }

        public void SaveBlockDestroyed(FitType fitType, List<(BlockState beforeExplodeState, BlockTypes blockType)> destroyedBlocks, bool saveToService = false)
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
            UpdateSaveData(GameDataKey, GameData, saveToService);
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
                if (ChallengeDictionary.TryGetValue(guid, out var challenge))
                {
                    challenge.SetChallengeData(ChallengeData.challenges[guid]);
                    continue;
                }
                ChallengeData.challenges.Remove(guid);
                Debug.LogWarning($"Challenge with GUID {guid} not found in current challenges. Removing from save data.");
            }
            foreach (var guid in ChallengeDictionary.Keys.ToList())
            {
                ChallengeData.challenges.TryAdd(guid, null);
            }
        }
        
        public void SaveChallenges(Guid challengeGuid, SavableChallengeData savable, bool saveToService = false)
        {
            if (!ChallengeDictionary.ContainsKey(challengeGuid))
            {
                Debug.LogError($"Challenge with GUID {challengeGuid} not found in current challenges.");
                return;
            }
            ChallengeData.challenges[challengeGuid] = savable;
            UpdateSaveData(ChallengeDataKey, ChallengeData, saveToService);
            var thisChallenge = ChallengeDictionary[challengeGuid];
            if (thisChallenge is FitMasterChallenge) return;
            if (ChallengeDictionary.Values.Where(x => x is not FitMasterChallenge).All(x => x.Completed))
            {
                _fitMasterChallengePublisher?.Publish(new ChallengeUpdateEvent<FitMasterChallengeData>(
                    new FitMasterChallengeData()));
            }
        }

        private void UpdateSaveData<T>(string id, T data, bool saveToService = false)
        {
            Action updateDataAction = () =>
            {
                //CurrentSaveFile.AddOrUpdateData(id, data);
                JsonSaveManager.Instance.AddOrUpdateData(id, data, false).Forget();
                JsonSaveManager.Instance.Save(saveToService).Forget();
            };
            if (JsonSaveManager.Instance.Saving)
            {
                _saveDataQueue.Enqueue(updateDataAction);
            }
            else
            {
                updateDataAction.Invoke();
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