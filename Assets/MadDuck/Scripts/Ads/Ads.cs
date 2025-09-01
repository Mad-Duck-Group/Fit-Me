using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using MadDuck.Scripts.UIs.Panels.Gameplay;
using R3;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;

public class Ads : MonoSingleton<Ads>
{
    [FormerlySerializedAs("_gameOverPanel")] [SerializeField] private GameOverPanel gameOverPanel;
    private RewardedAd _rewardedAd;
    private IDisposable _adsRefreshTimer;
    private CancellationTokenSource _timerCts;
    private bool _rewarded;

    private void Start()
    {
        MobileAds.Initialize(LoadRewardedAd);
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
    }

    private void OnDestroy()
    {
        CancelAdsSessionTimer();
        DisposeAds();
    }

    void LoadRewardedAd(InitializationStatus status = null)
    {
        _rewardedAd = null;
        AdRequest request = new AdRequest();
        string adUnitId = null;

#if UNITY_ANDROID
        adUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
        adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
        adUnitId = "unexpected_platform";
#endif

        RewardedAd.Load(adUnitId, request, (ad, error) =>
        {
            if (error != null)
            {
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
    }
    
    public bool TryShowAd()
    {
        if (_rewardedAd == null || !_rewardedAd.CanShowAd()) return false;
        _rewardedAd.Show(_ =>
        {
            _rewarded = true;
        });
        return true;
    }

    private void HandleAdClosed()
    {
        LoadRewardedAd();
        if (!_rewarded) return;
        _rewarded = false;
        GameManager.Instance.Continue();
        gameOverPanel.OnAdsClosed();
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
