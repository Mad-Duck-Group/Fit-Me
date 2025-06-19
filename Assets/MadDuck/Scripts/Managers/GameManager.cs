using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MessagePipe;
using PrimeTween;
using R3;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using TMPro;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum ScoreTypes
{
    Placement,
    Combo,
    Bomb,
    FitMe,
}

public enum GameDifficulty
{
    Easy,
    Normal,
    Hard,
    Insane
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

[Serializable]
public struct GameDifficultySettings
{
    [BoxGroup] public GameDifficulty difficulty;
    [BoxGroup] public float maxScorePerDifficulty;
    [BoxGroup] public int maxInfectionCount;
    [field: SerializeField] public float PreInfectTime { get; private set; }
    [field: SerializeField, BoxGroup] public Vector2 InfectionTimeRange { get; private set; }
}

public class GameManager : MonoSingleton<GameManager>
{
    [Header("Time Settings")]
    [SerializeField] private float gameTimer = 60f;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private Image timerFill;
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color endColor = Color.red;
    [SerializeField] private float bombTimeBonus = 10f;
    
    [Header("Count Off Settings")]
    [SerializeField] private float countOffTime = 3f;
    [SerializeField] private GameObject countOffPanel;
    [SerializeField] private TMP_Text countOffText;

    // [Header("Reroll Settings")] 
    // [SerializeField] private Button reRollButton;
    // [SerializeField] private TMP_Text reRollText;
    // [SerializeField] private int maxReRoll = 2;
    // [SerializeField] private int reRollScoreThreshold = 5000;

    [Header("Pause Settings")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Slider volumeSlider;
    
    [Header("Game Over Settings")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;
    
    [Header("Game Clear Settings")]
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private TMP_Text gameClearText;
    
    [Header("Score Settings")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private int scorePerPlacement = 100;
    [SerializeField] private int scorePerCombo = 100;
    [SerializeField] private int scorePerBomb = 200;
    [SerializeField] private int scorePerFitMe = 10000;

    [Title("Game Manager Debug")]
    [field: SerializeField, Sirenix.OdinInspector.ReadOnly]
    public SerializableReactiveProperty<GameState> CurrentGameState { get; private set; } = new(GameState.CountOff);
    
    [Header("Infected Settings")] 
    [SerializeField, Redcode.Extensions.ReadOnly] private GameDifficulty difficulty;
    [SerializeField] private List<GameDifficultySettings> gameDifficultySettings;
    [SerializeField] private bool checkGameDifficulty;
    [SerializeField] private bool usePercentage;
    [SerializeField, HideIf(nameof(usePercentage))] private float startInfectTimeRange = 10f;
    [SerializeField, ShowIf(nameof(usePercentage)), MinValue(0.1f)] private Vector2 firstInfectTimePercentRange = new(0.1f, 0.5f);
    [field: SerializeField] public float PreInfectTime { get; private set; } = 1f;
    [field: SerializeField, MinValue(0.1f)] public Vector2 InfectionTimeRange { get; private set; } = new(0, 10);
    [SerializeField] private int maxInfectionCount = 1;
    private int _currentGameDifficultyIndex;
    private int _currentInfectionCount;
    private readonly List<float> _listInfectTimePercent = new();
    private int _listInfectIndex;
    
    [Header("Debug Settings")]
    public Color32 infectColor = new(255, 0, 0, 255);

    private float _runningTime;
    private GameState _beforePauseState;
    private bool _sceneActivated;
    private int _previousReRollScore;
    private float _currentGameTimer;
    private int _score;
    private bool _countDownPlayed;
    public static event Action OnSceneActivated;
    void Start()
    {
        CurrentGameState.Value = GameState.CountOff;
        _currentGameTimer = gameTimer;
        gameOverPanel.SetActive(false);
        gameOverText.transform.localScale = Vector3.zero;
        pausePanel.SetActive(false);
        if (!usePercentage) { maxInfectionCount = 1; }
        CalculatePercentageInfectTime();
        UpdateScoreText(false);
        volumeSlider.gameObject.SetActive(false);
        ActivateScene();
    }
    
    public void ActivateScene()
    {
        if (_sceneActivated) return;
        _sceneActivated = true;
        OnSceneActivated?.Invoke();
        StartCountOff();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_sceneActivated) return;
        UpdateGameTimer();
        UpdateSafeInfectedTimer();
    }
    
    /// <summary>
    /// Change the score by the given value
    /// </summary>
    /// <param name="value"></param>
    public void ChangeScore(int value)
    {
        _score += value;
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
                ChangeGameTimer(bombTimeBonus);
                break;
            case ScoreTypes.FitMe:
                ChangeScore(scorePerFitMe);
                ChangeGameTimer(gameTimer);
                GameDifficulty();
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

        scoreText.text = _score.ToString("N0");
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

    private void CalculatePercentageInfectTime()
    {
        _listInfectTimePercent.Clear();
        for (int i = 0; i < maxInfectionCount; i++)
        {
            _listInfectTimePercent.Add(Random.Range(firstInfectTimePercentRange.x, firstInfectTimePercentRange.y) * gameTimer);
        }
        _listInfectTimePercent.Sort();
    }
    
    private void UpdateSafeInfectedTimer()
    {
        if (CurrentGameState.Value is not (GameState.PlaceBlock or GameState.UseItem)) return;
        _runningTime += Time.deltaTime;
        var elapsedTime = gameTimer - _currentGameTimer;

        switch (checkGameDifficulty)
        {
            case true:
                if (_score < gameDifficultySettings[_currentGameDifficultyIndex].maxScorePerDifficulty) return;
                if (_listInfectIndex < 0 || _listInfectIndex >= _listInfectTimePercent.Count) return;
                if (_runningTime < _listInfectTimePercent[_listInfectIndex]) return;
                break;
            
            case false:
                switch (usePercentage)
                {
                    case false:
                        if (elapsedTime < startInfectTimeRange || _currentInfectionCount >= maxInfectionCount) return;
                        break;
                    case true:
                        if (_listInfectIndex < 0 || _listInfectIndex >= _listInfectTimePercent.Count) return;
                        if (elapsedTime < _listInfectTimePercent[_listInfectIndex]) return;
                        break;
                }
                break;
        }

        switch (maxInfectionCount)
        {
            case 0:
            case >= 2 when !usePercentage || _currentInfectionCount >= maxInfectionCount:
                return;
            case >= 2:
                GridManager.Instance.InfectRandomBlock();
                _currentInfectionCount++;
                _listInfectIndex++;
                break;
            default:
                GridManager.Instance.InfectRandomBlock();
                _currentInfectionCount++;
                break;
        }
    }
    
    public void GameDifficulty()
    {
        if (gameDifficultySettings.Count == 0) return;
        if (_currentGameDifficultyIndex >= gameDifficultySettings.Count) return;
        if (_score >= gameDifficultySettings[_currentGameDifficultyIndex].maxScorePerDifficulty)
        {
            difficulty = gameDifficultySettings[_currentGameDifficultyIndex].difficulty;
            _currentGameDifficultyIndex++;
        }
        
        maxInfectionCount = gameDifficultySettings[_currentGameDifficultyIndex].maxInfectionCount;
        InfectionTimeRange = gameDifficultySettings[_currentGameDifficultyIndex].InfectionTimeRange;
        PreInfectTime = gameDifficultySettings[_currentGameDifficultyIndex].PreInfectTime;
    }

    public void ChangeGameTimer(float value, bool bump = true)
    {
        float newTimer = _currentGameTimer + value;
        _currentGameTimer = Mathf.Clamp(newTimer, 0, gameTimer);
        if (bump)
        {
            Tween.Scale(timerSlider.transform, 1.2f, 0.1f, cycleMode: CycleMode.Yoyo, cycles: 2);
        }
    }

    // public bool ChangeReRoll(int value)
    // {
    //     int before = _currentReRoll;
    //     _currentReRoll += value;
    //     _currentReRoll = Mathf.Clamp(_currentReRoll, 0, maxReRoll);
    //     reRollButton.interactable = _currentReRoll > 0;
    //     if (_currentReRoll == before) return false;
    //     UpdateReRollText();
    //     return true;
    // }
    //
    // private void UpdateReRollText(bool bump = true)
    // {
    //     reRollText.text = $"{_currentReRoll}/{maxReRoll}";
    //     if (bump)
    //     {
    //         Tween.Scale(reRollText.transform, 1.2f, 0.1f, cycleMode: CycleMode.Yoyo, cycles: 2);
    //     }
    // }
    //
    // public void ReRoll()
    // {
    //     if (_currentReRoll <= 0) return;
    //     //if (ChangeReRoll(-1)) SoundManager.Instance.PlaySoundFX(SoundFXTypes.ReRollLose, out _);
    //     RandomBlockManager.Instance.ReRoll();
    // }
    
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
    
    public void GameOver(bool fail = false)
    {
        CurrentGameState.Value = GameState.GameOver;
        _currentGameTimer = 0;
        Debug.Log("Game Over!");
        gameOverText.text = fail ? "Failed!" : "Time's Up!";
        gameOverPanel.SetActive(true);
        Tween.Scale(gameOverText.transform, 1, 0.5f, ease: Ease.OutBounce);
    }

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
    
    public void ToggleVolumeSlider()
    {
        volumeSlider.gameObject.SetActive(!volumeSlider.gameObject.activeSelf);
    }
}
