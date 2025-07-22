using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MadDuck.Scripts.UIs.Transitions
{
    [Serializable]
    public class AlphaTransition : IUITransition
    {
        [SerializeField] private string canvasGroupKey = "PanelCanvasGroup";
        [SerializeField] private TweenSettings<float> transitionSettings;

        private Sequence _transitionSequence;
        private CanvasGroup _transitionObject;

        public void Initialize(IUIPanel panel)
        {
           if (!panel.TransitionObjectProviders.TryGetValue(canvasGroupKey, out var temp))
           {
                return;
           }
           _transitionObject = temp as CanvasGroup;
           if (_transitionObject) return;
           Debug.LogError($"Transition object with key '{canvasGroupKey}' not found in panel '{panel.PanelName}'. Ensure it is set up correctly.");
        }

        public async UniTask Transition(CancellationToken cancellationToken = default)
        {
            _transitionSequence = Sequence.Create()
                .Group(Tween.Alpha(_transitionObject, transitionSettings));
            await _transitionSequence.ToUniTask(cancellationToken: cancellationToken);
        }

        public void CancelTransition()
        {
            _transitionSequence.Stop();
        }
    }
}