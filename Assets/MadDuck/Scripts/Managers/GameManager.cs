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
    
    [Header("Infected Settings")] 
    [SerializeField] private bool usePercentage;
    [SerializeField, HideIf(nameof(usePercentage))] private float startInfectTimeRange = 10f;
    [SerializeField, ShowIf(nameof(usePercentage))] private Vector2 firstInfectTimePercentRange = new(0.1f, 0.5f);
    [SerializeField] private Vector2 infectionTimeRange = new Vector2(0, 10);
    [SerializeField] private int maxInfectionCount = 1;
    private int _currentInfectionCount = 0;
    private List<float> listInfectTimePercent = new List<float>();
    private int _listInfectIndex = 0;
    public Vector2 InfectionTimeRange => infectionTimeRange;
    
    
    private bool _sceneActivated;
    //private int _currentReRoll;
    private int _previousReRollScore;
    private float _currentGameTimer;
    private float _countOffTimer;
    private bool _isGameOver;
    private bool _isGameClear;
    private bool _isPaused;
    private bool _gameStarted;
    private int _score;
    private bool _countDownPlayed;
    public bool IsGameOver => _isGameOver;
    public bool IsGameClear => _isGameClear;
    public bool GameStarted => _gameStarted;
    public bool IsPaused => _isPaused;
    //public int CurrentReRoll => _currentReRoll;
    void Start()
    {
        _currentGameTimer = gameTimer;
        gameOverPanel.SetActive(false);
        gameOverText.transform.localScale = Vector3.zero;
        pausePanel.SetActive(false);
        countOffPanel.SetActive(true);
        if (!usePercentage) { maxInfectionCount = 1; }
        CalculatePercentageInfectTime();
        UpdateScoreText(false);
        //UpdateReRollText(false);
        //reRollButton.interactable = false;
        // volumeSlider.value = SoundManager.Instance.MasterVolume;
        // volumeSlider.onValueChanged.AddListener((_) =>
        // {
        //     SoundManager.Instance.ChangeMixerVolume(volumeSlider.value);
        // });
        volumeSlider.gameObject.SetActive(false);
        // if (LoadSceneManager.Instance.FirstSceneLoaded == SceneManager.GetActiveScene() || LoadSceneManager.Instance.Retry)
        // {
        //     ActivateScene();
        //     LoadSceneManager.Instance.Retry = false;
        // }
        ActivateScene();
    }
    
    public void ActivateScene()
    {
        if (_sceneActivated) return;
        _sceneActivated = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_sceneActivated) return;
        UpdateCountOff();
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
    private void UpdateCountOff()
    {
        if (GameStarted || IsPaused) return;
        _countOffTimer += Time.deltaTime;
        int countOff = Mathf.CeilToInt(countOffTime - _countOffTimer) - 1;
        if (countOff == 0)
        {
            countOffText.text = "GO!";
        }
        else
        {
            countOffText.text = countOff.ToString();
        }
        if (_countOffTimer < countOffTime) return;
        _gameStarted = true;
        _countOffTimer = 0;
        countOffPanel.SetActive(false);
        RandomBlockManager.Instance.SpawnAtStart();
    }

    /// <summary>
    /// Update the game timer
    /// </summary>
    private void UpdateGameTimer()
    {
        if (!GameStarted || IsGameOver || IsPaused || IsGameClear) return;
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
        if (_currentGameTimer <= 0 && !_isGameClear)
        {
            GameOver();
        }
    }

    private void CalculatePercentageInfectTime()
    {
        listInfectTimePercent.Clear();
        for (int i = 0; i < maxInfectionCount; i++)
        {
            listInfectTimePercent.Add(Random.Range(firstInfectTimePercentRange.x, firstInfectTimePercentRange.y) * gameTimer);
        }
        //listInfectTimePercent.Sort((a, b) => b.CompareTo(a));
        listInfectTimePercent.Sort();
        Debug.Log(listInfectTimePercent.Count + "\n" + "1." +  listInfectTimePercent[0] + "\n" + "2." +  listInfectTimePercent[1] + "\n");
    }
    
    private void UpdateSafeInfectedTimer()
    {
        if (!GameStarted || IsGameOver || IsPaused || IsGameClear) return;
        var elapsedTime = gameTimer - _currentGameTimer;
        switch (usePercentage)
        {
            case false:
                if (!(elapsedTime >= startInfectTimeRange) || _currentInfectionCount >= maxInfectionCount) return;
                break;
            case true:
                if (_listInfectIndex < 0 || _listInfectIndex >= listInfectTimePercent.Count) return;
                if (!(elapsedTime >= listInfectTimePercent[_listInfectIndex])) return;
                break;
        }

        if (maxInfectionCount >= 2)
        {
            if (!usePercentage || _listInfectIndex == listInfectTimePercent.Count || _currentInfectionCount >= maxInfectionCount) return; 
            GridManager.Instance.InfectRandomBlock();
            _currentInfectionCount++;
            _listInfectIndex++;
        }
        else
        {
            GridManager.Instance.InfectRandomBlock();
            _currentInfectionCount++;
        }
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
        if (IsGameOver || IsGameClear || !GameStarted) return;
        _isPaused = true;
        pausePanel.SetActive(true);
    }
    
    public void ResumeGame()
    {
        if (IsGameOver || IsGameClear || !GameStarted) return;
        _isPaused = false;
        pausePanel.SetActive(false);
    }
    
    public void GameOver(bool fail = false)
    {
        _isGameOver = true;
        _currentGameTimer = 0;
        Debug.Log("Game Over!");
        gameOverText.text = fail ? "Failed!" : "Time's Up!";
        gameOverPanel.SetActive(true);
        Tween.Scale(gameOverText.transform, 1, 0.5f, ease: Ease.OutBounce);
    }
    
    public void GameClear()
    {
        _isGameClear = true;
        _currentGameTimer = 0;
        Debug.Log("Game Clear!");
        gameClearText.text = "Game Clear!";
        gameClearPanel.SetActive(true);
        Tween.Scale(gameClearText.transform, 1, 0.5f, ease: Ease.OutBounce);
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
