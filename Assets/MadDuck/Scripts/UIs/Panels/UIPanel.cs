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
    
    public interface IUIPanel : ISupportUITransition
    {
        string PanelName { get; }
        VisibilityState VisibilityState { get; }
        TransitionState TransitionState { get; set; }
        InputState InputState { get; }
        UIPanelController PanelController { get; set; }
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
        [TitleGroup("Transition", order: 9)]
        [DetailedInfoBox("<b>Read Me</b>",
            "Key:\n" +
            "<b>PanelCanvasGroup</b>\n" +
            "<b>PanelRectTransform</b>\n" +
            "are reserved for components of the panel. Do not use these keys for other components.",
            InfoMessageType.Warning)]
        [ShowInInspector, HideLabel]
        private InspectorVoid _keyInfo;
        [field: TitleGroup("Transition", order: 9)]
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
            TransitionObjectProviders.Add("PanelRectTransform", transform as RectTransform);
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

        public bool TryGetTransitionObject<T>(string key, out T transitionObject) where T : Component
        {
            if (TransitionObjectProviders.TryGetValue(key, out var obj) && obj is T component)
            {
                transitionObject = component;
                return true;
            }
            transitionObject = null;
            Debug.LogWarning($"Transition object with key '{key}' not found in panel '{PanelName}'. Ensure it is set up correctly.");
            return false;
        }

        private void OnDestroy()
        {
            CancelTransition();
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