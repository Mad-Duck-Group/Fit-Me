using System;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Tutorials;
using MadDuck.Scripts.Tutorials.States;
using MadDuck.Scripts.UIs.Transitions;
using MadDuck.Scripts.Utils.Inspectors;
using MessagePipe;
using PrimeTween;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.Tutorial
{
    public struct FadeTutorialBackgroundEvent
    {
        public readonly bool fadeIn;
        
        public FadeTutorialBackgroundEvent(bool fadeIn)
        {
            this.fadeIn = fadeIn;
        }
    }
    public class TutorialPanel : UIPanel
    {
        [Title("References")]
        [SerializeField] private TMPWriter textWriter;
        [SerializeField] private TMPWriter headerWriter;
        [SerializeField] private RectTransform textBox;
        [SerializeField] private RectTransform loopieWhoopie;
        [SerializeField] private Button nextButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image tutorialImage;

        [Title("Tween")] 
        [SerializeField] private RectTransformInset textBoxSizeNoImage;
        [SerializeField] private RectTransformInset textBoxSizeWithImage;
        [SerializeField] private TweenSettings textBoxSizeTweenSettings;
        [SerializeField] private Vector2 loopieWhoopiePositionNoImage;
        [SerializeField] private Vector2 loopieWhoopiePositionWithImage;
        [SerializeField] private TweenSettings loopieWhoopiePositionTweenSettings;
        [SerializeField] private TweenSettings<Vector3> nextButtonScaleTweenSettings;
        [SerializeField] private TweenSettings<float> backgroundFadeTweenSettings;
        [SerializeField] private TweenSettings<float> tutorialImageFadeTweenSettings;
        [SerializeField] private TweenSettings<float> tutorialTextFadeTweenSettings;
        
        private IDisposable _tutorialDisplaySubscription;
        private IDisposable _fadeBackgroundSubscription;
        private Sequence _tutorialImageFadeSequence;
        private Sequence _fadeTextSequence;
        private Sequence _changeTutorialSequence;
        private Sequence _nextButtonSequence;
        private Sequence _fadeBackgroundSequence;
        public static event Action OnNext;

        public override void Initialize()
        {
            base.Initialize();
            _tutorialDisplaySubscription = GlobalMessagePipe.GetSubscriber<TutorialDisplayEvent>()
                .Subscribe(x => OnTutorialDisplay(x).Forget());
            _fadeBackgroundSubscription = GlobalMessagePipe.GetSubscriber<FadeTutorialBackgroundEvent>()
                .Subscribe(OnFadeBackground);
            nextButton.onClick.AddListener(OnNextButtonClicked);
            textWriter.TextComponent.text = string.Empty;
            headerWriter.TextComponent.text = string.Empty;
            headerWriter.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
            tutorialImage.gameObject.SetActive(false);
            loopieWhoopie.anchoredPosition = loopieWhoopiePositionNoImage;
            textBox.offsetMin = textBoxSizeNoImage.OffsetMin;
            textBox.offsetMax = textBoxSizeNoImage.OffsetMax;
            backgroundImage.color = backgroundImage.color.WithA(backgroundFadeTweenSettings.startValue);
        }

        private void OnDestroy()
        {
            _tutorialDisplaySubscription?.Dispose();
            _fadeBackgroundSubscription?.Dispose();
        }
        
        public override void Show()
        {
            base.Show();
            textWriter.StartWriter();
            textWriter.SkipWriter();
            headerWriter.StartWriter();
            headerWriter.SkipWriter();
        }

        private async UniTaskVoid OnTutorialDisplay(TutorialDisplayEvent eventData)
        {
            headerWriter.TextComponent.text =
                eventData.tutorialData.hasHeader ? eventData.tutorialData.headerText : string.Empty;
            textWriter.TextComponent.text = eventData.tutorialData.tutorialText;
            var loopieWhoopieFinalPosition = eventData.tutorialData.hasImage
                ? loopieWhoopiePositionWithImage
                : loopieWhoopiePositionNoImage;
            var positionTweenSettings = new TweenSettings<Vector2>
            {
                startValue = loopieWhoopie.anchoredPosition,
                endValue = loopieWhoopieFinalPosition,
                settings = loopieWhoopiePositionTweenSettings,
            };
            var textBoxLeftFinalValue = eventData.tutorialData.hasImage
                ? textBoxSizeWithImage
                : textBoxSizeNoImage;
            var textBoxOffsetMinSettings = new TweenSettings<Vector2>
            {
                startValue = textBox.offsetMin,
                endValue = textBoxLeftFinalValue.OffsetMin,
                settings = textBoxSizeTweenSettings,
            };
            var textBoxOffsetMaxSettings = new TweenSettings<Vector2>
            {
                startValue = textBox.offsetMax,
                endValue = textBoxLeftFinalValue.OffsetMax,
                settings = textBoxSizeTweenSettings,
            };
            _changeTutorialSequence = Sequence.Create()
                .Group(Tween.UIOffsetMin(textBox, textBoxOffsetMinSettings))
                .Group(Tween.UIOffsetMax(textBox, textBoxOffsetMaxSettings))
                .Group(Tween.UIAnchoredPosition(loopieWhoopie, positionTweenSettings));
            await _changeTutorialSequence.ToUniTask();
            
            tutorialImage.gameObject.SetActive(eventData.tutorialData.hasImage);
            if (eventData.tutorialData.hasImage)
            {
                tutorialImage.sprite = eventData.tutorialData.tutorialImage;
            }
            tutorialImage.enabled = false;
            tutorialImage.color = tutorialImage.color.WithA(tutorialImageFadeTweenSettings.startValue);
            
            _fadeTextSequence = Sequence.Create()
                .Group(Tween.Alpha(textWriter.TextComponent, tutorialTextFadeTweenSettings))
                .Group(Tween.Alpha(headerWriter.TextComponent, tutorialTextFadeTweenSettings));
            await _fadeTextSequence.ToUniTask();

            headerWriter.gameObject.SetActive(eventData.tutorialData.hasHeader);
            if (eventData.tutorialData.hasHeader)
            {
                headerWriter.StartWriter();
                headerWriter.OnFinishWriter.AddListener(_ =>
                {
                    headerWriter.OnFinishWriter.RemoveAllListeners();
                    tutorialImage.enabled = true;
                    _tutorialImageFadeSequence.Stop();
                    _tutorialImageFadeSequence = Sequence.Create()
                        .Group(Tween.Alpha(tutorialImage, tutorialImageFadeTweenSettings));
                    textWriter.StartWriter();
                    textWriter.OnFinishWriter.AddListener(_ =>
                    {
                        ShowNextButton();
                    });
                });
            }
            else
            {
                tutorialImage.enabled = true;
                _tutorialImageFadeSequence.Stop();
                _tutorialImageFadeSequence = Sequence.Create()
                    .Group(Tween.Alpha(tutorialImage, tutorialImageFadeTweenSettings));
                textWriter.StartWriter();
                textWriter.OnFinishWriter.AddListener(_ =>
                {
                    ShowNextButton();
                });
            }
            return;

            void ShowNextButton()
            {
                textWriter.OnFinishWriter.RemoveAllListeners();
                if (!eventData.tutorialData.hasNextButton)
                {
                    nextButton.gameObject.SetActive(false);
                    return;
                }
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = false;
                _nextButtonSequence = Sequence.Create()
                    .Group(Tween.Scale(nextButton.transform, nextButtonScaleTweenSettings))
                    .OnComplete(() =>
                    {
                        nextButton.interactable = true;
                    });
            }
        }
        
        private void OnNextButtonClicked()
        {
            Reset().Forget();
        }

        private async UniTaskVoid Reset()
        {
            _fadeBackgroundSequence.Complete();
            _changeTutorialSequence.Complete();
            _nextButtonSequence.Complete();
            nextButton.interactable = false;
            _nextButtonSequence = Sequence.Create()
                .Group(Tween.Scale(nextButton.transform, nextButtonScaleTweenSettings.WithDirection(false)))
                .OnComplete(() =>
                {
                    nextButton.gameObject.SetActive(false);
                });
            _tutorialImageFadeSequence.Complete();
            _tutorialImageFadeSequence = Sequence.Create()
                .Group(Tween.Alpha(tutorialImage, tutorialImageFadeTweenSettings.WithDirection(false)))
                .OnComplete(() =>
                {
                    tutorialImage.gameObject.SetActive(false);
                });
            _fadeTextSequence.Complete();
            _fadeTextSequence = Sequence.Create()
                .Group(Tween.Alpha(textWriter.TextComponent, tutorialTextFadeTweenSettings.WithDirection(false)))
                .Group(Tween.Alpha(headerWriter.TextComponent, tutorialTextFadeTweenSettings.WithDirection(false)));
            var allSequence = Sequence.Create()
                .Group(_nextButtonSequence)
                .Group(_tutorialImageFadeSequence)
                .Group(_fadeTextSequence);
            headerWriter.StopWriter();
            headerWriter.OnFinishWriter.RemoveAllListeners();
            textWriter.StopWriter();
            textWriter.OnFinishWriter.RemoveAllListeners();
            await allSequence.ToUniTask();
            headerWriter.TextComponent.text = string.Empty;
            textWriter.TextComponent.text = string.Empty;
            OnNext?.Invoke();
        }

        private void OnFadeBackground(FadeTutorialBackgroundEvent fadeTutorialBackgroundEvent)
        {
            _fadeBackgroundSequence.Stop();
            _fadeBackgroundSequence = Sequence.Create()
                .Group(Tween.Alpha(backgroundImage, backgroundFadeTweenSettings.WithDirection(!fadeTutorialBackgroundEvent.fadeIn)));
        }
    }
}