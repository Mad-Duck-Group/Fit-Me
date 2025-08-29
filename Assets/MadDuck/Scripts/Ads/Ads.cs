using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using MadDuck.Scripts.UIs.Panels.Gameplay;
using R3;
using UnityCommunity.UnitySingleton;
using UnityEngine;

public class Ads : MonoSingleton<Ads>
{
    private RewardedAd _rewardedAd;
    public GameOverPanel _gameOverPanel;
    private IDisposable _adsRefreshTimer;
    private CancellationTokenSource _timerCts;

    void Start()
    {
        MobileAds.Initialize(LoadRewardedAd);
    }

    private void OnDestroy()
    {
        CancelAdsSessionTimer();
        DisposeAds();
    }

    void LoadRewardedAd(InitializationStatus status = null)
    {
        _rewardedAd = null;
        string adUnitId;
#if UNITY_ANDROID
            adUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
            adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
            adUnitId = "unexpected_platform";
#endif
        AdRequest request = new AdRequest();

        RewardedAd.Load(adUnitId, request, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError("โฆษณาโหลดไม่สำเร็จ: " + error.GetMessage());
                return;
            }

            _rewardedAd = ad;
            RegisterAdEvents();
            CountdownAdSession();
        });
    }

    void RegisterAdEvents()
    {
        _rewardedAd.OnAdFullScreenContentClosed += HandleAdClosed;

        _rewardedAd.OnAdPaid += adValue =>
        {
            var micros = adValue.Value;
            var currency = adValue.CurrencyCode;
            var precision = adValue.Precision;
            
            Debug.Log($"ได้รับรายได้จากโฆษณา: {micros} {currency}");
        };
    }
    
    public bool TryShowAd()
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show(reward =>
            {
                GameManager.Instance.Continue();
                _gameOverPanel.OnAdsClosed();
            });
            return true;
        }
        Debug.Log("โฆษณายังไม่พร้อมแสดง");
        return false;
    }
    
    private void HandleAdClosed()
    {
        LoadRewardedAd();
    }


    private void CountdownAdSession()
    {
        CancelAdsSessionTimer();
        _timerCts = new CancellationTokenSource();
        _adsRefreshTimer = Observable.Timer(TimeSpan.FromHours(1), _timerCts.Token)
            .Subscribe(_ => 
            {
                LoadRewardedAd();
                CountdownAdSession(); 
            });
    }

    private void CancelAdsSessionTimer()
    {
        _timerCts?.Cancel();
        _adsRefreshTimer?.Dispose();
    }

    private void DisposeAds()
    {
        if (_rewardedAd == null) return;
        _rewardedAd.OnAdFullScreenContentClosed -= HandleAdClosed;
        _rewardedAd.Destroy();
    }

}
