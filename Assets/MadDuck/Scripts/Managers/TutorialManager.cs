using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Tutorials;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using VContainer;

namespace MadDuck.Scripts.Managers
{
    [ShowOdinSerializedPropertiesInInspector]
    public class TutorialManager : MonoSingleton<TutorialManager>, ISerializationCallbackReceiver
    {
        [OdinSerialize, HideReferenceObjectPicker, HideLabel]
        private TutorialStateMachine tutorialStateMachine = new();
        
        [Title("Settings")]
        [SerializeField] private float tutorialStartDelay = 1.5f;
        
        [Title("Panel")] 
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule tutorialCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule gameplayCrossFadeRule = new();
        
        [Inject] private UIPanelController _panelController;
        private CancellationTokenSource _showTutorialCts;

        private void OnEnable()
        {
            GameManager.OnStartTutorial += OnStartTutorial;
            LoadSceneManager.OnStartFadeOut += OnSceneChanged;
        }

        private void OnDisable()
        {
            GameManager.OnStartTutorial -= OnStartTutorial;
            LoadSceneManager.OnStartFadeOut -= OnSceneChanged;
        }

        private void OnSceneChanged()
        {
            tutorialCrossFadeRule.nextPanel.DeactivateInput();
        }

        private void OnStartTutorial()
        {
            StartTutorial().Forget();
        }

        private async UniTaskVoid StartTutorial()
        {
            tutorialCrossFadeRule.nextPanel.Initialize();
            await UniTask.WaitForSeconds(tutorialStartDelay);
            await ShowTutorial();
            tutorialStateMachine.Initialize();
            tutorialStateMachine.StartTutorial();
        }
        
        public async UniTask ShowTutorial()
        {
            _showTutorialCts?.Cancel();
            _showTutorialCts = new CancellationTokenSource();
            await _panelController.ChangePanel(gameplayCrossFadeRule.nextPanel, tutorialCrossFadeRule.nextPanel, tutorialCrossFadeRule.crossFadeSettings, 
                _showTutorialCts.Token);
        }
        
        public async UniTask HideTutorial()
        {
            _showTutorialCts?.Cancel();
            _showTutorialCts = new CancellationTokenSource();
            await _panelController.ChangePanel(tutorialCrossFadeRule.nextPanel, gameplayCrossFadeRule.nextPanel, gameplayCrossFadeRule.crossFadeSettings, 
                _showTutorialCts.Token);
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