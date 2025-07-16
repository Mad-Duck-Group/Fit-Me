using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using MadDuck.Scripts.UIs.Panels.MainMenu;
using PrimeTween;
using Sirenix.OdinInspector;
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
        VisibilityState VisibilityState { get; }
        TransitionState TransitionState { get; }
        InputState InputState { get; }
        
        void Show();
        void Hide();
        Sequence TransitionIn();
        Sequence TransitionOut();
        void ActivateInput();
        void DeactivateInput();
    }

    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour, IUIPanel
    {
        #region Inspectors
        [TitleGroup("Cross Fade", order: 9)]
        [SerializeField] protected CrossFadeSettings crossFadeSettings;
        
        [TitleGroup("Debug", order: 10)]
        [ShowInInspector, DisplayAsString]
        public VisibilityState VisibilityState { get; private set; } = VisibilityState.Hidden;
        [TitleGroup("Debug", order: 10)]
        [ShowInInspector, DisplayAsString]
        public TransitionState TransitionState { get; protected set; } = TransitionState.Idle;
        [TitleGroup("Debug", order: 10)]
        [ShowInInspector, DisplayAsString] 
        public InputState InputState { get; private set; } = InputState.Inactive;

        #endregion

        #region Fields and Properties

        protected CanvasGroup panelCanvasGroup;
        protected Sequence transitionSequence;
        public event Action<UIPanel, CrossFadeSettings, Action> ChangeUIPanelCallback;
        public event Action<ICascadeScreen, UIPanel> LoadingScreenCallback;
        #endregion

        #region Initialization

        protected virtual void Awake()
        {
            panelCanvasGroup = GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                Debug.LogError($"UIPanel {name} requires a CanvasGroup component.");
            }

            Hide();
            DeactivateInput();
        }

        #endregion

        #region Visibility

        public virtual void Show()
        {
            VisibilityState = VisibilityState.Shown;
            panelCanvasGroup.gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            VisibilityState = VisibilityState.Hidden;
            panelCanvasGroup.gameObject.SetActive(false);
        }

        #endregion

        #region Transitions

        public virtual Sequence TransitionIn()
        {
            TransitionState = TransitionState.TransitioningIn;
            return default;
        }

        public virtual Sequence TransitionOut()
        {
            TransitionState = TransitionState.TransitioningOut;
            return default;
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

        #region Events
        protected void ChangePanel(UIPanel newPanel, CrossFadeSettings crossFadeSettings = default, Action customCrossFade = null)
        {
            ChangeUIPanelCallback?.Invoke(newPanel, crossFadeSettings, customCrossFade);
        }
        
        protected void CascadeScreen(ICascadeScreen cascade, UIPanel next)
        {
            LoadingScreenCallback?.Invoke(cascade, next);
        }
        
        public void ClearEvents()
        {
            ChangeUIPanelCallback = null;
            LoadingScreenCallback = null;
        }
        #endregion
    }
}