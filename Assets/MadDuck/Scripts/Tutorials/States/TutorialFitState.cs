using System;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using UnityEngine;

namespace MadDuck.Scripts.Tutorials.States
{
    [Serializable]
    public class TutorialFitState : TutorialBaseState
    {
        public override void Shutdown()
        {
            base.Shutdown();
            GameManager.OnFitMeAdded -= OnFitMe;
        }

        public override void Enter()
        {
            base.Enter();
            GameManager.OnFitMeAdded += OnFitMe;
            GameManager.Instance.CurrentGameState.Value = GameState.PlaceBlock;
            TutorialManager.Instance.HideTutorial().Forget();
        }
        
        public override void Exit()
        {
            base.Exit();
            GameManager.OnFitMeAdded -= OnFitMe;
            TutorialManager.Instance.ShowTutorial().Forget();
            GameManager.Instance.CurrentGameState.Value = GameState.Tutorial;
        }

        private void OnFitMe(ScoreTypes scoreTypes, int previous, int current, Vector3 position)
        {
            Complete();
            stateMachine.MoveNext();
        }
    }
}