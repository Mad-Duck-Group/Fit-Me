using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Others
{
    public class ScoreChangeView : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private TMP_Text scoreChangeText;

        [Title("Settings")]
        [SerializeField] private bool fitMe;
        [SerializeField] private Vector2 fitMeScoreOffset = new(0, 100);
        
        [Title("Tween")] 
        [SerializeField] private TweenSettings<Vector3> scoreChangeScaleTweenSettings;
        [SerializeField] private TweenSettings<float> scoreChangeAlphaTweenSettings;
        [SerializeField] private TweenSettings scoreChangeMoveTweenSettings;
        [SerializeField] private float scoreChangeStayDuration = 1f;
        
        private Sequence _scoreChangeSequence;
        public event Action OnFinalPositionReached;

        public void Show(ScoreTypes scoreType, int change, Vector2 finalPosition)
        {
            if (!fitMe)
            {
                var sign = change > 0 ? "+" : string.Empty;
                scoreChangeText.text = $"{sign}{change:N0}";
            }
            else
            {
                scoreChangeText.text = "FIT!";
            }
            var offset = scoreType is ScoreTypes.FitMe ? fitMeScoreOffset : Vector2.zero;
            var modifier = fitMe ? 1f : -1f;
            var startPosition = transform.position + (Vector3)offset * modifier;
            transform.position = startPosition;
            var tweenPositionSettings = new TweenSettings<Vector2>
            {
                startValue = startPosition,
                endValue = finalPosition,
                settings = scoreChangeMoveTweenSettings,
                startFromCurrent = true
            };
            _scoreChangeSequence = Sequence.Create()
                .Group(Tween.Alpha(scoreChangeText, scoreChangeAlphaTweenSettings))
                .Group(Tween.Scale(transform, scoreChangeScaleTweenSettings))
                .ChainDelay(scoreChangeStayDuration)
                .Chain(Tween.UIAnchoredPosition((RectTransform)transform, tweenPositionSettings).OnComplete(() =>
                {
                    OnFinalPositionReached?.Invoke();
                }))
                .Group(Tween.Alpha(scoreChangeText, scoreChangeAlphaTweenSettings.WithDirection(false)))
                .Group(Tween.Scale(transform, scoreChangeScaleTweenSettings.WithDirection(false)))
                .OnComplete(() => Destroy(gameObject));
        }

        public void Cancel()
        {
            _scoreChangeSequence.Complete();
        }
    }
}