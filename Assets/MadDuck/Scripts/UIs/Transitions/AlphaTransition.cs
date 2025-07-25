using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.Utils;
using PrimeTween;
using Redcode.Extensions;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MadDuck.Scripts.UIs.Transitions
{
    [Serializable]
    public class AlphaTransition : IUITransition
    {
        [SerializeField] private string objectKey = "PanelCanvasGroup";
        [SerializeField] private bool relative;
        [SerializeField] private TweenSettings<float> transitionSettings;

        private Sequence _transitionSequence;
        private Component _transitionObject;

        public void Initialize(IUIPanel panel)
        {
            panel.TryGetTransitionObject(objectKey, out _transitionObject);
        }

        public Sequence? Transition()
        {
            if (!_transitionObject) return null;
            TweenSettings<float> settings;
            switch (_transitionObject)
            {
                case CanvasGroup canvasGroup:
                    settings = relative
                        ? transitionSettings.ToRelative(canvasGroup.alpha)
                        : transitionSettings;
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Alpha(canvasGroup, settings));
                    break;
                case Graphic image:
                    settings = relative
                        ? transitionSettings.ToRelative(image.color.a)
                        : transitionSettings;
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Alpha(image, settings));
                    break;
                case Shadow shadow:
                    settings = relative
                        ? transitionSettings.ToRelative(shadow.effectColor.a)
                        : transitionSettings;
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Alpha(shadow, settings));
                    break;
                case SpriteRenderer spriteRenderer:
                    settings = relative
                        ? transitionSettings.ToRelative(spriteRenderer.color.a)
                        : transitionSettings;
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Alpha(spriteRenderer, settings));
                    break;
                default:
                    Debug.LogWarning("AlphaTransition: Unsupported component type for alpha transition.");
                    break;
            }
            return _transitionSequence;
        }

        public void CancelTransition()
        {
            _transitionSequence.Stop();
        }
    }
}