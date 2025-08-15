using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils.Inspectors;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace MadDuck.Scripts.UIs.Others
{
    public class DragMeToPlay : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private SpriteRenderer speechBubble;
        [SerializeField] private SpriteRenderer glow;
        
        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> speechBubbleAlphaTweenSettings;
        
        [Title("Debug")] 
        [ShowInInspector, ReadOnly] private Block _block;
        
        private Sequence _speechBubbleSequence;
        private Sequence _positionSequence;
        private bool _fit;

        private void OnEnable()
        {
            BlockManager.OnBlockSpawned += OnBlockSpawned;
            GridManager.OnFitCheck += OnFitCheck;
        }

        private void OnDisable()
        {
            BlockManager.OnBlockSpawned -= OnBlockSpawned;
            GridManager.OnFitCheck -= OnFitCheck;
        }

        private void OnDestroy()
        {
            if (!_block) return;
            _block.OnBlockBeingDrag -= FadeOutBubble;
            _block.OnBlockEndDrag -= FadeInBubble;
        }

        private void OnBlockSpawned(List<Block> blocks)
        {
            _block = blocks[0];
            _block.OnBlockBeingDrag += FadeOutBubble;
            _block.OnBlockEndDrag += FadeInBubble;
        }
        
        private void OnFitCheck(FitType fitType)
        {
            if (fitType is not FitType.FitMe) return;
            _fit = true;
        }

        private void FadeOutBubble()
        {
            _speechBubbleSequence.Stop();
            _speechBubbleSequence = Sequence.Create()
                .Group(Tween.Alpha(speechBubble, speechBubbleAlphaTweenSettings))
                .Group(Tween.Alpha(glow, speechBubbleAlphaTweenSettings))
                .OnComplete(() =>
                {
                    speechBubble.gameObject.SetActive(false);
                    glow.gameObject.SetActive(false);
                });
        }
        
        private void FadeInBubble()
        {
            if (_fit) return;
            _speechBubbleSequence.Stop();
            speechBubble.gameObject.SetActive(true);
            glow.gameObject.SetActive(true);
            _speechBubbleSequence = Sequence.Create()
                .Group(Tween.Alpha(speechBubble, speechBubbleAlphaTweenSettings.WithDirection(false)))
                .Group(Tween.Alpha(glow, speechBubbleAlphaTweenSettings.WithDirection(false)));
        }
    }
}