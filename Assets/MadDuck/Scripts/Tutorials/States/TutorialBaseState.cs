using System;
using MadDuck.Scripts.Frameworks.StateMachine;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Panels.Tutorial;
using MessagePipe;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MadDuck.Scripts.Tutorials.States
{
    public struct TutorialDisplayEvent
    {
        public TutorialData tutorialData;
        
        public TutorialDisplayEvent(TutorialData data)
        {
            tutorialData = data;
        }
    }

    [Serializable]
    public struct TutorialData
    {
        [SerializeField] public bool hasNextButton;
        [SerializeField] public bool hasImage;
        [SerializeField, ShowIf(nameof(hasImage))] public Sprite tutorialImage;
        [SerializeField] public bool hasHeader;
        [SerializeField, ShowIf(nameof(hasHeader))] public string headerText;
        [SerializeField, TextArea] public string tutorialText;
    }
    
    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public abstract class TutorialBaseState : State
    {
        [SerializeField] protected TutorialData tutorialData;
        
        protected TutorialStateMachine stateMachine;
        protected IPublisher<TutorialDisplayEvent> tutorialDisplayPublisher;

        public virtual void Initialize(TutorialStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
            tutorialDisplayPublisher = GlobalMessagePipe.GetPublisher<TutorialDisplayEvent>();
        }

        public override void Enter()
        {
            base.Enter();
            TutorialPanel.OnNext += OnNext;
            tutorialDisplayPublisher.Publish(new TutorialDisplayEvent(tutorialData));
        }
        
        public override void Exit()
        {
            base.Exit();
            TutorialPanel.OnNext -= OnNext;
        }
        
        protected virtual void OnNext()
        {
            if (!tutorialData.hasNextButton) return;
            Complete();
            stateMachine.MoveNext();
        }

        public virtual void Skip()
        {
            Complete();
        }
        
        public virtual void Complete()
        {
            
        }
    }
}