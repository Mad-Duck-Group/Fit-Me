using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.UIs.Panels;
using PrimeTween;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif
using Sirenix.Serialization;
using Sirenix.Utilities.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MadDuck.Scripts.UIs.Transitions
{
    public interface IUITransition
    {
        void Initialize(IUIPanel panel);
        Sequence? Transition();
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
        public float customOffset;
        [HideInInspector, HideReferenceObjectPicker] public TransitionGroupCollection transitionGroupCollection = new();
        [Button("Open Transition Editor")]
        public void OpenTransitionEditor()
        {
            #if UNITY_EDITOR
            transitionGroupCollection ??= new TransitionGroupCollection();
            var window = OdinEditorWindow.InspectObject(transitionGroupCollection);
            window.titleContent = new GUIContent("Transition Group Editor", EditorIcons.ImageCollection.Active);
            window.minSize = new Vector2(1000, 500);
            #endif
        }
    }

    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public record CrossFadeRule
    {
        public IUIPanel nextPanel;
        public CrossFadeSettings crossFadeSettings = new();
    }
    
    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public record PageCrossFadeRule
    {
        public IUIPanel thisPanel;
        public CrossFadeSettings crossFadeSettings;
    }

    public enum TransitionGroupType
    {
        Group,
        Chain
    }
    
    [ShowOdinSerializedPropertiesInInspector]
    public record TransitionGroupCollection
    {
        [NonSerialized, OdinSerialize, HideReferenceObjectPicker]
        public TransitionGroup nextIn = new();
        
        [NonSerialized, OdinSerialize, HideReferenceObjectPicker]
        public TransitionGroup previousOut = new();
        
        [NonSerialized, OdinSerialize, HideReferenceObjectPicker]
        public TransitionGroup transitionIn = new();
        
        [NonSerialized, OdinSerialize, HideReferenceObjectPicker]
        public TransitionGroup transitionOut = new();

        public void CancelAll()
        {
            nextIn?.CancelTransition();
            previousOut?.CancelTransition();
            transitionIn?.CancelTransition();
            transitionOut?.CancelTransition();
        }
    }
    
    [ShowOdinSerializedPropertiesInInspector]
    public record TransitionGroup
    {
        [NonSerialized, OdinSerialize, HideReferenceObjectPicker]
        [TableList(DrawScrollView = false)]
        public List<TransitionData> transition = new();
        
        private Sequence _transitionSequence;
        
        public void Initialize(IUIPanel panel)
        {
            if (transition == null || transition.Count == 0) return;

            foreach (var data in transition)
            {
                data.transition?.Initialize(panel);
            }
        }

        public async UniTask Transition(CancellationToken cancellationToken = default)
        {
            if (transition == null || transition.Count == 0)
            {
                return;
            }

            _transitionSequence = Sequence.Create();

            foreach (var data in transition)
            {
                var transitionSequence = data.transition?.Transition();
                if (transitionSequence == null) continue;
                switch (data.transitionGroupType)
                {
                    case TransitionGroupType.Group:
                        _transitionSequence.Group(transitionSequence.Value);
                        break;
                    case TransitionGroupType.Chain:
                        _transitionSequence.Chain(transitionSequence.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (data.delay > 0) _transitionSequence.ChainDelay(data.delay);
            }
            await _transitionSequence.ToUniTask(cancellationToken: cancellationToken);
        }

        public void CancelTransition()
        {
            _transitionSequence.Stop();
        }
    }
    
    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public record TransitionData
    {
        public IUITransition transition;
        public TransitionGroupType transitionGroupType = TransitionGroupType.Group;
        public float delay = 0f;
    }

    #if UNITY_EDITOR
    [ShowOdinSerializedPropertiesInInspector]
    public class TransitionGroupEditor : OdinEditorWindow
    {
        public static void OpenWindow()
        {
            GetWindow<TransitionGroupEditor>().Show();
        }
        
    }
    #endif
}