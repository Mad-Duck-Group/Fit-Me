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
        [SerializeField] private TMP_Text continueCountText;
        
        [Title("Settings")]
        [SerializeField] private float adsTimeout = 10f;
        [SerializeField] private bool enableAds = true;
        [SerializeField] private int maxContinueCount = 1;

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
            continueCountText.text = maxContinueCount.ToString();
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
            if (maxContinueCount <= 0)
            {
                EnableAds(false);
            }
            else
            {
                EnableAds(enableAds);
            }
        }

        public void OnAdsClosed()
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, gameplayUIPanelCrossFadeRule.nextPanel, gameplayUIPanelCrossFadeRule.crossFadeSettings,
                transitionCts.Token).Forget();
        }

        private void EnableAds(bool enable)
        {
            if (enable)
            {
                if (Ads.Instance.TryShowAd())
                    _adsTimerSubscription?.Dispose();
                maxContinueCount--;
            }
            else
            {
                // Add fallback logic here if ads are not available
               Debug.Log("Ads not available");
               return;
            }
        }
    }
}
