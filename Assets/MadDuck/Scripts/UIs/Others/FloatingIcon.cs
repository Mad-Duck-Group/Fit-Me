using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Transitions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Others
{
    public interface IFloatingUIElement
    {
        void Initialize();
        UniTask Show();
        UniTask Hide();
        UniTask PlayAnimation();
        void Cancel();
    }
    
    [ShowOdinSerializedPropertiesInInspector]
    public class FloatingIcon : MonoBehaviour, IFloatingUIElement, ISupportUITransition, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        [SerializeField] private SerializableDictionary<string, Component> transitionObjects = new();
        [OdinSerialize] private TransitionGroup showTransitionGroup = new();
        [OdinSerialize] private TransitionGroup hideTransitionGroup = new();
        [OdinSerialize] private TransitionGroup animationTransitionGroup = new();
        private CancellationTokenSource _cts = new();
        private bool _firstShow;
        private Vector2 _initialPosition;
        
        public void Initialize()
        {
            showTransitionGroup.Initialize(this);
            hideTransitionGroup.Initialize(this);
            animationTransitionGroup.Initialize(this);
            _cts = new CancellationTokenSource();
        }
        
        public async UniTask Show()
        {
            if (!_firstShow)
            {
                _firstShow = true;
                _initialPosition = transform.localPosition;
            }
            else
            {
                transform.localPosition = _initialPosition;
            }
            Cancel();
            gameObject.SetActive(true);
            await showTransitionGroup.Transition(_cts.Token);
        }

        public async UniTask Hide()
        {
            Cancel();
            await hideTransitionGroup.Transition(_cts.Token);
            gameObject.SetActive(false);
        }

        public async UniTask PlayAnimation()
        {
            Cancel();
            await animationTransitionGroup.Transition(_cts.Token);
        }

        public void Cancel()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            showTransitionGroup.CancelTransition();
            hideTransitionGroup.CancelTransition();
            animationTransitionGroup.CancelTransition();
        }

        public bool TryGetTransitionObject<T>(string key, out T component) where T : Component
        {
            if (transitionObjects != null && transitionObjects.TryGetValue(key, out var obj) && obj is T tObj)
            {
                component = tObj;
                return true;
            }
            component = null;
            return false;
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