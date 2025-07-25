using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Panels.Gameplay;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils.Inspectors;
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
using Random = UnityEngine.Random;

#region Enums
public enum ScoreTypes
{
    Placement,
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
    GameClear
}

public enum GameplayUIPanelType
{
    CountOff,
    Gameplay,
    Pause,
    GameOver,
    Result
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
public class GameManager : MonoSingleton<GameManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
{
    #region Inspectors

    #region References
    [HideIfGroup("References", Condition = InspectorSettings.GameDesignerModeKey)]
    [BoxGroup("References/Box", LabelText = "References", CenterLabel = true)]
    [ShowInInspector, HideLabel] private InspectorVoid _referencesTitle;
    
    [TabGroup("References/Box/Tab", "Pause")]
    [SerializeField] private GameObject pausePanel;
    [TabGroup("References/Box/Tab", "Pause")]
    [SerializeField] private Button resumeButton;
    [TabGroup("References/Box/Tab", "Pause")]
    [SerializeField] private Button helpButton;
    [TabGroup("References/Box/Tab", "Pause")]
    [SerializeField] private Button mainMenuButton;
    [TabGroup("References/Box/Tab", "Pause")]
    [SerializeField] private Button closeSFXButton;
    [TabGroup("References/Box/Tab", "Pause")]
    [SerializeField] private Button closeMusicButton;
    
    [TabGroup("References/Box/Tab", "Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [TabGroup("References/Box/Tab", "Game Over")]
    [SerializeField] private TMP_Text gameOverText;
    [TabGroup("References/Box/Tab", "Game Over")]
    [SerializeField] private Button continueButton;
    
    [TabGroup("References/Box/Tab", "Result")]
    [SerializeField] private GameObject resultPanel;
    [TabGroup("References/Box/Tab", "Result")]
    [SerializeField] private TMP_Text resultScoreText;
    [TabGroup("References/Box/Tab", "Result")]
    [SerializeField] private TMP_Text fitScoreText;
    [TabGroup("References/Box/Tab", "Result")]
    [SerializeField] private Button homeButton;
    [TabGroup("References/Box/Tab", "Result")]
    [SerializeField] private Button tryAgainButton;
    
    [TabGroup("References/Box/Tab", "Score")]
    [SerializeField] private TMP_Text scoreText;
    
    [TabGroup("References/Box/Tab", "Other")]
    [SerializeField] private TMP_Text versionText;
    #endregion

    #region Settings
    [Title("Settings")]
    [ShowInInspector, HideLabel] private InspectorVoid _settingsTitle;
    
    [TabGroup("Settings", "Count Off")]
    [SerializeField] private float countOffTime = 3f;
    
    [TabGroup("Settings", "Score")]
    [SerializeField] private int scorePerPlacement = 100;
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
    
    [SerializeField, HideDuplicateReferenceBox, HideLabel]
    private UIPanelController panelController = new();
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
    [TabGroup("Audios", "SFX")]
    [SerializeField] private EventReference giveUpSfx;
    #endregion
    
    #region Debug
    [field: Title("Game Manager Debug")]
    [field: SerializeField, DisplayAsString]
    public SerializableReactiveProperty<GameState> CurrentGameState { get; private set; } = new(GameState.CountOff);
    [SerializeField, Sirenix.OdinInspector.ReadOnly] private int score;
    
    [field: Title("Infection Debug")]
    [SerializeField, DisplayAsString] private int difficultyIndex;
    [ShowInInspector, ReadOnly] private GameDifficultySettings CurrentGameDifficultySettings => 
        gameDifficultySettings.Count > 0 ? gameDifficultySettings[difficultyIndex] : default;
    [SerializeField, DisplayAsString] private bool hostInfectionRunning;
    #endregion
    #endregion

    #region Fields and Properties
    private readonly List<float> _listInfectTime = new();
    private readonly List<Block> _aboutToInfectBlocks = new();
    private GameState _beforePauseState;
    private bool _sceneActivated;
    private int _previousReRollScore;
    private bool _countDownPlayed;
    private AudioReference _bgmReference;
    public static event Action OnSceneActivated;
    private IDisposable _infectionDisposable;
    private IDisposable _blockOnGridCountDisposable;
    #endregion
    
    #region Initialization
    void Start()
    {
        versionText.text = $"{Application.version}";
        CurrentGameState.Value = GameState.CountOff;
        gameOverPanel.SetActive(false);
        gameOverText.transform.localScale = Vector3.zero;
        pausePanel.SetActive(false);
        NextGameDifficulty();
        UpdateScoreText(false);
        panelDictionary.Values.ForEach(p =>
        {
            p.Initialize();
            p.PanelController = panelController;
        });
        ActivateScene();
    }
    
    public void ActivateScene()
    {
        if (_sceneActivated) return;
        _sceneActivated = true;
        OnSceneActivated?.Invoke();
        LateSubscribe();
        StartCountOff();
    }
    
    /// <summary>
    /// Update the count off timer
    /// </summary>
    private void StartCountOff()
    {
        if (!panelDictionary.TryGetValue(GameplayUIPanelType.CountOff, out var panel) || panel is not CountOffScreen countOffScreen)
        {
            Debug.LogError("Count off panel not found in panel dictionary or is not of type CountOffScreen.");
            return;
        }
        countOffScreen.OnCountOffComplete = () => GameStart().Forget();
        panelController.ShowPanel(panel).Forget();
        /*if (countOffTime <= 0)
        {
            GameStart();
            Debug.Log("Count off time is 0 or less, starting game immediately.");
            return;
        }
        countOffPanel.SetActive(true);
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(Mathf.CeilToInt(countOffTime) + 1) // Take 4 values (3, 2, 1, 0)
            .Select((_, i) => Mathf.CeilToInt(countOffTime) - i) // Convert to countdown values
            .Do(current =>  countOffText.text = current.ToString())
            .Subscribe(
                current =>
                {
                    // Update text based on current countdown value
                    countOffText.text = current > 0 ? current.ToString() : "GO!";
                },
                _ =>
                {
                    // On completed (after countdown finishes)
                    GameStart();
                })
            .AddTo(this);*/
    }

    private async UniTaskVoid GameStart()
    {
        await panelController.ChangePanel(panelDictionary[GameplayUIPanelType.CountOff], panelDictionary[GameplayUIPanelType.Gameplay]);
        CurrentGameState.Value = GameState.PlaceBlock;
        _bgmReference = AudioManager.Instance.PlayAudio(gameplayBgm, transform.position);
        BlockManager.Instance.SpawnAtStart();
    }
    #endregion

    #region Events
    private void OnEnable()
    {
        GridManager.OnBlockDestroyed += OnPreInfectBlockDestroyed;
        GridManager.OnBlockStateChanged += OnBlockInfected;
        GridManager.OnBlockPlaced += OnBlockPlaced;
    }

    private void LateSubscribe()
    {
        _blockOnGridCountDisposable = GridManager.Instance.BlocksOnGrid
            .ObserveCountChanged(true).Subscribe(OnBlockOnGridCountChanged);
    }

    private void OnDisable()
    {
        GridManager.OnBlockDestroyed -= OnPreInfectBlockDestroyed;
        GridManager.OnBlockStateChanged -= OnBlockInfected;
        GridManager.OnBlockPlaced -= OnBlockPlaced;
        _blockOnGridCountDisposable?.Dispose();
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
        if (block.BlockState != BlockState.PreInfected) return;
        block.StopFlashing();
        if (!_aboutToInfectBlocks.Contains(block)) return;
        _aboutToInfectBlocks.Remove(block);
        CalculateInfectTime();
        if (GridManager.Instance.TotalInfected > CurrentGameDifficultySettings.InfectionCountRange.x)
            return;
        RandomSpawnInfection();
    }

    private void OnBlockInfected(Block block)
    {
        if (block.BlockState is not BlockState.Infected) return;
        if (!_aboutToInfectBlocks.Contains(block)) return;
        _aboutToInfectBlocks.Remove(block);
    }
    #endregion

    #region Updates
    void Update()
    {
        if (!_sceneActivated) return;
    }

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
        if (score >= CurrentGameDifficultySettings.MaxScorePerDifficulty)
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
    public void ChangeScore(int value)
    {
        score += value;
        UpdateScoreText();
    }

    public void AddScore(ScoreTypes scoreType, int contactedAmount = 0)
    {
        switch (scoreType)
        {
            case ScoreTypes.Placement:
                ChangeScore(scorePerPlacement);
                Debug.Log("Placement Score: " + scorePerPlacement);
                break;
            case ScoreTypes.Combo:
                if (contactedAmount <= 1) return;
                int score = scorePerCombo * (contactedAmount - 1);
                Debug.Log("Combo Score: " + score);
                ChangeScore(score);
                break;
            case ScoreTypes.Bomb:
                if (contactedAmount <= 2) return;
                int bombScore = scorePerBomb * contactedAmount;
                Debug.Log("Bomb Score: " + bombScore);
                ChangeScore(bombScore);
                break;
            case ScoreTypes.FitMe:
                ChangeScore(scorePerFitMe);
                break;
        }
    }

    /// <summary>
    /// Update the score text
    /// </summary>
    private void UpdateScoreText(bool bump = true)
    {
        //Bump animation
        if (bump)
        {
            Tween.Scale(scoreText.transform, 1.2f, 0.1f, cycleMode: CycleMode.Yoyo, cycles: 2);
        }

        scoreText.text = score.ToString("N0");
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
        pausePanel.SetActive(true);
    }
    
    public void ResumeGame()
    {
        if (CurrentGameState.Value is GameState.CountOff or GameState.GameOver or GameState.GameClear) return;
        CurrentGameState.Value = _beforePauseState;
        pausePanel.SetActive(false);
    }
    #endregion
    
    #region Game Over
    public void GameOver()
    {
        CurrentGameState.Value = GameState.GameOver;
        AudioManager.Instance.PlayAudio(gameOverSfx, transform.position);
        Debug.Log("Game Over!");
        gameOverText.text = "Failed!";
        gameOverPanel.SetActive(true);
        Tween.Scale(gameOverText.transform, 1, 0.5f, ease: Ease.OutBounce);
        GridManager.Instance.StopAllPreInfectFlash();
    }
    #endregion

    #region Scene Change
    public void BackToMenu()
    {
        _bgmReference.Stop();
        LoadSceneManager.Instance.LoadScene(SceneType.MainMenu, LoadSceneMode.Single, false);
    }

    public void Retry()
    {
        _bgmReference.Stop();
        LoadSceneManager.Instance.ReloadScene(LoadSceneMode.Single, false);
    }
    #endregion

    #region Another Button

    public void ToggleMuteSFX()
    {
        AudioManager.Instance.ToggleMuteBus(BusType.SFX);
    }

    public void ToggleMuteBGM()
    {
        AudioManager.Instance.ToggleMuteBus(BusType.BGM);
    }

    public void ToResultScreen()
    {
        AudioManager.Instance.PlayAudio(giveUpSfx, transform.position);
        _bgmReference.Stop();
        _bgmReference = AudioManager.Instance.PlayAudio(resultBgm, transform.position);
        resultPanel.gameObject.SetActive(true);
        gameOverPanel.gameObject.SetActive(false);
    }
    
    public void Continue()
    {
        GridManager.Instance.RemoveAllBlocks();
        gameOverPanel.gameObject.SetActive(false);
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
