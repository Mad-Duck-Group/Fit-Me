using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Transitions;
using R3;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.Gameplay
{
    [ShowOdinSerializedPropertiesInInspector]
    public class GameOverPanel : UIPanel
    {
        [Title("References")] 
        [SerializeField] private Image adsTimer;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button adsButton;
        
        [Title("Settings")]
        [SerializeField] private float adsTimeout = 10f;

        [Title("Panels")] 
        [OdinSerialize, HideReferenceObjectPicker]
        private CrossFadeRule resultCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker]
        private CrossFadeRule gameplayUIPanelCrossFadeRule = new();
        
        private float _adsTimerValue;
        private IDisposable _adsTimerSubscription;


        public override void Initialize()
        {
            base.Initialize();
            continueButton.onClick.AddListener(OnSkipButtonClicked);
            adsButton.onClick.AddListener(OnAdsButtonClicked);
            ResetPanel();
        }

        public override void Show()
        {
            base.Show();
            ResetPanel();
        }

        private void ResetPanel()
        {
            adsTimer.fillAmount = 1;
        }

        public override void OnPanelReady()
        {
            base.OnPanelReady();
            _adsTimerValue = adsTimeout;
            _adsTimerSubscription = Observable.EveryUpdate(UnityFrameProvider.Update).Subscribe(_ =>
            {
                UpdateAdsTimer();
            });
        }

        private void UpdateAdsTimer()
        {
            _adsTimerValue -= Time.deltaTime;
            _adsTimerValue = Mathf.Clamp(_adsTimerValue, 0, adsTimeout);
            adsTimer.fillAmount = _adsTimerValue / adsTimeout;
            if (_adsTimerValue > 0) return;
            _adsTimerValue = adsTimeout;
            OnSkipButtonClicked();
        }

        private void OnSkipButtonClicked()
        {
            _adsTimerSubscription?.Dispose();
            GameManager.Instance.ToResultScreen().Forget();
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, resultCrossFadeRule.nextPanel, resultCrossFadeRule.crossFadeSettings,
                transitionCts.Token).Forget();
        }

        private void OnAdsButtonClicked()
        {
            if (Ads.Instance.TryShowAd())
                _adsTimerSubscription?.Dispose();
        }

        public void OnAdsClosed()
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, gameplayUIPanelCrossFadeRule.nextPanel, gameplayUIPanelCrossFadeRule.crossFadeSettings,
                transitionCts.Token).Forget();
        }
    }
}
