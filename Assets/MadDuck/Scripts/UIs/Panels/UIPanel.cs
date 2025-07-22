using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels.MainMenu;
using MadDuck.Scripts.UIs.Panels.Transition;
using MadDuck.Scripts.UIs.Transitions;
using MadDuck.Scripts.Utils.Inspectors;
using PrimeTween;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Panels
{
    public enum VisibilityState
    {
        Shown,
        Hidden
    }
    
    public enum TransitionState
    {
        TransitioningIn,
        TransitioningOut,
        Idle
    }
    
    public enum InputState
    {
        Active,
        Inactive
    }
    
    public interface IUIPanel
    {
        string PanelName { get;}
        VisibilityState VisibilityState { get; }
        TransitionState TransitionState { get; set; }
        InputState InputState { get; }
        UIPanelController PanelController { get; set; }
        SerializableDictionary<string, Component> TransitionObjectProviders { get; }
        void Initialize();
        void Show();
        void Hide();
        void ActivateInput();
        void DeactivateInput();
        void CancelTransition();
        void OnPanelReady();
    }

    [RequireComponent(typeof(CanvasGroup))]
    [ShowOdinSerializedPropertiesInInspector]
    public abstract class UIPanel : MonoBehaviour, IUIPanel, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        #region Inspectors
        [TitleGroup("Cross Fade", order: 9)]
        [InfoBox(
            "Key 'PanelCanvasGroup' is reserved for the CanvasGroup component of the panel. Do not use it for other components.", InfoMessageType.Warning)]
        [ShowInInspector, HideLabel]
        private InspectorVoid _keyInfo;
        [field: TitleGroup("Cross Fade", order: 9)]
        [field: SerializeField] public SerializableDictionary<string, Component> TransitionObjectProviders { get; private set; } = new();

        [TitleGroup("Debug", order: 10)]
        [ShowInInspector, DisplayAsString]
        public VisibilityState VisibilityState { get; private set; } = VisibilityState.Hidden;
        [TitleGroup("Debug", order: 10)]
        [ShowInInspector, DisplayAsString]
        public TransitionState TransitionState { get; set; } = TransitionState.Idle;
        [TitleGroup("Debug", order: 10)]
        [ShowInInspector, DisplayAsString] 
        public InputState InputState { get; private set; } = InputState.Inactive;
        [TitleGroup("Debug", order: 10)]
        [ShowInInspector, ReadOnly]
        public UIPanelController PanelController { get; set; }
        #endregion

        #region Fields and Properties
        public string PanelName => gameObject.name;
        protected CanvasGroup panelCanvasGroup;
        protected CancellationTokenSource transitionCts;
        #endregion

        #region Initialization

        public virtual void Initialize()
        {
            panelCanvasGroup = GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                Debug.LogError($"UIPanel {name} requires a CanvasGroup component.");
            }
            TransitionObjectProviders.Add("PanelCanvasGroup", panelCanvasGroup);
            Hide();
            DeactivateInput();
        }

        #endregion

        #region Visibility

        public virtual void Show()
        {
            gameObject.SetActive(true);
            VisibilityState = VisibilityState.Shown;
            panelCanvasGroup.alpha = 1f;
        }

        public virtual void Hide()
        {
            VisibilityState = VisibilityState.Hidden;
            panelCanvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        #endregion

        #region Input Management

        public virtual void ActivateInput()
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
            InputState = InputState.Active;
        }

        public virtual void DeactivateInput()
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            InputState = InputState.Inactive;
        }

        #endregion

        #region Others
        public virtual void CancelTransition()
        {
            transitionCts?.Cancel();
        }

        public virtual void OnPanelReady()
        {
            
        }
        #endregion

        #region Serialization
        [SerializeField, HideInInspector]
        private SerializationData serializationData;
        SerializationData ISupportsPrefabSerialization.SerializationData 
        {
            get => serializationData;
            set => serializationData = value;
        }
        public void OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
        }

        public void OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
        }
        #endregion
    }
}