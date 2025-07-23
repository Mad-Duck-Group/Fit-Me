using System;
using System.Collections.Generic;
using System.Linq;
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
    [Serializable]
    [ShowOdinSerializedPropertiesInInspector]
    public class UIPanelController
    {
        [Title("Settings")]
        [SerializeField] private float loadingScreenDuration = 1f;
        
        [Title("Debug")]
        [ShowInInspector, ReadOnly] private SerializableDictionary<int, List<IUIPanel>> DebugPanelPriority => new(_activePanelPriority);

        private SortedDictionary<int, List<IUIPanel>> _activePanelPriority = new();
        public List<IUIPanel> TopPanels => _activePanelPriority.Count == 0 ? 
            new List<IUIPanel>() : _activePanelPriority[_activePanelPriority.Keys.Max()];

        private CrossFadeSettings _currentCrossFadeSettings = null;

        public async UniTask ShowPanel(IUIPanel panel, CrossFadeSettings crossFadeSettings = null, CancellationToken cancellationToken = default)
        {
            panel.Show();
            if (crossFadeSettings != null)
            {
                crossFadeSettings.transitionGroupCollection.nextIn.Initialize(panel);
                panel.TransitionState = TransitionState.TransitioningIn;
                await crossFadeSettings.transitionGroupCollection.nextIn.Transition(cancellationToken);
                panel.TransitionState = TransitionState.Idle;
            }
            FocusPanel(panel);
            panel.OnPanelReady();
        }
        
        public async UniTask ChangePanelWithTransition(ITransitionScreen transition, IUIPanel previous, IUIPanel next, CrossFadeSettings crossFadeSettings = null,
            CancellationToken cancellationToken = default)
        {
            next.CancelTransition();
            _currentCrossFadeSettings?.transitionGroupCollection?.CancelAll();
            _currentCrossFadeSettings = crossFadeSettings;
            UnfocusPanel(previous);
            if (crossFadeSettings == null)
            {
                transition.Show();
                FocusPanel(transition);
                await transition.TransitionBeforeLoad(cancellationToken);
                previous.Hide();
                next.Show();
                await UniTask.WaitForSeconds(loadingScreenDuration, cancellationToken: cancellationToken);
                await transition.TransitionAfterLoad(cancellationToken);
                UnfocusPanel(transition);
                transition.Hide();
                FocusPanel(next);
                return;
            }
            
            //Initialize
            crossFadeSettings.transitionGroupCollection.nextIn.Initialize(next);
            crossFadeSettings.transitionGroupCollection.previousOut.Initialize(previous);
            crossFadeSettings.transitionGroupCollection.transitionIn.Initialize(transition);
            crossFadeSettings.transitionGroupCollection.transitionOut.Initialize(transition);
            //=======================================================================================//
            
            //Transition In
            transition.Show();
            transition.TransitionState = TransitionState.TransitioningIn;
            await crossFadeSettings.transitionGroupCollection.transitionIn.Transition(cancellationToken: cancellationToken);
            transition.TransitionState = TransitionState.Idle;
            //=======================================================================================//
            
            //Transition Before Load
            await transition.TransitionBeforeLoad(cancellationToken);
            FocusPanel(transition);
            transition.OnPanelReady();
            //=======================================================================================//
            
            //Previous Out
            previous.TransitionState = TransitionState.TransitioningOut;
            await crossFadeSettings.transitionGroupCollection.previousOut.Transition(cancellationToken: cancellationToken);
            previous.TransitionState = TransitionState.Idle;
            previous.Hide();
            //=======================================================================================//
            
            //Next In
            next.Show();
            next.TransitionState = TransitionState.TransitioningIn;
            await UniTask.WhenAll(crossFadeSettings.transitionGroupCollection.nextIn.Transition(cancellationToken: cancellationToken),
                UniTask.WaitForSeconds(loadingScreenDuration, cancellationToken: cancellationToken));
            next.TransitionState = TransitionState.Idle;
            //=======================================================================================//
            
            //Transition After Load
            await transition.TransitionAfterLoad(cancellationToken);
            transition.TransitionState = TransitionState.TransitioningOut;
            await crossFadeSettings.transitionGroupCollection.transitionOut.Transition(cancellationToken: cancellationToken);
            transition.TransitionState = TransitionState.Idle;
            UnfocusPanel(transition);
            transition.Hide();
            //=======================================================================================//
            
            //Next Ready
            FocusPanel(next);
            next.OnPanelReady();
            //=======================================================================================//
        }
        
        public async UniTask ChangePanel(IUIPanel previous, IUIPanel next, CrossFadeSettings crossFadeSettings = null, 
            CancellationToken cancellationToken = default)
        {
            next.CancelTransition();
            _currentCrossFadeSettings?.transitionGroupCollection?.CancelAll();
            _currentCrossFadeSettings = crossFadeSettings;
            UnfocusPanel(previous);
            if (crossFadeSettings == null)
            {
                await ShowPanel(next, cancellationToken: cancellationToken);
                return;
            }
            UniTask transitionOut;
            crossFadeSettings.transitionGroupCollection.previousOut.Initialize(previous);
            switch (crossFadeSettings.crossFadeType)
            {
                case CrossFadeType.Parallel:
                    previous.TransitionState = TransitionState.TransitioningOut;
                    transitionOut = crossFadeSettings.transitionGroupCollection.previousOut.Transition(cancellationToken);
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    ShowPanel(next, crossFadeSettings, cancellationToken: cancellationToken).Forget();
                    await transitionOut;
                    previous.TransitionState = TransitionState.Idle;
                    previous.Hide();
                    break;
                case CrossFadeType.InThenOut:
                    await ShowPanel(next, crossFadeSettings, cancellationToken: cancellationToken);
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    previous.TransitionState = TransitionState.TransitioningOut;
                    transitionOut = crossFadeSettings.transitionGroupCollection.previousOut.Transition(cancellationToken);
                    await transitionOut;
                    previous.TransitionState = TransitionState.Idle;
                    previous.Hide();
                    break;
                case CrossFadeType.OutThenIn:
                    previous.TransitionState = TransitionState.TransitioningOut;
                    transitionOut = crossFadeSettings.transitionGroupCollection.previousOut.Transition(cancellationToken);
                    await transitionOut;
                    previous.TransitionState = TransitionState.Idle;
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    ShowPanel(next, crossFadeSettings, cancellationToken: cancellationToken).Forget();
                    previous.Hide();
                    break;
                case CrossFadeType.OnlyIn:
                    await ShowPanel(next, crossFadeSettings, cancellationToken: cancellationToken);
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    //previous.Hide();
                    break;
                case CrossFadeType.OnlyOut:
                    previous.TransitionState = TransitionState.TransitioningOut;
                    await crossFadeSettings.transitionGroupCollection.previousOut.Transition(cancellationToken);
                    previous.TransitionState = TransitionState.Idle;
                    previous.Hide();
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    ShowPanel(next, cancellationToken: cancellationToken).Forget();
                    break;
                case CrossFadeType.None:
                    previous.Hide();
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    ShowPanel(next, cancellationToken: cancellationToken).Forget();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Focus a UIPanel and set its priority to the highest.
        /// </summary>
        /// <param name="panel">Panel to focus</param>
        /// <param name="coPriority">Use the same priority with the highest priority panels?</param>
        public void FocusPanel(IUIPanel panel, bool coPriority = false)
        {
            var topPriority = _activePanelPriority.Count > 0 
                ? _activePanelPriority.Keys.Max() 
                : 0;
            SetPriority(panel, coPriority ? topPriority + 1 : topPriority);
            //panel.ActivateInput();
            UpdateInputState();
        }
        
        /// <summary>
        /// Unfocus a UIPanel and remove it from the priority list.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="keepPriority"></param>
        public void UnfocusPanel(IUIPanel panel)
        {
            int? currentPriority = _activePanelPriority.Keys
                .Cast<int?>()
                .FirstOrDefault(k => k.HasValue && _activePanelPriority[k.Value].Contains(panel));
            if (!currentPriority.HasValue)
            {
                Debug.LogWarning($"UIPanel {panel.PanelName} is not found in the priority list.");
                return;
            }
            // Remove the panel from the current priority
            _activePanelPriority[currentPriority.Value].Remove(panel);
            // If the list is empty, remove the priority from the dictionary
            if (_activePanelPriority[currentPriority.Value].Count == 0)
            {
                _activePanelPriority.Remove(currentPriority.Value);
            }
            panel.DeactivateInput();
            //UpdateInputState();
        }

        public void SwapPriority(UIPanel a, UIPanel b)
        {
            //NOTE: Implement when needed
        }

        public void EquatePriority(UIPanel a, UIPanel b)
        {
            //NOTE: Implement when needed
        }

        public void SetPriority(IUIPanel panel, int priority)
        {
            int? previousPriority = _activePanelPriority.Keys
                .Cast<int?>()
                .FirstOrDefault(k => k.HasValue && _activePanelPriority[k.Value].Contains(panel));
            // If the panel is already in the priority list, remove it from its current priority
            if (previousPriority.HasValue)
            {
                _activePanelPriority[previousPriority.Value].Remove(panel);
                // If the list is empty, remove the priority from the dictionary
                if (_activePanelPriority[previousPriority.Value].Count == 0)
                {
                    _activePanelPriority.Remove(previousPriority.Value);
                }
            }

            // Add the panel to the new priority
            if (_activePanelPriority.TryGetValue(priority, out var existingPanels))
            {
                existingPanels.Add(panel);
            }
            else
            {
                _activePanelPriority[priority] = new List<IUIPanel> { panel };
            }
        }

        private void UpdateInputState()
        {
            var topPriority = _activePanelPriority.Count > 0 
                ? _activePanelPriority.Keys.Max() 
                : 0;
            var deactivatePanels = _activePanelPriority
                .Where(kvp => kvp.Key < topPriority)
                .SelectMany(kvp => kvp.Value)
                .ToList();
            deactivatePanels.ForEach(p => p.DeactivateInput());
            _activePanelPriority[topPriority].ForEach(p => p.ActivateInput());
        }
    }
}