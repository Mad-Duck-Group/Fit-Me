using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Panels.Tutorial;
using MadDuck.Scripts.Units;
using MessagePipe;

namespace MadDuck.Scripts.Tutorials.States
{
    [Serializable]
    public class TutorialDragAndDropState : TutorialBaseState
    {
        private IPublisher<StartSpawnEvent> _startSpawnPublisher;
        private IPublisher<FadeTutorialBackgroundEvent> _fadeTutorialBackgroundPublisher;
        private List<Block> _spawnedBlocks = new();
        
        public override void Initialize(TutorialStateMachine stateMachine)
        {
            base.Initialize(stateMachine);
            _startSpawnPublisher = GlobalMessagePipe.GetPublisher<StartSpawnEvent>();
            _fadeTutorialBackgroundPublisher = GlobalMessagePipe.GetPublisher<FadeTutorialBackgroundEvent>();
        }
        
        public override void Shutdown()
        {
            base.Shutdown();
            _startSpawnPublisher = null;
            _fadeTutorialBackgroundPublisher = null;
            BlockManager.OnBlockSpawned -= OnBlockSpawned;
            _spawnedBlocks.ForEach(b =>
            {
                b.OnBlockBeingDrag -= OnBlockBeingDrag;
                b.OnBlockEndDrag -= OnBlockEndDrag;
            });
            _spawnedBlocks.Clear();
        }
        
        public override void Enter()
        {
            base.Enter();
            BlockManager.OnBlockSpawned += OnBlockSpawned;
            _startSpawnPublisher.Publish(new StartSpawnEvent());
            GameManager.Instance.CurrentGameState.Value = GameState.PlaceBlock;
            _fadeTutorialBackgroundPublisher.Publish(new FadeTutorialBackgroundEvent(false));
        }
        
        public override void Exit()
        {
            base.Exit();
            _fadeTutorialBackgroundPublisher.Publish(new FadeTutorialBackgroundEvent(true));
            GameManager.Instance.CurrentGameState.Value = GameState.Tutorial;
            BlockManager.OnBlockSpawned -= OnBlockSpawned;
            _spawnedBlocks.ForEach(b =>
            {
                b.OnBlockBeingDrag -= OnBlockBeingDrag;
                b.OnBlockEndDrag -= OnBlockEndDrag;
            });
            _spawnedBlocks.Clear();
        }
        
        private void OnBlockSpawned(List<Block> blocks)
        {
            BlockManager.OnBlockSpawned -= OnBlockSpawned;
            _spawnedBlocks = blocks;
            _spawnedBlocks.ForEach(b =>
            {
                b.OnBlockBeingDrag += OnBlockBeingDrag;
                b.OnBlockEndDrag += OnBlockEndDrag;
            });
        }

        private void OnBlockBeingDrag()
        {
            TutorialManager.Instance.HideTutorial().Forget();
        }

        private void OnBlockEndDrag(bool placed)
        {
            TutorialManager.Instance.ShowTutorial().Forget();
            if (!placed) return;
            Complete();
            stateMachine.MoveNext();
        }
    }
}