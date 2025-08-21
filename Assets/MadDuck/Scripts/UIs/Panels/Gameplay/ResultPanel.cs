using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using PrimeTween;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.Gameplay
{
    [ShowOdinSerializedPropertiesInInspector]
    public class ResultPanel : UIPanel
    {
        [Title("References")] 
        [SerializeField] private GameObject yourScoreBlock;
        [SerializeField] private GameObject newHighScoreBlock;
        [SerializeField] private GameObject newFitMeBlock;
        [SerializeField] private TMP_Text resultScoreText;
        [SerializeField] private TMP_Text fitScoreText;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button tryAgainButton;
        
        [Title("Audios")]
        [SerializeField] EventReference newHighScoreSfx;
        [SerializeField] EventReference newFitMeSfx;

        [Title("Tween")] 
        [SerializeField] private TweenSettings<Vector3> newHighScoreScaleTweenSettings;
        [SerializeField] private TweenSettings<Vector3> newFitMeScaleTweenSettings;
        
        private static PlayerDataManager PlayerData => PlayerDataManager.Instance;
        private static GameManager GameManager => GameManager.Instance;
        
        private Tween _newHighScoreScaleTween;
        private Tween _newFitMeScaleTween;
        private CancellationTokenSource _showDataCts;
        private uint _beforeSaveHighScore;
        private uint _beforeSaveMostFitMe;
    
        public override void Initialize()
        {
            base.Initialize();
            homeButton.onClick.AddListener(OnHomeButtonClicked);
            tryAgainButton.onClick.AddListener(OnTryAgainButtonClicked);
            ResetPanel();
        }
        
        public override void Show()
        {
            base.Show();
            ResetPanel();
        }

        private void ResetPanel()
        {
            resultScoreText.text = string.Empty;
            fitScoreText.text = string.Empty;
            yourScoreBlock.SetActive(true);
            newHighScoreBlock.SetActive(false);
            newFitMeBlock.SetActive(false);
            newHighScoreBlock.transform.localScale = Vector3.zero;
            newFitMeBlock.transform.localScale = Vector3.zero;
        }
        
        public override void OnPanelReady()
        {
            base.OnPanelReady();
            _beforeSaveHighScore = PlayerData.ScoreData.highScore.score;
            _beforeSaveMostFitMe = PlayerData.FitMeData.mostFitMe.fitMe;
            SaveScoreData();
            _showDataCts = new CancellationTokenSource();
            ShowData(_showDataCts.Token).Forget();
        }

        private async UniTaskVoid ShowData(CancellationToken cancellationToken = default)
        {
            await UniTask.WaitForSeconds(1f, cancellationToken: cancellationToken);
            await ShowHighScore(cancellationToken);
            await UniTask.WaitForSeconds(1f, cancellationToken: cancellationToken);
            await ShowFitMeScore(cancellationToken);
        }
        
        private void OnHomeButtonClicked()
        {
            FinishAllTween();
            GameManager.BackToMenu();
        }

        private void OnTryAgainButtonClicked()
        {
            FinishAllTween();
            GameManager.Retry();
        }

        private void FinishAllTween()
        {
            _showDataCts?.Cancel();
            _newHighScoreScaleTween.Complete();
            _newFitMeScaleTween.Complete();
        }
    
        private async UniTask ShowHighScore(CancellationToken cancellationToken = default)
        {
            resultScoreText.text = GameManager.Score.Value.ToString("N0");
            yourScoreBlock.SetActive(true);
            newHighScoreBlock.SetActive(false);
            if (GameManager.Score.Value <= _beforeSaveHighScore) return;
            yourScoreBlock.SetActive(false);
            newHighScoreBlock.SetActive(true);
            AudioManager.Instance.PlayAudioOneShot(newHighScoreSfx, transform.position);
            _newHighScoreScaleTween = Tween.Scale(newHighScoreBlock.transform, newHighScoreScaleTweenSettings);
            await _newHighScoreScaleTween.ToUniTask(cancellationToken: cancellationToken);
        }
    
        private async UniTask ShowFitMeScore(CancellationToken cancellationToken = default)
        {
            fitScoreText.text =   
                GameManager.FitmeScore.Value.ToString("N0");
            if (GameManager.FitmeScore.Value <= _beforeSaveMostFitMe) return;
            newFitMeBlock.SetActive(true);
            AudioManager.Instance.PlayAudioOneShot(newFitMeSfx, transform.position);
            _newFitMeScaleTween = Tween.Scale(newFitMeBlock.transform, newFitMeScaleTweenSettings);
            await _newFitMeScaleTween.ToUniTask(cancellationToken: cancellationToken);
        }
    
        private void SaveScoreData()
        {
            PlayerData.SaveScore((uint)GameManager.Score.Value);
            PlayerData.SaveFitMe((uint)GameManager.FitmeScore.Value);
        }
    }
}
