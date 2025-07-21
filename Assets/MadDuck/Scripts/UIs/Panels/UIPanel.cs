using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels.MainMenu;
using MadDuck.Scripts.UIs.Panels.Transition;
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
        void Initialize();
        void Show();
        void Hide();
        Sequence TransitionIn();
        Sequence TransitionOut();
        void ActivateInput();
        void DeactivateInput();
        void ClearEvents();
    }

    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour, IUIPanel
    {
        #region Inspectors
        [TitleGroup("Cross Fade", order: 9)]
        [field: SerializeField] public CrossFadeSettings CrossFadeSettings { get; private set; }
        
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
        public delegate UniTask OnChangeUIPanel(UIPanel previous, UIPanel next, CrossFadeSettings crossFadeSettings, 
            Action customCrossFade, CancellationToken cancellationToken = default);
        public event OnChangeUIPanel ChangePanelCallback;
        public delegate UniTask OnLoadingScreen(ITransitionScreen transitionScreen, UIPanel previous, UIPanel next);
        public event OnLoadingScreen TransitionScreenCallback;
        public CancellationTokenSource CancellationTokenSource { get; private set; } = new();
        #endregion

        #region Initialization

        public virtual void Initialize()
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
        protected void ChangePanel(UIPanel previous, UIPanel next, CrossFadeSettings crossFadeSettings = default, Action customCrossFade = null)
        {
            next.CancelTransition();
            CancellationTokenSource = new CancellationTokenSource();
            ChangePanelCallback?.Invoke(previous, next, crossFadeSettings, customCrossFade, CancellationTokenSource.Token);
        }
        
        protected void TransitionScreen(ITransitionScreen transition, UIPanel previous, UIPanel next)
        {
            TransitionScreenCallback?.Invoke(transition, previous, next);
        }
        
        public void ClearEvents()
        {
            ChangePanelCallback = null;
            TransitionScreenCallback = null;
        }

        public void CancelTransition()
        {
            transitionSequence.Stop();
            CancellationTokenSource?.Cancel();
        }
        #endregion
    }
}