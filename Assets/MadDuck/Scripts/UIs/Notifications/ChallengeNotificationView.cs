using System;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Challenges;
using MadDuck.Scripts.Utils;
using PrimeTween;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Notifications
{
    [Serializable]
    public struct ChallengeNotificationData : INotificationData
    {
        [OdinSerialize] public IChallenge challenge;
        
        public ChallengeNotificationData(IChallenge challenge)
        {
            this.challenge = challenge;
        }
    }
    public class ChallengeNotificationView : MonoBehaviour, INotificationView
    {
        [Title("Inspectors")]
        [SerializeField] private TMP_Text challengeNameText;
        [SerializeField] private TMP_Text challengeDescriptionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image challengeIcon;
        
        [Title("Settings")]
        [SerializeField] private Sprite defaultIcon;

        [Title("Tween")] 
        [SerializeField] private TweenSettings<Vector2> showingRelativePositionTweenSettings;
        [SerializeField] private ShakeSettings scaleTweenSettings;

        private TweenSettings<Vector2> _relativePositionSettings;
        private Sequence _visibilitySequence;
        private Sequence _animationSequence;
        
        public void SetData<T>(T data) where T : INotificationData
        {
            if (data is ChallengeNotificationData generalData)
            {
                var challenge = generalData.challenge;
                challengeNameText.text = challenge.ChallengeName;
                challengeDescriptionText.text = challenge.ChallengeDescription;
                var progress = challenge.GetProgress();
                progress.x = Mathf.Clamp(progress.x, progress.x, progress.y);
                var isInt = progress.x % 1 == 0 && progress.y % 1 == 0;
                var format = isInt ? "N0" : "N2";
                progressText.text = $"{progress.x.ToString(format)} / {progress.y.ToString(format)}";
                progressSlider.maxValue = progress.y;
                progressSlider.value = progress.x;
                challengeIcon.sprite = challenge.ChallengeIcon ? challenge.ChallengeIcon : defaultIcon;
            }
            else
            {
                Debug.LogWarning($"[ChallengeNotificationView] Invalid data type: {typeof(T)}");
            }
        }

        public INotificationView Instantiate(Transform parent, Vector2 position)
        {
            var instance = Instantiate(this, parent);
            ((RectTransform)instance.transform).anchoredPosition = position;
            return instance;
        }

        public void Initialize()
        {
            challengeNameText.text = string.Empty;
            challengeDescriptionText.text = string.Empty;
            progressText.text = string.Empty;
            progressSlider.maxValue = 1;
            progressSlider.value = 0;
            challengeIcon.sprite = defaultIcon;
            _relativePositionSettings = showingRelativePositionTweenSettings.ToRelative(((RectTransform)transform).anchoredPosition);
        }
        
        public async UniTask Show()
        {
            _visibilitySequence = Sequence.Create()
                .Group(Tween.UIAnchoredPosition((RectTransform)transform, _relativePositionSettings));
            await _visibilitySequence.ToUniTask();
        }

        public async UniTask PlayAnimation()
        {
            _animationSequence = Sequence.Create()
                .Group(Tween.PunchScale(transform, scaleTweenSettings));
            await _animationSequence.ToUniTask();
        }

        public async UniTask Hide()
        {
            _visibilitySequence = Sequence.Create()
                .Group(Tween.UIAnchoredPosition((RectTransform)transform, _relativePositionSettings.WithDirection(false)));
            await _visibilitySequence.ToUniTask();
        }

        public void Cancel()
        {
            _visibilitySequence.Complete();
            _animationSequence.Complete();
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}