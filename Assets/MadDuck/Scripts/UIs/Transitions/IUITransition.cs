using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MadDuck.Scripts.UIs.Transitions
{
    public interface IUITransition
    {
        void Initialize(IUIPanel panel);
        UniTask Transition(CancellationToken cancellationToken = default);
        void CancelTransition();
    }
    
    public enum CrossFadeType
    {
        Parallel,
        InThenOut,
        OutThenIn,
        OnlyIn,
        OnlyOut,
        None
    }

    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public record CrossFadeSettings
    {
        public CrossFadeType crossFadeType;
        public bool useTransitionScreen;
        public float customOffset;
        [field: TabGroup("Transitions", "Next In")]
        [field: OdinSerialize, DisableIf(nameof(crossFadeType), CrossFadeType.OnlyOut)] 
        [field: HideLabel]
        public IUITransition nextIn;
        [field: TabGroup("Transitions", "Previous Out")]
        [field: OdinSerialize, DisableIf(nameof(crossFadeType), CrossFadeType.OnlyIn)] 
        [field: HideLabel]
        public IUITransition previousOut;
        [field: TabGroup("Transitions", "Transition In")]
        [field: OdinSerialize, EnableIf(nameof(useTransitionScreen))] 
        [field: HideLabel]
        public IUITransition transitionIn;
        [field: TabGroup("Transitions", "Transition Out")]
        [field: OdinSerialize, EnableIf(nameof(useTransitionScreen))] 
        [field: HideLabel]
        public IUITransition transitionOut;
    }

    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public record CrossFadeRule
    {
        public IUIPanel nextPanel;
        public CrossFadeSettings crossFadeSettings = new();
    }
}