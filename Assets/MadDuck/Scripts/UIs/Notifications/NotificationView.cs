using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Utils;
using PrimeTween;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Notifications
{
    public class NotificationView : MonoBehaviour
    {
        [Title("Inspectors")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text messageText;
        
        [Title("Settings")]
        [SerializeField] private Sprite defaultIcon;

        [Title("Tween")] 
        [SerializeField] private TweenSettings<Vector2> showingRelativePositionTweenSettings;
        [SerializeField] private ShakeSettings scaleTweenSettings;

        private TweenSettings<Vector2> _relativePositionSettings;
        private Sequence _visibilitySequence;
        private Sequence _animationSequence;
        
        public void Initialize()
        {
            messageText.text = string.Empty;
            icon.sprite = null;
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

        public void SetMessage(string message)
        {
            messageText.text = message;
        }

        public void SetIcon(Sprite sprite)
        {
            if (!sprite)
            {
                sprite = defaultIcon;
            }
            icon.sprite = sprite;
        }
    }
}