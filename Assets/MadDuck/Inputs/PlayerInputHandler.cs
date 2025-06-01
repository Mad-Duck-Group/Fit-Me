using System;
using Cysharp.Threading.Tasks;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Inputs
{
    /// <summary>
    /// Handle player input. Required in the same object as CharacterHub with Player character type.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour, PlayerInputAction.IPlayerActions
    {
        #region Data Structures
        [Serializable]
        public record InputButton(InputAction InputAction)
        {
            private InputAction InputAction { get; set; } = InputAction;
            [ShowInInspector, DisplayAsString] 
            public string ButtonName =>
                InputAction != null 
                    ? InputAction.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions) 
                    : string.Empty;
            public bool isDown;
            public bool isUp;
            public bool isHeld;
            public bool isUpAfterHeld;
            /// <summary>
            /// Warning: Use isUpAfterHeld if you want to check if the button is released after being held. This property is only for input handler.
            /// </summary>
            public bool heldLastTime;
            public InputBinding? inputBinding;
        }
        #endregion

        #region Inspector

        #region Values
        [field: SerializeField, ReadOnly] public bool AnyButtonPressed { get; private set; }
        [field: SerializeField, ReadOnly] public Vector2 MouseDelta { get; private set; }

        #endregion
        
        #region Buttons
        [field: SerializeField, ReadOnly] 
        public SerializableReactiveProperty<InputButton> InteractButton { get; private set; }
        [field: SerializeField, ReadOnly] 
        public SerializableReactiveProperty<InputButton> Action0Button { get; private set; }
        [field: SerializeField, ReadOnly] 
        public SerializableReactiveProperty<InputButton> Action1Button { get; private set; }
        [field: SerializeField, ReadOnly]
        public SerializableReactiveProperty<InputButton> PauseGameButton { get; private set; }
        #endregion
        
        #endregion

        #region Fields
        private PlayerInputAction _playerInputAction;
        private IDisposable _anyButtonPressListener;
        #endregion

        #region Life Cycle
        private void OnEnable()
        {
            Subscribe();
            RegisterInputAction();
        }
        
        private void OnDisable()
        {
            Unsubscribe();
        }

        private void RegisterInputAction()
        {
            // InteractButton.Value = new InputButton(_playerInputAction.Player.Interact);
            // JerkBaitButton.Value = new InputButton(_playerInputAction.Player.JerkBait);
            // Action0Button.Value = new InputButton(_playerInputAction.Player.Action0);
            // Action1Button.Value = new InputButton(_playerInputAction.Player.Action1);
            // ReelingButton.Value = new InputButton(_playerInputAction.Player.Reeling);
            // JerkBindings = _playerInputAction.Player.JerkBait.bindings.ToArray();
        }
        #endregion

        #region Subscriptions

        private void Subscribe()
        {
            if (_playerInputAction == null)
            {
                _playerInputAction = new PlayerInputAction();
                _playerInputAction.Player.SetCallbacks(this);
            }
            _playerInputAction.Player.Enable();
            _anyButtonPressListener = InputSystem.onAnyButtonPress.Call(OnAnyButton);
        }
    
        private void Unsubscribe()
        {
            _playerInputAction.Player.Disable();
            _anyButtonPressListener?.Dispose();
        }
        #endregion

        #region Event Handlers

        private async void OnAnyButton(InputControl inputControl)
        {
            AnyButtonPressed = true;
            await UniTask.WaitForEndOfFrame();
            AnyButtonPressed = false;
        }

        public void OnPauseGame(InputAction.CallbackContext context)
        {
            BindPressButton(PauseGameButton, context);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            BindPressButton(InteractButton, context);
        }

        public void OnAction0(InputAction.CallbackContext context)
        {
            BindHoldButton(Action0Button, context);
        }

        public void OnAction1(InputAction.CallbackContext context)
        {
            BindPressButton(Action1Button, context);
        }

        public void OnMouseDelta(InputAction.CallbackContext context)
        {
            MouseDelta = context.ReadValue<Vector2>();
        }

        #endregion

        #region Button
        private void BindPressButton(ReactiveProperty<InputButton> button, InputAction.CallbackContext context)
        {
            button.Value.isDown = context.performed;
            button.Value.isUp = context.canceled;
            button.Value.isHeld = context.performed;
            button.Value.isUpAfterHeld = context.canceled;
            button.Value.heldLastTime = context.performed;
            button.Value.inputBinding = context.action.GetBindingForControl(context.control);
            button.OnNext(button.Value);
            ButtonPressTask(button).Forget();
        }

        private async UniTaskVoid ButtonPressTask(ReactiveProperty<InputButton> button)
        {
            await UniTask.WaitForEndOfFrame();
            button.Value.isDown = false;
            if (!button.Value.isHeld)
            {
                button.Value.isUp = false;
                button.Value.isUpAfterHeld = false;
            }
            button.OnNext(button.Value);
        }
        
        private void BindHoldButton(ReactiveProperty<InputButton> button, InputAction.CallbackContext context)
        {
            button.Value.inputBinding = context.action.GetBindingForControl(context.control);
            switch (context)
            {
                case { started: true, performed: false }:
                    button.Value.isDown = true;
                    button.Value.isHeld = false;
                    button.Value.isUp = false;
                    button.Value.isUpAfterHeld = false;
                    button.Value.heldLastTime = false;
                    button.OnNext(button.Value);
                    ButtonPressTask(button).Forget();
                    break;
                case { performed: true }:
                    button.Value.isDown = false;
                    button.Value.isHeld = true;
                    button.Value.isUp = false;
                    button.Value.isUpAfterHeld = false;
                    button.Value.heldLastTime = true;
                    button.OnNext(button.Value);
                    break;
                case { canceled: true }:
                    button.Value.isDown = false;
                    button.Value.isHeld = false;
                    button.Value.isUp = true;
                    button.Value.isUpAfterHeld = button.Value.heldLastTime;
                    button.Value.heldLastTime = false;
                    button.OnNext(button.Value);
                    ButtonPressTask(button).Forget();
                    break;
            }
        }
        #endregion
    }
}
