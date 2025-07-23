using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using PrimeTween;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Transitions
{
    [Serializable]
    public class ScaleTransition : IUITransition
    {
        [SerializeField] private string rectTransformKey = "PanelRectTransform";
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
            _transitionSequence = Sequence.Create()
                .Group(Tween.Scale(_transitionObject, transitionSettings));
            return _transitionSequence;
        }

        public void CancelTransition()
        {
            _transitionSequence.Stop();
        }
    }
}