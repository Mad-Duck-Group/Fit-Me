using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.Utils;
using PrimeTween;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Transitions
{
    [Serializable]
    public class PositionTransition : IUITransition
    {
        private enum PositionType
        {
            World,
            Local,
            AnchoredUI
        }
        [SerializeField] private string rectTransformKey = "PanelRectTransform";
        [SerializeField] private PositionType positionType = PositionType.AnchoredUI;
        [SerializeField] private bool relative;
        [SerializeField] private TweenSettings<Vector3> transitionSettings;

        private Sequence _transitionSequence;
        private RectTransform _transitionObject;

        public void Initialize(IUIPanel panel)
        {
            panel.TryGetTransitionObject(rectTransformKey, out _transitionObject);
        }

        public Sequence? Transition()
        {
            if (!_transitionObject) return null;
            TweenSettings<Vector3> settings;
            switch (positionType)
            {
                case PositionType.World: 
                    settings = relative
                        ? transitionSettings.ToRelative(_transitionObject.position)
                        : transitionSettings;
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.Position(_transitionObject, settings));
                    break;
                case PositionType.Local:
                    settings = relative
                        ? transitionSettings.ToRelative(_transitionObject.localPosition)
                        : transitionSettings;
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.LocalPosition(_transitionObject, settings));
                    break;
                case PositionType.AnchoredUI:
                    settings = relative
                        ? transitionSettings.ToRelative(_transitionObject.anchoredPosition)
                        : transitionSettings;
                    _transitionSequence = Sequence.Create()
                        .Group(Tween.UIAnchoredPosition(_transitionObject, settings.ToVector2()));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return _transitionSequence;
        }

        public void CancelTransition()
        {
            _transitionSequence.Stop();
        }
    }
}