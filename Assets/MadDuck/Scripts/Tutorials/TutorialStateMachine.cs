using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Frameworks.StateMachine;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Tutorials.States;
using MadDuck.Scripts.UIs.Panels;
using MessagePipe;
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
        AfterDragAndDrop4 = 5,
        Fit = 6,
        AfterFit1 = 7,
        AfterFit2 = 8,
        AfterFit3 = 9,
        AfterFit4 = 10,
        Failure = 999
    }
    
    [Serializable]
    public class TutorialStateMachine : StateMachine
    {
        [TitleGroup("Tutorial")]
        [SerializeField] private SerializableDictionary<TutorialState, TutorialBaseState> stateDictionary = new();
        [SerializeField] private TutorialState initialTutorialState = TutorialState.Intro;
        [TitleGroup("Tutorial")]
        [Button("Test Skip")]
        private void TestSkipTo(TutorialState tutorialState)
        {
            SkipTo(tutorialState);
        }
        
        [TitleGroup("Audios")]
        [SerializeField] private EventReference soundEffect;
        
        [TitleGroup("Debug")]
        [field: SerializeField, DisplayAsString] public TutorialState CurrentTutorialState { get; set; }
        
        public void Initialize()
        {
            stateDictionary.Values.ForEach(x => x.Initialize(this));
            GameManager.OnGameOver += OnGameOver;
        }
        
        public void Shutdown()
        {
            GameManager.OnGameOver -= OnGameOver;
            stateDictionary.Values.ForEach(x => x.Shutdown());
        }

        private void OnGameOver(bool isGameplayMode)
        {
            CurrentTutorialState = TutorialState.Failure;
            if (stateDictionary.TryGetValue(CurrentTutorialState, out var failureState))
            {
                ChangeState(failureState);
            }
            else
            {
                Debug.LogError($"Failure state not found in state dictionary.");
            }
        }

        public void StartTutorial()
        {
            CurrentTutorialState = initialTutorialState;
            if (stateDictionary.TryGetValue(CurrentTutorialState, out var initialState))
            {
                AudioManager.Instance.PlayAudioOneShot(soundEffect, Vector3.zero);
                ChangeState(initialState);
            }
            else
            {
                Debug.LogError($"Initial state {initialTutorialState} not found in state dictionary.");
            }
        }
        
        public void MoveNext()
        {
            Debug.Log("Moving to next tutorial state");
            var nextTutorialState = CurrentTutorialState + 1;
            if (stateDictionary.TryGetValue(nextTutorialState, out var nextState))
            {
                CurrentTutorialState = nextTutorialState;
                AudioManager.Instance.PlayAudioOneShot(soundEffect, Vector3.zero);
                ChangeState(nextState);
            }
            else
            {
                Debug.Log("End of tutorial");
                TutorialManager.Instance.HideTutorial().Forget();
                PlayerDataManager.Instance.SaveTutorialCompletion();
                LoadSceneManager.Instance.LoadScene(SceneType.Gameplay, LoadSceneMode.Single, false).Forget();
                Shutdown();
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
                x.Skip();
            });
            CurrentTutorialState = beforeTarget;
            MoveNext();
        }
    }
}