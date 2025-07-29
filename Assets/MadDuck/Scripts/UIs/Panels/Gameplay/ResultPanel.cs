using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ShowOdinSerializedPropertiesInInspector]
public class ResultPanel : UIPanel
{
    [Title("References")] 
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject newHighScoreBlock;
    [SerializeField] private GameObject newFitMeBlock;
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private TMP_Text fitScoreText;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button tryAgainButton;

    private PlayerDataManager playerData => PlayerDataManager.Instance;
    private GameManager gameManager => GameManager.Instance;
    
    public override void Initialize()
    {
        base.Initialize();
        homeButton.onClick.AddListener(OnHomeButtonClicked);
        tryAgainButton.onClick.AddListener(OnTryAgainButtonClicked);
    }

    private void OnEnable()
    {
        ShowHighScore();
        ShowFitMeScore();
        SaveScoreData();
    }
    
    private void OnHomeButtonClicked()
    {
        gameManager.BackToMenu();
    }

    private void OnTryAgainButtonClicked()
    {
        gameManager.Retry();   
    }
    
    private void ShowHighScore()
    {
        newHighScoreBlock.SetActive(false);
        resultScoreText.text = gameManager.Score.Value.ToString("N0");
        if (gameManager.Score.Value > playerData.ScoreData.highScore)
        {
            newHighScoreBlock.SetActive(true);
        }
    }
    
    private void ShowFitMeScore()
    {
        fitScoreText.text = gameManager.FitmeScore.Value.ToString("N0");
        if (gameManager.Score.Value > playerData.ScoreData.highScore)
        {
            Debug.Log("New FitMe Score!");
        }
    }
    
    private void SaveScoreData()
    {
        playerData.SaveScore((uint)gameManager.Score.Value, false);
        playerData.SaveFitMe((uint)gameManager.FitmeScore.Value, false);
        playerData.FinishSave();
    }
}
