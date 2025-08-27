using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Others;
using MadDuck.Scripts.UIs.Panels.Tutorial;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils;
using MessagePipe;
using Sirenix.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MadDuck.Scripts.Tutorials.States
{
    [Serializable]
    public class TutorialDragAndDropState : TutorialBaseState
    {
        [OdinSerialize] private IFloatingUIElement floatingHandIconPrefab;
        [SerializeField] private Transform handIconParent;
        private IPublisher<StartSpawnEvent> _startSpawnPublisher;
        private IPublisher<FadeTutorialBackgroundEvent> _fadeTutorialBackgroundPublisher;
        private List<Block> _spawnedBlocks = new();
        private IFloatingUIElement _floatingHandIconInstance;
        private GameObject _handIconGameObject;
        
        public override void Initialize(TutorialStateMachine stateMachine)
        {
            base.Initialize(stateMachine);
            _startSpawnPublisher = GlobalMessagePipe.GetPublisher<StartSpawnEvent>();
            _fadeTutorialBackgroundPublisher = GlobalMessagePipe.GetPublisher<FadeTutorialBackgroundEvent>();
            _floatingHandIconInstance = floatingHandIconPrefab.InstantiateAsInterface(new InstantiateParameters()
                        {
                            parent = handIconParent,
                            worldSpace = false
                        }, out _handIconGameObject);
            _floatingHandIconInstance.Initialize();
            _handIconGameObject.SetActive(false);
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
            if (_handIconGameObject)
            {
                Object.Destroy(_handIconGameObject);
            }
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
            var middleBlock = _spawnedBlocks[Mathf.FloorToInt(_spawnedBlocks.Count / 2f)];
            var iconPosition = PointerManager.Instance.WorldToWorldCanvasPosition(middleBlock.transform.position);
            _handIconGameObject.transform.position = iconPosition;
            ShowHandIcon().Forget();
        }
        
        private async UniTaskVoid ShowHandIcon()
        {
            await _floatingHandIconInstance.Show();
            await _floatingHandIconInstance.PlayAnimation();
        }

        private void OnBlockBeingDrag()
        {
            _floatingHandIconInstance.Hide().Forget();
            TutorialManager.Instance.HideTutorial().Forget();
        }

        private void OnBlockEndDrag(bool placed)
        {
            TutorialManager.Instance.ShowTutorial().Forget();
            if (!placed)
            {
                ShowHandIcon().Forget();
            }
            else
            {
                floatingHandIconPrefab.Hide().Forget();
                Complete();
                stateMachine.MoveNext();
            }
        }
    }
}