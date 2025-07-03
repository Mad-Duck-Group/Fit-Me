using System;
using System.Collections.Generic;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils.Inspectors;
using PrimeTween;
using R3;
using Sirenix.OdinInspector;
using TMPro;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;
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
#endregion

[Serializable]
public struct GameDifficultySettings
{
    [field: SerializeField] public float MaxScorePerDifficulty{ get; private set; }
    [field: SerializeField] public Vector2Int InfectionCountRange { get; private set; }
    [field: SerializeField] public Vector2 InfectionTimeRange { get; private set; }
    [field: SerializeField] public Vector2 FirstInfectTimeRange { get; private set; }
    [field: SerializeField] public float PreInfectTime { get; private set; }
}

public class GameManager : MonoSingleton<GameManager>
{
    #region Inspectors

    #region References
    [HideIfGroup("References", Condition = InspectorSettings.GameDesignerModeKey)]
    [BoxGroup("References/Box", LabelText = "References", CenterLabel = true)]
    [SerializeField, HideLabel] private InspectorVoid referencesTitle;
    [TabGroup("References/Box/Tab", "Timer")]
    [SerializeField] private Slider timerSlider;
    [TabGroup("References/Box/Tab", "Timer")]
    [SerializeField] private Image timerFill;
    [TabGroup("References/Box/Tab", "Timer")]
    [SerializeField] private Color startColor = Color.green;
    [TabGroup("References/Box/Tab", "Timer")]
    [SerializeField] private Color endColor = Color.red;
    
    [TabGroup("References/Box/Tab", "Count Off")]
    [SerializeField] private GameObject countOffPanel;
    [TabGroup("References/Box/Tab", "Count Off")]
    [SerializeField] private TMP_Text countOffText;
    
    [TabGroup("References/Box/Tab", "Pause")]
    [SerializeField] private GameObject pausePanel;
    [TabGroup("References/Box/Tab", "Pause")]
    [SerializeField] private Slider volumeSlider;
    
    [TabGroup("References/Box/Tab", "Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [TabGroup("References/Box/Tab", "Game Over")]
    [SerializeField] private TMP_Text gameOverText;
    [TabGroup("References/Box/Tab", "Game Over")]
    [SerializeField] private Button retryButton;
    
    [TabGroup("References/Box/Tab", "Score")]
    [SerializeField] private TMP_Text scoreText;
    #endregion

    #region Settings
    [Title("Settings")]
    [SerializeField, HideLabel] private InspectorVoid settingsTitle;
    //[TabGroup("Settings", "Timer")] [SerializeField] private float gameTimer = 60f;
    [TabGroup("Settings", "Timer")]
    [SerializeField] private float bombTimeBonus = 10f;
    
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

    #region Debug
    [field: Title("Game Manager Debug")]
    [field: SerializeField, DisplayAsString]
    public SerializableReactiveProperty<GameState> CurrentGameState { get; private set; } = new(GameState.CountOff);
    [SerializeField, Sirenix.OdinInspector.ReadOnly] private int score;
    
    [field: Title("Infection Debug")]
    [SerializeField, DisplayAsString] private int difficultyIndex;

    [ShowInInspector, Sirenix.OdinInspector.ReadOnly] private int CurrentInfectionCount => GridManager.Instance.InfectionCount;
    [SerializeField, Sirenix.OdinInspector.ReadOnly] private bool canInfect;
    [SerializeField, Sirenix.OdinInspector.ReadOnly] private float runningTime;
    #endregion
    #endregion

    #region Fields and Properties
    private readonly List<float> _listInfectTime = new();
    private int _listInfectIndex;
    private GameState _beforePauseState;
    private bool _sceneActivated;
    private int _previousReRollScore;
    //private float _currentGameTimer;
    private bool _countDownPlayed;
    public static event Action OnSceneActivated;
    #endregion
    
    #region Initialization
    void Start()
    {
        CurrentGameState.Value = GameState.CountOff;
        //_currentGameTimer = gameTimer;
        gameOverPanel.SetActive(false);
        gameOverText.transform.localScale = Vector3.zero;
        pausePanel.SetActive(false);
        NextGameDifficulty();
        UpdateScoreText(false);
        volumeSlider.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        retryButton.onClick.AddListener(() =>
        {
            LoadSceneManager.Instance.ReloadScene(LoadSceneMode.Single, false);
        });
        ActivateScene();
    }
    
    public void ActivateScene()
    {
        if (_sceneActivated) return;
        _sceneActivated = true;
        OnSceneActivated?.Invoke();
        StartCountOff();
    }
    
    /// <summary>
    /// Update the count off timer
    /// </summary>
    private void StartCountOff()
    {
        if (countOffTime <= 0)
        {
            CurrentGameState.Value = GameState.PlaceBlock;
            countOffPanel.SetActive(false);
            RandomBlockManager.Instance.SpawnAtStart();
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
                    CurrentGameState.Value = GameState.PlaceBlock;
                    countOffPanel.SetActive(false);
                    RandomBlockManager.Instance.SpawnAtStart();
                })
            .AddTo(this);
    }
    #endregion
    
    #region Events

    private void OnEnable()
    {
        GridManager.OnBlockDestroyed += OnBlockDestroyed;
    }

    private void OnDisable()
    {
        GridManager.OnBlockDestroyed -= OnBlockDestroyed;
    }

    private void OnBlockDestroyed(Block block)
    {
        if (!aboutToInfectBlocks.Contains(block)) return;
        aboutToInfectBlocks.Remove(block);
        CalculateInfectTime();
        _listInfectIndex = 0;
        runningTime = 0;
    }

    #endregion
    
    #region Updates
    void Update()
    {
        if (!_sceneActivated) return;
        //UpdateGameTimer();
        UpdateInfectionTimer();
    }

    
    /*
    /// <summary>
    /// Update the game timer
    /// </summary>
    private void UpdateGameTimer()
    {
        if (CurrentGameState.Value is GameState.CountOff or GameState.Pause) return;
        _currentGameTimer -= Time.deltaTime;
        timerSlider.value = _currentGameTimer / gameTimer;
        Color color = Color.Lerp(endColor, startColor, _currentGameTimer / gameTimer);
        timerFill.color = color;
        switch (_currentGameTimer)
        {
            case > 10 when _countDownPlayed:
                _countDownPlayed = false;
                break;
            case <= 10 when !_countDownPlayed:
                _countDownPlayed = true;
                break;
        }
        if (_currentGameTimer <= 0 && CurrentGameState.Value is not (GameState.GameClear or GameState.GameOver))
        {
            GameOver();
        }
    }
    */

    /// <summary>
    /// Update the infection timer
    /// </summary>
    private List<Block> aboutToInfectBlocks = new();
    private void UpdateInfectionTimer()
    {
        if (CurrentGameState.Value is not (GameState.PlaceBlock or GameState.UseItem)) return;
        if (GridManager.Instance.BlocksOnGrid.Count > 0)
        {
            runningTime += Time.deltaTime;
        }
        else
        {
            runningTime = 0;
        }


        if (infectionThreshold)
        {
            if (!canInfect) return;
            if (_listInfectIndex < 0 || _listInfectIndex >= _listInfectTime.Count) return;
            if (runningTime < _listInfectTime[_listInfectIndex]) return;
        }
        else
        {
            if (!usePercentage)
            {
                if (runningTime < startInfectTimeRange || CurrentInfectionCount >= startInfectionCount) return;
            }
            else
            {
                if (_listInfectIndex < 0 || _listInfectIndex >= _listInfectTime.Count) return;
                if (runningTime < _listInfectTime[_listInfectIndex]) return;
            }
        }

        if (startInfectionCount == 0 || CurrentInfectionCount >= startInfectionCount) return;

        Debug.Log("Have Infect");
        if (!GridManager.Instance.InfectRandomBlock(out var infectedBlock))
        {
            CalculateInfectTime();
            _listInfectIndex = 0;
            runningTime = 0;
            return;
        }
        aboutToInfectBlocks.Add(infectedBlock);
        //CurrentInfectionCount++;
        _listInfectIndex++;
    }
    
    
    
    /// <summary>
    /// เอาไว้กันไม่ให้ติดเชื้อทันทีหลังจากใช้ไอเทม
    /// </summary>
    public void ProtectedState()
    {
        if (CurrentInfectionCount <= 0) return;
        //CurrentInfectionCount--;
        
        if (_listInfectIndex > 0) _listInfectIndex--;
        runningTime = 0;
        CalculateInfectTime();
    }
    
    /// <summary>
    /// Change the game the next difficulty
    /// </summary>
    public void NextGameDifficulty()
    {
        if (gameDifficultySettings.Count == 0) return;
        if (score >= gameDifficultySettings[difficultyIndex].MaxScorePerDifficulty)
        {
            difficultyIndex++;
            if (difficultyIndex >= gameDifficultySettings.Count)
                difficultyIndex = gameDifficultySettings.Count - 1;
        }
        runningTime = 0;
        
        SetValueToDifficulty(gameDifficultySettings[difficultyIndex]);
    }

    private void SetValueToDifficulty(GameDifficultySettings gameDifficulty)
    {
        var minCount = gameDifficulty.InfectionCountRange.x;
        var maxCount = gameDifficulty.InfectionCountRange.y;
        canInfect = minCount >= 1;
        difficultyIndex = gameDifficultySettings.IndexOf(gameDifficulty);
        startInfectionCount = minCount;
        firstInfectTimePercentRange = gameDifficulty.FirstInfectTimeRange;
        PreInfectTime = gameDifficulty.PreInfectTime;
        InfectionTimeRange = gameDifficulty.InfectionTimeRange;
        MaxInfectionCount = maxCount;
        
        CalculateInfectTime();
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
                //ChangeGameTimer(bombTimeBonus);
                break;
            case ScoreTypes.FitMe:
                ChangeScore(scorePerFitMe);
                //ChangeGameTimer(gameTimer);
                break;
        }
        //if (_score - _previousReRollScore < reRollScoreThreshold) return;
        //int reRoll = Mathf.FloorToInt((_score - _previousReRollScore) / (float)reRollScoreThreshold);
        //if (ChangeReRoll(reRoll)) SoundManager.Instance.PlaySoundFX(SoundFXTypes.ReRollGain, out _);
        //_previousReRollScore += reRollScoreThreshold;
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
                for (int i = CurrentInfectionCount; i < startInfectionCount; i++)
                {
                    _listInfectTime.Add(Random.Range(
                        gameDifficultySettings[difficultyIndex].FirstInfectTimeRange.x, 
                        gameDifficultySettings[difficultyIndex].FirstInfectTimeRange.y));
                }
                break;
            
            case false:
                for (int i = CurrentInfectionCount; i < startInfectionCount; i++)
                {
                    _listInfectTime.Add(Random.Range(firstInfectTimePercentRange.x, firstInfectTimePercentRange.y));
                }
                break;
        }
        _listInfectTime.Sort();
    }
    
    
    
    /*
    public void ChangeGameTimer(float value, bool bump = true)
    {
        float newTimer = _currentGameTimer + value;
        _currentGameTimer = Mathf.Clamp(newTimer, 0, gameTimer);
        if (bump)
        {
            Tween.Scale(timerSlider.transform, 1.2f, 0.1f, cycleMode: CycleMode.Yoyo, cycles: 2);
        }
    }
    */
    
    
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
    
    public void ToggleVolumeSlider()
    {
        volumeSlider.gameObject.SetActive(!volumeSlider.gameObject.activeSelf);
    }
    #endregion
    
    #region Game Over
    public void GameOver(bool fail = false)
    {
        CurrentGameState.Value = GameState.GameOver;
        //_currentGameTimer = 0;
        Debug.Log("Game Over!");
        gameOverText.text = fail ? "Failed!" : "Time's Up!";
        gameOverPanel.SetActive(true);
        Tween.Scale(gameOverText.transform, 1, 0.5f, ease: Ease.OutBounce);
        GridManager.Instance.StopAllPreInfectFlash();
        retryButton.gameObject.SetActive(true);
    }
    #endregion

    #region Scene Change
    public void BackToMenu()
    {
        if (SceneManager.sceneCount > 1) return;
        //SceneManager.LoadScene(SceneNames.MainMenu.ToString());
    }

    public void Retry()
    {
        if (SceneManager.sceneCount > 1) return;
        //LoadSceneManager.Instance.Retry = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    #endregion
}
