using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private TMP_Text scoreChangeText;
        [SerializeField] private TMP_Text fitMeText;
        [SerializeField] private TMP_Text fitMeChangeText;
        [SerializeField] private Button pauseButton;
        
        [Title("Tween Score")] 
        [SerializeField] private TweenSettings<Vector3> scoreScaleTweenSettings;
        [SerializeField] private TweenSettings<Vector3> scoreChangeScaleTweenSettings;
        [SerializeField] private TweenSettings<Vector3> scoreChangeRelativePositionTweenSettings;
        [SerializeField] private TweenSettings<float> scoreChangeAlphaTweenSettings;
        [SerializeField] private float scoreChangeStayDuration = 1f;
        
        [Title("Tween FitMe")] 
        [SerializeField] private TweenSettings<Vector3> fitMeScaleTweenSettings;
        [SerializeField] private TweenSettings<Vector3> fitMeChangeScaleTweenSettings;
        [SerializeField] private TweenSettings<Vector3> fitMeChangeRelativePositionTweenSettings;
        [SerializeField] private TweenSettings<float> fitMeChangeAlphaTweenSettings;
        [SerializeField] private float fitMeChangeStayDuration = 1f;
        
        [Title("Panels")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule pauseCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule gameOverCrossFadeRule = new();
        
        private IDisposable _scoreSubscription;
        private Sequence _scoreSequence;
        private Sequence _scoreChangeSequence;

        private IDisposable _fitMeSubscription;
        private Sequence _fitMeSequence;
        private Sequence _fitMeChangeSequence;
        
        private void OnEnable()
        {
            GameManager.OnGameOver += OnGameOver;
            _scoreSubscription = GameManager.Instance.Score.Pairwise()
                .Subscribe(x => OnScoreChanged(x.Previous, x.Current)).AddTo(this);
            
            _fitMeSubscription = GameManager.Instance.FitmeScore.Pairwise()
                .Subscribe(x => OnFitMeChanged(x.Previous, x.Current)).AddTo(this);
        }
        
        private void OnDisable()
        {
            GameManager.OnGameOver -= OnGameOver;
            _scoreSubscription?.Dispose();
            _fitMeSubscription?.Dispose();
        }
        
        public override void Initialize()
        {
            base.Initialize();
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
            scoreText.text = 0.ToString("N0");
            scoreChangeText.text = string.Empty;
            scoreChangeText.alpha = 0f;
            scoreChangeText.transform.localScale = Vector3.zero;
            
            fitMeText.text = 0.ToString("N0");
            fitMeChangeText.text = string.Empty;
            fitMeChangeText.alpha = 0f;
            fitMeChangeText.transform.localScale = Vector3.zero;
        }
        
        private void OnGameOver()
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, gameOverCrossFadeRule.nextPanel, gameOverCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
        }
        
        private void OnPauseButtonClicked()
        {
            GameManager.Instance.PauseGame();
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, pauseCrossFadeRule.nextPanel, pauseCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
        }
        
        private void OnScoreChanged(int previous, int current)
        {
            scoreText.text = current.ToString("N0");
            _scoreSequence.Complete();
            _scoreSequence = Sequence.Create(Tween.Scale(scoreText.transform, scoreScaleTweenSettings));
            var change = current - previous;
            if (change == 0) return;
            var sign = change > 0 ? "+" : "-";
            scoreChangeText.text = $"{sign}{change:N0}";
            _scoreChangeSequence.Complete();
            var initialPosition = scoreChangeText.rectTransform.anchoredPosition;
            var relativePositionSettings =
                scoreChangeRelativePositionTweenSettings.ToRelative(initialPosition);
            _scoreChangeSequence = Sequence.Create()
                .Group(Tween.Scale(scoreChangeText.transform, scoreChangeScaleTweenSettings))
                .Group(Tween.Alpha(scoreChangeText, scoreChangeAlphaTweenSettings))
                .Group(Tween.UIAnchoredPosition(scoreChangeText.rectTransform, relativePositionSettings.ToVector2()))
                .ChainDelay(scoreChangeStayDuration)
                .Chain(Tween.Alpha(scoreChangeText, scoreChangeAlphaTweenSettings.WithDirection(false)))
                .Group(Tween.Scale(scoreChangeText.transform, scoreChangeScaleTweenSettings.WithDirection(false)))
                .OnComplete(() =>
                {
                    scoreChangeText.rectTransform.anchoredPosition = initialPosition;
                    scoreChangeText.alpha = 0f;
                    scoreChangeText.transform.localScale = Vector3.zero;
                });
        }
        
        private void OnFitMeChanged(int previous, int current)
        {
            fitMeText.text = current.ToString("N0");
            _fitMeSequence.Complete();
            _fitMeSequence = Sequence.Create(Tween.Scale(fitMeText.transform, fitMeScaleTweenSettings));
            var change = current - previous;
            if (change == 0) return;
            var sign = change > 0 ? "+" : "-";
            fitMeChangeText.text = $"{sign}{change:N0}";
            _fitMeChangeSequence.Complete();
            var initialPosition = fitMeChangeText.rectTransform.anchoredPosition;
            var relativePositionSettings =
                fitMeChangeRelativePositionTweenSettings.ToRelative(initialPosition);
            _fitMeChangeSequence = Sequence.Create()
                .Group(Tween.Scale(fitMeChangeText.transform, fitMeChangeScaleTweenSettings))
                .Group(Tween.Alpha(fitMeChangeText, fitMeChangeAlphaTweenSettings))
                .Group(Tween.UIAnchoredPosition(fitMeChangeText.rectTransform, relativePositionSettings.ToVector2()))
                .ChainDelay(fitMeChangeStayDuration)
                .Chain(Tween.Alpha(fitMeChangeText, fitMeChangeAlphaTweenSettings.WithDirection(false)))
                .Group(Tween.Scale(fitMeChangeText.transform, fitMeChangeScaleTweenSettings.WithDirection(false)))
                .OnComplete(() =>
                {
                    fitMeChangeText.rectTransform.anchoredPosition = initialPosition;
                    fitMeChangeText.alpha = 0f;
                    fitMeChangeText.transform.localScale = Vector3.zero;
                });
        }
    }
}