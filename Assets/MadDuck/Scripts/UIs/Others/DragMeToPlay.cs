using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils;
using MadDuck.Scripts.Utils.Inspectors;
using PrimeTween;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace MadDuck.Scripts.UIs.Others
{
    [ShowOdinSerializedPropertiesInInspector]
    public class DragMeToPlay : MonoBehaviour, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        [Title("References")]
        [SerializeField] private SpriteRenderer speechBubble;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private Transform floatingHandIconParent;
        [OdinSerialize] private IFloatingUIElement floatingHandIconPrefab;
        
        private IFloatingUIElement _floatingHandIconInstance;
        private GameObject _handIconGameObject;
        
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
            if (_handIconGameObject)
            {
                Destroy(_handIconGameObject);
            }
        }

        private void OnBlockSpawned(List<Block> blocks)
        {
            _block = blocks[0];
            _block.OnBlockBeingDrag += FadeOutBubble;
            _block.OnBlockEndDrag += FadeInBubble;
            _floatingHandIconInstance = floatingHandIconPrefab.InstantiateAsInterface(new InstantiateParameters()
                {
                    parent = floatingHandIconParent,
                }, 
                out _handIconGameObject);
            _floatingHandIconInstance.Initialize();
            _handIconGameObject.transform.SetAsFirstSibling();
            var iconPosition = PointerManager.Instance.WorldToWorldCanvasPosition(_block.transform.position);
            _handIconGameObject.transform.position = iconPosition;
            ShowHandIcon().Forget();
        }
        
        private async UniTaskVoid ShowHandIcon()
        {
            await _floatingHandIconInstance.Show();
            await _floatingHandIconInstance.PlayAnimation();
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
            _floatingHandIconInstance.Hide().Forget();
        }
        
        private void FadeInBubble(bool placed)
        {
            if (_fit) return;
            _speechBubbleSequence.Stop();
            speechBubble.gameObject.SetActive(true);
            glow.gameObject.SetActive(true);
            _speechBubbleSequence = Sequence.Create()
                .Group(Tween.Alpha(speechBubble, speechBubbleAlphaTweenSettings.WithDirection(false)))
                .Group(Tween.Alpha(glow, speechBubbleAlphaTweenSettings.WithDirection(false)));
            ShowHandIcon().Forget();
        }
        
        #region Serialization
        public void OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
        }

        public void OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
        }

        [SerializeField, HideInInspector]
        private SerializationData serializationData;
        public SerializationData SerializationData 
        { 
            get => serializationData;
            set => serializationData = value;
        }
        #endregion
    }
}