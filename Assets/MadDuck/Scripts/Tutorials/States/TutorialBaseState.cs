using System;
using MadDuck.Scripts.Frameworks.StateMachine;
using MadDuck.Scripts.UIs.Panels;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MadDuck.Scripts.Tutorials.States
{
    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public abstract class TutorialBaseState : State
    {
        [OdinSerialize] public IUIPanel Page { get; private set; }
        [SerializeField, TextArea] private string tutorialText;
        
        private TutorialStateMachine _stateMachine;

        public virtual void Initialize(TutorialStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }
        public virtual void Complete()
        {
            
        }
    }
}