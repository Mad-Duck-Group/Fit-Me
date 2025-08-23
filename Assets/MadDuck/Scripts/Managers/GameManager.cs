using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Challenges;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Panels.Gameplay;
using MadDuck.Scripts.UIs.Transitions;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils.Inspectors;using MessagePipe;
using ObservableCollections;
using PrimeTween;
using R3;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using TMPro;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VContainer;
using Random = UnityEngine.Random;

#region Enums
public enum ScoreTypes
{
    Placement,
    PreInfect,
    Combo,
    Bomb,
    FitMe,
}

public enum GameState
{
    CountOff,
    Pause,
    PlaceBlock,
    UseItem,
    GameOver,
    GameClear,
    Tutorial,
}

public enum GameplayUIPanelType
{
    CountOff,
    Gameplay,
    Pause,
    GameOver,
    Result,
}
#endregion

#region Requests
public struct GameStateRequest{}
public struct InfectionConfigRequest{}

public struct InfectionConfig
{
    public int maxInfectionCount;
    public Vector2 infectionTimeRange;
    public Color infectionColor;
    public float preInfectTime;
}

#endregion

[Serializable]
public struct GameDifficultySettings
{
    [field: SerializeField] public float MaxScorePerDifficulty{ get; private set; }
    [field: SerializeField] public Vector2Int InfectionCountRange { get; private set; }
    [field: SerializeField] public Vector2 InfectionTimeRange { get; private set; }
    [field: SerializeField] public Vector2 FirstInfectTimeRange { get; private set; }
    [field: SerializeField] public float PreInfectTime { get; private set; }
    [ShowInInspector, ReadOnly] public bool CanInfect => InfectionCountRange.x >= 1;
}
[ShowOdinSerializedPropertiesInInspector]
public class GameManager : MonoSingleton<GameManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization,
    IRequestHandler<GameStateRequest, GameState>,
    IRequestHandler<InfectionConfigRequest, InfectionConfig>
{
    #region Inspectors

    #region References
    [HideIfGroup("References", Condition = InspectorSettings.GameDesignerModeKey)]
    [BoxGroup("References/Box", LabelText = "References", CenterLabel = true)]
    [ShowInInspector, HideLabel] private InspectorVoid _referencesTitle;
    
    #endregion

    #region Settings
    [Title("Settings")]
    [ShowInInspector, HideLabel] private InspectorVoid _settingsTitle;
    
    [TabGroup("Settings", "General")]
    [SerializeField] private bool tutorialMode;
    
    [TabGroup("Settings", "Score")]
    [SerializeField] private int scorePerPlacement = 100;
    [TabGroup("Settings", "Score")]
    [SerializeField] private int scorePerPreInfect = 50;
    [TabGroup("Settings", "Score")]
    [SerializeField] private int scorePerCombo = 100;
    [TabGroup("Settings", "Score")]
    [SerializeField] private int scorePerBomb = 200;
    [TabGroup("Settings", "Score")]
    [SerializeField] private int scorePerFitMe = 10000;
    
    [TabGroup("Settings", "Infection")] 
    [SerializeField] private bool infectionThreshold;
    [TabGroup("Settings", "Infection")]
    [SerializeField, ShowIf(nameof(infectionThreshold))] 
    private List<GameDifficultySettings> gameDifficultySettings;
    [TabGroup("Settings", "Infection"), HideIf(nameof(infectionThreshold))] 
    [SerializeField] private bool usePercentage;
    [TabGroup("Settings", "Infection")] [SerializeField, HideIf(nameof(usePercentage)), HideIf(nameof(infectionThreshold))] 
    private float startInfectTimeRange = 10f;
    [TabGroup("Settings", "Infection")] [SerializeField, ShowIf(nameof(usePercentage)), MinValue(0.1f), HideIf(nameof(infectionThreshold))] 
    private Vector2 firstInfectTimePercentRange = new(0.1f, 0.5f);
    [field: TabGroup("Settings", "Infection"), HideIf(nameof(infectionThreshold))] 
    [field: SerializeField] public float PreInfectTime { get; private set; } = 1f;
    [field: TabGroup("Settings", "Infection"), HideIf(nameof(infectionThreshold))] 
    [field: SerializeField, MinValue(0.1f)] public Vector2 InfectionTimeRange { get; private set; } = new(0, 10);
    [TabGroup("Settings", "Infection"),] 
    [SerializeField, HideIf(nameof(infectionThreshold))] private int startInfectionCount = 1;
    
    [TabGroup("Settings", "Infection"),] 
    [field: SerializeField, HideIf(nameof(infectionThreshold))] public int MaxInfectionCount { get; private set; }= 10;

    [TabGroup("Settings", "Infection")]
    public Color32 infectColor = new(255, 0, 0, 255);
    #endregion
    
    #region Panels
    [Title("Panels")]
    [ShowInInspector, HideLabel] private InspectorVoid _panelsTitle;
    
    [OdinSerialize] private SerializableDictionary<GameplayUIPanelType, IUIPanel> panelDictionary = new();
    
    [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule countOffCrossFadeRule = new();
    #endregion

    #region Audios
    [Title("Audios")]
    [ShowInInspector, HideLabel] private InspectorVoid _audiosTitle;
    
    [TabGroup("Audios", "BGM")]
    [SerializeField] private EventReference gameplayBgm;
    [TabGroup("Audios", "BGM")]
    [SerializeField] private EventReference resultBgm;
    
    [TabGroup("Audios", "SFX")]
    [SerializeField] private EventReference gameOverSfx;
    #endregion
    
    #region Debug
    [field: TitleGroup("Game Manager Debug")]
    [field: SerializeField, DisplayAsString]
    [field: TitleGroup("Game Manager Debug")]
    public SerializableReactiveProperty<GameState> CurrentGameState { get; private set; } = new(GameState.CountOff);
    [field: TitleGroup("Game Manager Debug")]
    [field: SerializeField, ReadOnly] public SerializableReactiveProperty<int> Score { get; private set; } = new(0);
    [field: TitleGroup("Game Manager Debug")]
    [field: SerializeField, ReadOnly] public SerializableReactiveProperty<int> FitmeScore { get; private set; } = new(0);
    [TitleGroup("Game Manager Debug")]
    [Button("Test Game Over")]
    private void TestGameOver() => GameOver();

    [field: Title("Infection Debug")]
    [SerializeField, DisplayAsString] private int difficultyIndex;
    [ShowInInspector, ReadOnly] private GameDifficultySettings CurrentGameDifficultySettings => 
        gameDifficultySettings.Count > 0 ? gameDifficultySettings[difficultyIndex] : default;
    [SerializeField, DisplayAsString] private bool hostInfectionRunning;
    #endregion
    #endregion

    #region Fields and Properties
    [Inject] private UIPanelController _panelController;
    private readonly List<float> _listInfectTime = new();
    private readonly List<Block> _aboutToInfectBlocks = new();
    private GameState _beforePauseState;
    public GameState BeforePauseState { get => _beforePauseState; set => _beforePauseState = value; }

    private bool _sceneActivated;
    private int _previousReRollScore;
    private bool _countDownPlayed;
    private AudioReference _bgmReference;
    public static event Action OnStartTutorial;
    public static event Action<bool> OnGameOver;
    public delegate void ScoreAddedDelegate(ScoreTypes scoreTypes, int previous, int current, Vector3 position);
    public static event ScoreAddedDelegate OnScoreAdded;
    public static event ScoreAddedDelegate OnFitMeAdded;
    private IPublisher<StartSpawnEvent> _startSpawnPublisher;
    private IPublisher<SceneActivateEvent> _sceneActivatePublisher;
    private IPublisher<ChallengeUpdateEvent<FailChallengeData>> _failChallengeUpdatePublisher;
    private IDisposable _infectionDisposable;
    private IDisposable _blockOnGridCountDisposable;
    #endregion
    
    #region Initialization
    
    private void Initialize()
    {
        panelDictionary.Values.ForEach(p =>
        {
            p.Initialize();
            p.PanelController = _panelController;
        });
        var gameplayPanel = panelDictionary[GameplayUIPanelType.Gameplay];
        _panelController.ShowPanel(gameplayPanel).Forget();
        gameplayPanel.DeactivateInput();
        NextGameDifficulty();
        ActivateScene();
    }
    
    private void ActivateScene()
    {
        if (_sceneActivated) return;
        _sceneActivated = true;
        _sceneActivatePublisher.Publish(new SceneActivateEvent(SceneType.Gameplay));
        LateSubscribe();
        if (!tutorialMode)
            StartCountOff();
        else
            StartTutorial();
    }
    
    private void StartTutorial()
    {
        CurrentGameState.Value = GameState.Tutorial;
        _bgmReference = AudioManager.Instance.PlayAudio(gameplayBgm, transform.position);
        OnStartTutorial?.Invoke();
    }
    
    /// <summary>
    /// Update the count off timer
    /// </summary>
    private void StartCountOff()
    {
        CurrentGameState.Value = GameState.CountOff;
        var gameplayPanel = panelDictionary[GameplayUIPanelType.Gameplay];
        _panelController.ChangePanel(gameplayPanel, countOffCrossFadeRule.nextPanel, countOffCrossFadeRule.crossFadeSettings).Forget();
    }

    public void GameStart()
    {
        CurrentGameState.Value = GameState.PlaceBlock;
        _bgmReference = AudioManager.Instance.PlayAudio(gameplayBgm, transform.position);
        _startSpawnPublisher.Publish(new StartSpawnEvent());
    }
    #endregion

    #region Events
    private void OnEnable()
    {
        _startSpawnPublisher = GlobalMessagePipe.GetPublisher<StartSpawnEvent>();
        _sceneActivatePublisher = GlobalMessagePipe.GetPublisher<SceneActivateEvent>();
        _failChallengeUpdatePublisher = GlobalMessagePipe.GetPublisher<ChallengeUpdateEvent<FailChallengeData>>();
        LoadSceneManager.OnFinishLoad += Initialize;
        LoadSceneManager.OnStartFadeOut += OnSceneChanged;
        GridManager.OnBlockDestroyed += OnPreInfectBlockDestroyed;
        GridManager.OnBlockStateChanged += OnBlockInfected;
        GridManager.OnBlockPlaced += OnBlockPlaced;
        GridManager.OnScoreAdded += AddScore;
        GridManager.OnNextGameDifficulty += NextGameDifficulty;
        BlockManager.OnGameOver += GameOver;
    }

    private void LateSubscribe()
    {
        _blockOnGridCountDisposable = GridManager.Instance.BlocksOnGrid
            .ObserveCountChanged(true).Subscribe(OnBlockOnGridCountChanged);
    }

    private void OnDisable()
    {
        LoadSceneManager.OnFinishLoad -= Initialize;
        LoadSceneManager.OnStartFadeOut -= OnSceneChanged;
        GridManager.OnBlockDestroyed -= OnPreInfectBlockDestroyed;
        GridManager.OnBlockStateChanged -= OnBlockInfected;
        GridManager.OnBlockPlaced -= OnBlockPlaced;
        GridManager.OnScoreAdded -= AddScore;
        GridManager.OnNextGameDifficulty -= NextGameDifficulty;
        BlockManager.OnGameOver -= GameOver;
        _blockOnGridCountDisposable?.Dispose();
    }

    private void OnSceneChanged()
    {
        _bgmReference.Stop();
        panelDictionary.Values.ForEach(x => x.DeactivateInput());
    }

    private void OnBlockPlaced(Block block)
    {
        if (GridManager.Instance.BlocksOnGrid.Count == 0) return;
        if (GridManager.Instance.TotalInfected != 0) return;
        if (hostInfectionRunning) return;
        RandomSpawnInfection();
    }

    private void OnBlockOnGridCountChanged(int newCount)
    {
        if (newCount != 0) return;
        _infectionDisposable?.Dispose();
        hostInfectionRunning = false;
    }

    private void OnPreInfectBlockDestroyed(Block block)
    {
        if (block.beforeExplodeState != BlockState.PreInfected) return;
        AddScore(ScoreTypes.PreInfect, worldPosition: block.transform.position);
    }

    private void OnBlockInfected(Block block)
    {
        if (block.BlockState is not BlockState.Infected) return;
        if (!_aboutToInfectBlocks.Contains(block)) return;
        _aboutToInfectBlocks.Remove(block);
    }
    #endregion

    #region Updates
    private void RandomSpawnInfection()
    {
        if (!CurrentGameDifficultySettings.CanInfect) return;
        if (GridManager.Instance.TotalInfected >= CurrentGameDifficultySettings.InfectionCountRange.x)
        {
            _infectionDisposable?.Dispose();
            hostInfectionRunning = false;
            return;
        }
        _infectionDisposable?.Dispose();
        CalculateInfectTime();
        float delay = _listInfectTime[0];
        hostInfectionRunning = true;
        _infectionDisposable = Observable
            .Timer(TimeSpan.FromSeconds(delay))
            .Subscribe(_ =>
            {
                if (GridManager.Instance.InfectRandomBlock(out var block))
                    _aboutToInfectBlocks.Add(block);
                hostInfectionRunning = false;
            });
    }

    /// <summary>
    /// เอาไว้กันไม่ให้ติดเชื้อทันทีหลังจากใช้ไอเทม
    /// </summary>
    public void ProtectedState()
    {
        CalculateInfectTime();
    }
    
    /// <summary>
    /// Change the game the next difficulty
    /// </summary>
    public void NextGameDifficulty()
    {
        _aboutToInfectBlocks.Clear();
        if (gameDifficultySettings.Count == 0) return;
        if (Score.Value >= CurrentGameDifficultySettings.MaxScorePerDifficulty)
        {
            difficultyIndex++;
            if (difficultyIndex >= gameDifficultySettings.Count)
                difficultyIndex = gameDifficultySettings.Count - 1;
        }
        
        SetValueToDifficulty(CurrentGameDifficultySettings);
    }

    private void SetValueToDifficulty(GameDifficultySettings gameDifficulty)
    {
        var minCount = gameDifficulty.InfectionCountRange.x;
        var maxCount = gameDifficulty.InfectionCountRange.y;
        difficultyIndex = gameDifficultySettings.IndexOf(gameDifficulty);
        startInfectionCount = minCount;
        firstInfectTimePercentRange = gameDifficulty.FirstInfectTimeRange;
        PreInfectTime = gameDifficulty.PreInfectTime;
        InfectionTimeRange = gameDifficulty.InfectionTimeRange;
        MaxInfectionCount = maxCount;
    }
    #endregion

    #region Utils
    /// <summary>
    /// Change the score by the given value
    /// </summary>
    /// <param name="value"></param>
    private void ChangeScore(int value)
    {
        Score.Value += value;
    }
    
    private void ChangeFitMe(int value)
    {
        FitmeScore.Value += value;
    }

    public void AddScore(ScoreTypes scoreType, int contactedAmount = 0, Vector3 worldPosition = default)
    {
        int finalScore = 0;
        var previousScore = Score.Value;
        var previousFitMe = FitmeScore.Value;
        if (worldPosition == default)
            worldPosition = GridManager.Instance.GetGridCenter();
        switch (scoreType)
        {
            case ScoreTypes.Placement:
                finalScore = scorePerPlacement;
                break;
            case ScoreTypes.PreInfect:
                finalScore = scorePerPreInfect;
                break;
            case ScoreTypes.Combo:
                if (contactedAmount <= 1) return;
                finalScore = scorePerCombo * (contactedAmount - 1);
                break;
            case ScoreTypes.Bomb:
                if (contactedAmount <= 2) return;
                finalScore = scorePerBomb * contactedAmount;
                break;
            case ScoreTypes.FitMe:
                ChangeFitMe(1);
                OnFitMeAdded?.Invoke(scoreType, previousFitMe, FitmeScore.Value, worldPosition);
                finalScore = scorePerFitMe;
                break;
        }
        ChangeScore(finalScore);
        OnScoreAdded?.Invoke(scoreType, previousScore,Score.Value, worldPosition);
    }

    private void CalculateInfectTime()
    {
        _listInfectTime.Clear();
        switch (infectionThreshold)
        {
            case true:
                for (int i = GridManager.Instance.TotalInfected; i < CurrentGameDifficultySettings.InfectionCountRange.x; i++)
                {
                    _listInfectTime.Add(Random.Range(
                        CurrentGameDifficultySettings.FirstInfectTimeRange.x, 
                        CurrentGameDifficultySettings.FirstInfectTimeRange.y));
                }
                break;
            
            case false:
                for (int i = GridManager.Instance.TotalInfected; i < startInfectionCount; i++)
                {
                    _listInfectTime.Add(Random.Range(firstInfectTimePercentRange.x, firstInfectTimePercentRange.y));
                }
                break;
        }
        _listInfectTime.Sort();
    }
    #endregion
    
    #region Pause
    public void PauseGame()
    {
        if (CurrentGameState.Value is GameState.CountOff or GameState.GameOver or GameState.GameClear) return;
        _beforePauseState = CurrentGameState.Value;
        CurrentGameState.Value = GameState.Pause;
    }
    
    public void ResumeGame()
    {
        if (CurrentGameState.Value is GameState.CountOff or GameState.GameOver or GameState.GameClear) return;
        CurrentGameState.Value = _beforePauseState;

    }
    #endregion
    
    #region Game Over
    public void GameOver()
    {
        CurrentGameState.Value = GameState.GameOver;
        AudioManager.Instance.PlayAudio(gameOverSfx, transform.position);
        Debug.Log("Game Over!");
        GridManager.Instance.StopAllPreInfectFlash();
        OnGameOver?.Invoke(!tutorialMode);
        _failChallengeUpdatePublisher.Publish(new ChallengeUpdateEvent<FailChallengeData>(new FailChallengeData()));
    }
    #endregion

    #region Scene Change
    public void BackToMenu()
    {
        LoadSceneManager.Instance.LoadScene(SceneType.MainMenu, LoadSceneMode.Single, false).Forget();
    }

    public void Retry()
    {
        LoadSceneManager.Instance.ReloadScene(LoadSceneMode.Single, false);
    }
    #endregion

    #region Another Button
    public async UniTaskVoid ToResultScreen()
    {
        _bgmReference.Stop();
        _bgmReference = AudioManager.Instance.PlayAudio(resultBgm, transform.position);
    }
    
    public void Continue()
    {
        CurrentGameState.Value = GameState.PlaceBlock;
        GridManager.Instance.ClearGrid().Forget();
    }
    #endregion
    
    #region Requests
    public GameState Invoke(GameStateRequest request) => CurrentGameState.Value;
    public InfectionConfig Invoke(InfectionConfigRequest request)
    {
        return new InfectionConfig()
        {
            maxInfectionCount = MaxInfectionCount,
            infectionTimeRange = InfectionTimeRange,
            infectionColor = infectColor,
            preInfectTime = PreInfectTime
        };
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
