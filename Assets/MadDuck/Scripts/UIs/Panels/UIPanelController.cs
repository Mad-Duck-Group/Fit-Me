using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels.MainMenu;
using MadDuck.Scripts.UIs.Panels.Transition;
using MadDuck.Scripts.Utils.Inspectors;
using PrimeTween;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Panels
{

    public enum CrossFadeType
    {
        Parallel,
        InThenOut,
        OutThenIn,
        Custom,
        None
    }

    [Serializable]
    public struct CrossFadeSettings
    {
        public CrossFadeType crossFadeType;
        [HideIf(nameof(crossFadeType), CrossFadeType.Custom)] public float customOffset;
        [ShowInInspector, ShowIf(nameof(crossFadeType), CrossFadeType.Custom)]
        [InfoBox("Custom cross fade requires manual scripting, if there are no cross fade, please check the implementation of this class"), HideLabel]
        private InspectorVoid _infoBox;
    }
    
    [Serializable]
    public class UIPanelController
    {
        [Title("Settings")]
        [SerializeField] private float loadingScreenDuration = 1f;
        
        [Title("Debug")]
        [ShowInInspector, ReadOnly] private SerializableDictionary<int, List<UIPanel>> DebugPanelPriority => new(_activePanelPriority);

        private SortedDictionary<int, List<UIPanel>> _activePanelPriority = new();
        public List<UIPanel> TopPanels => _activePanelPriority.Count == 0 ? 
            new List<UIPanel>() : _activePanelPriority[_activePanelPriority.Keys.Max()];

        public async UniTask ShowPanel(UIPanel panel)
        {
            panel.Show();
            await panel.TransitionIn().ToUniTask();
            FocusPanel(panel);
            panel.ChangePanelCallback += ChangePanel;
            panel.TransitionScreenCallback += TransitionScreen;
        }
        
        public async UniTask TransitionScreen(ITransitionScreen transition, UIPanel previous, UIPanel next)
        {
            previous.TransitionScreenCallback -= TransitionScreen;
            transition.Show();
            await transition.TransitionIn().ToUniTask();
            await transition.TransitionBefore().ToUniTask();
            await previous.TransitionOut().ToUniTask();
            UnfocusPanel(previous);
            previous.Hide();
            previous.ClearEvents();
            FocusPanel(transition as UIPanel);
            next.Show();
            await UniTask.WhenAll(next.TransitionIn().ToUniTask(), UniTask.WaitForSeconds(loadingScreenDuration));
            await transition.TransitionAfter().ToUniTask();
            await transition.TransitionOut().ToUniTask();
            UnfocusPanel(transition as UIPanel);
            transition.Hide();
            transition.ClearEvents();
            FocusPanel(next);
            next.ChangePanelCallback += ChangePanel;
            next.TransitionScreenCallback += TransitionScreen;
        }
        
        public async UniTask ChangePanel(UIPanel previous, UIPanel next, CrossFadeSettings crossFadeSettings = default, 
            Action customCrossFade = null, CancellationToken cancellationToken = default)
        {
            Debug.Log("Changing panel from " + previous.name + " to " + next.name);
            UnfocusPanel(previous);
            previous.ChangePanelCallback -= ChangePanel;
            UniTask transitionOut;
            switch (crossFadeSettings.crossFadeType)
            {
                case CrossFadeType.Parallel:
                    transitionOut = previous.TransitionOut().ToUniTask(cancellationToken: cancellationToken);
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    ShowPanel(next).Forget();
                    await transitionOut;
                    previous.Hide();
                    break;
                case CrossFadeType.InThenOut:
                    await ShowPanel(next);
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    transitionOut = previous.TransitionOut().ToUniTask(cancellationToken: cancellationToken);
                    await transitionOut;
                    previous.Hide();
                    break;
                case CrossFadeType.OutThenIn:
                    transitionOut = previous.TransitionOut().ToUniTask(cancellationToken: cancellationToken);
                    await transitionOut;
                    await UniTask.WaitForSeconds(crossFadeSettings.customOffset, cancellationToken: cancellationToken);
                    ShowPanel(next).Forget();
                    previous.Hide();
                    break;
                case CrossFadeType.Custom:
                    customCrossFade?.Invoke();
                    break;
                case CrossFadeType.None:
                    previous.TransitionOut().Complete();
                    previous.Hide();
                    ShowPanel(next).Forget();
                    break;
            }
            previous.ClearEvents();
        }
        
        /// <summary>
        /// Focus a UIPanel and set its priority to the highest.
        /// </summary>
        /// <param name="panel">Panel to focus</param>
        /// <param name="coPriority">Use the same priority with the highest priority panels?</param>
        public void FocusPanel(UIPanel panel, bool coPriority = false)
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
        public void UnfocusPanel(UIPanel panel)
        {
            int? currentPriority = _activePanelPriority.Keys
                .Cast<int?>()
                .FirstOrDefault(k => k.HasValue && _activePanelPriority[k.Value].Contains(panel));
            if (!currentPriority.HasValue)
            {
                Debug.LogWarning($"UIPanel {panel.name} is not found in the priority list.");
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

        public void SetPriority(UIPanel panel, int priority)
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
                _activePanelPriority[priority] = new List<UIPanel> { panel };
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