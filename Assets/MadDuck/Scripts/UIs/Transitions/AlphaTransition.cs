using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
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
            switch (_transitionObject)
            {
                case CanvasGroup canvasGroup:
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Alpha(canvasGroup, transitionSettings));
                    break;
                case Graphic image:
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Alpha(image, transitionSettings));
                    break;
                case Shadow shadow:
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Alpha(shadow, transitionSettings));
                    break;
                case SpriteRenderer spriteRenderer:
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Alpha(spriteRenderer, transitionSettings));
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