using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Others;
using MadDuck.Scripts.UIs.Transitions;
using MadDuck.Scripts.Utils;
using PrimeTween;
using R3;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.Gameplay
{
    [ShowOdinSerializedPropertiesInInspector]
    public class GameplayUIPanel : UIPanel
    {
        [Title("References")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text fitMeText;
        [SerializeField] private Button pauseButton;
        [SerializeField, AssetsOnly] private ScoreChangeView scoreChangeViewPrefab;
        [SerializeField, AssetsOnly] private ScoreChangeView fitMeChangeViewPrefab;
        
        [Title("Tween Score")] 
        [SerializeField] private TweenSettings<Vector3> scoreScaleTweenSettings;
        
        [Title("Tween FitMe")] 
        [SerializeField] private TweenSettings<Vector3> fitMeScaleTweenSettings;
        
        [Title("Panels")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule pauseCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule gameOverCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule resultCrossFadeRule = new();
        
        private Sequence _scoreSequence;
        private Sequence _fitMeSequence;
        
        private void OnEnable()
        {
            GameManager.OnGameOver += OnGameOver;
            GameManager.OnScoreAdded += OnScoreChanged;
            GameManager.OnFitMeAdded += OnFitMeChanged;
        }
        
        private void OnDisable()
        {
            GameManager.OnGameOver -= OnGameOver;
            GameManager.OnScoreAdded -= OnScoreChanged;
            GameManager.OnFitMeAdded -= OnFitMeChanged;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
            scoreText.text = 0.ToString("N0");
            fitMeText.text = 0.ToString("N0");
        }
        
        private void OnGameOver(bool showGameOverPanel)
        {
            if (!showGameOverPanel) return;

            if (GameOverPanel.CurrentContinueCount > 0)
            {
                transitionCts = new CancellationTokenSource();
                PanelController.ChangePanel(this, gameOverCrossFadeRule.nextPanel, gameOverCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
            }
            else
            {
                GameManager.Instance.ToResultScreen().Forget();
                transitionCts = new CancellationTokenSource();
                PanelController.ChangePanel(this, resultCrossFadeRule.nextPanel, resultCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
            }
        }
        
        private void OnPauseButtonClicked()
        {
            GameManager.Instance.PauseGame();
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, pauseCrossFadeRule.nextPanel, pauseCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
        }
        
        private void OnScoreChanged(ScoreTypes scoreType, int previous, int current, Vector3 worldPosition)
        {
            var canvasPosition = PointerManager.Instance.WorldToWorldCanvasPosition(worldPosition);
            var scoreChangeView = Instantiate(scoreChangeViewPrefab, canvasPosition, Quaternion.identity, transform);
            var finalPosition = transform.InverseTransformPoint(scoreText.transform.position);
            scoreChangeView.Show(scoreType, current - previous, finalPosition);
            scoreChangeView.OnFinalPositionReached += OnFinalPositionReached;
            return;

            void OnFinalPositionReached()
            {
                scoreChangeView.OnFinalPositionReached -= OnFinalPositionReached;
                scoreText.text = current.ToString("N0");
                _scoreSequence.Complete();
                _scoreSequence = Sequence.Create(Tween.Scale(scoreText.transform, scoreScaleTweenSettings));
            }
        }
        
        private void OnFitMeChanged(ScoreTypes scoreType, int previous, int current, Vector3 worldPosition)
        {
            var canvasPosition = PointerManager.Instance.WorldToWorldCanvasPosition(worldPosition);
            var fitMeChangeView = Instantiate(fitMeChangeViewPrefab, canvasPosition, Quaternion.identity, transform);
            var finalPosition = transform.InverseTransformPoint(fitMeText.transform.position);
            fitMeChangeView.Show(scoreType,current - previous, finalPosition);
            fitMeChangeView.OnFinalPositionReached += OnFinalPositionReached;
            return;
            
            void OnFinalPositionReached()
            {
                fitMeChangeView.OnFinalPositionReached -= OnFinalPositionReached;
                fitMeText.text = current.ToString("N0");
                _fitMeSequence.Complete();
                _fitMeSequence = Sequence.Create(Tween.Scale(fitMeText.transform, fitMeScaleTweenSettings));
            }
        }
    }
}