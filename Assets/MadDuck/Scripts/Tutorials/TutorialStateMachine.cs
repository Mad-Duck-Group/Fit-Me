using System;
using System.Linq;
using MadDuck.Scripts.Frameworks.StateMachine;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Tutorials.States;
using MadDuck.Scripts.UIs.Panels;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Tutorials
{
    public enum TutorialState
    {
        Intro = 0,
        DragAndDrop = 1,
        AfterDragAndDrop1 = 2,
        AfterDragAndDrop2 = 3,
        AfterDragAndDrop3 = 4,
        Fit = 5,
        AfterFit1 = 6,
        AfterFit2 = 7,
        AfterFit3 = 8,
        AfterFit4 = 9,
    }
    
    [Serializable]
    public class TutorialStateMachine : StateMachine
    {
        [TitleGroup("Tutorial")]
        [SerializeField] private SerializableDictionary<TutorialState, TutorialBaseState> stateDictionary = new();
        [SerializeField] private TutorialState initialTutorialState = TutorialState.Intro;
        [TitleGroup("Tutorial")]
        [field: SerializeField] public TutorialState CurrentTutorialState { get; private set; }
        [TitleGroup("Tutorial")]
        [Button("Test Skip")]
        private void TestSkipTo(TutorialState tutorialState)
        {
            SkipTo(tutorialState);
        }
        
        [Title("Panels")] 
        [field: SerializeReference, HideReferenceObjectPicker] 
        public UIPanelController PageController { get; private set; } = new();

        public void Initialize()
        {
            stateDictionary.Values.ForEach(x => x.Initialize(this));
        }

        public void StartTutorial()
        {
            CurrentTutorialState = initialTutorialState;
            if (stateDictionary.TryGetValue(CurrentTutorialState, out var initialState))
            {
                ChangeState(initialState);
            }
            else
            {
                Debug.LogError($"Initial state {initialTutorialState} not found in state dictionary.");
            }
        }
        
        public void MoveNext()
        {
            var nextTutorialState = CurrentTutorialState + 1;
            if (stateDictionary.TryGetValue(nextTutorialState, out var nextState))
            {
                CurrentTutorialState = nextTutorialState;
                ChangeState(nextState);
            }
            else
            {
                Debug.Log("End of tutorial");
                LoadSceneManager.Instance.LoadScene(SceneType.Gameplay, LoadSceneMode.Single, false).Forget();
            }
        }

        public void SkipTo(TutorialState target)
        {
            if (target < CurrentTutorialState)
            {
                Debug.LogWarning($"Cannot skip to a previous state: {target}. Current state is {CurrentTutorialState}.");
                return;
            }
            var beforeTarget = target - 1;
            if (beforeTarget < 0) return;
            var skipStates = stateDictionary.Skip((int)CurrentTutorialState)
                .Take((int)target - (int)CurrentTutorialState)
                .Select(pair => pair.Value)
                .ToList();
            if (skipStates.Count == 0) return;
            skipStates.ForEach(x =>
            {
                ChangeState(x);
                x.Complete();
            });
            CurrentTutorialState = beforeTarget;
            MoveNext();
        }
    }
}