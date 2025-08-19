using System;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using UnityEngine;

namespace MadDuck.Scripts.Tutorials.States
{
    [Serializable]
    public class TutorialFailureState : TutorialBaseState
    {
        public override void Enter()
        {
            tutorialData.hasNextButton = true; //force next button to be true
            base.Enter();
            GameManager.Instance.CurrentGameState.Value = GameState.Tutorial;
            TutorialManager.Instance.ShowTutorial().Forget();
        }

        protected override void OnNext()
        {
            stateMachine.CurrentTutorialState = TutorialState.Fit - 1;
            base.OnNext();
            GameManager.Instance.Continue();
        }
    }
}