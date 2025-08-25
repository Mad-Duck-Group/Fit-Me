using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using MadDuck.Scripts.UIs.Panels.Gameplay;
using UnityCommunity.UnitySingleton;
using UnityEngine;

public class Ads : MonoSingleton<Ads>
{
    private RewardedAd _rewardedAd;
    public GameOverPanel _gameOverPanel;

    void Start()
    {
        MobileAds.Initialize(initStatus => {
            LoadRewardedAd();
        });
        _ = CountdownAdSession();
    }

    void LoadRewardedAd()
    {
        string adUnitId;
#if UNITY_ANDROID
            adUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
            adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
            adUnitId = "unexpected_platform";
#endif
        AdRequest request = new AdRequest();

        RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("โฆษณาโหลดไม่สำเร็จ: " + error.GetMessage());
                return;
            }

            _rewardedAd = ad;
            RegisterAdEvents();
        });
    }

    void RegisterAdEvents()
    {
        _rewardedAd.OnAdFullScreenContentClosed += HandleAdClosed;

        _rewardedAd.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"ได้รับรายได้จากโฆษณา: {adValue.Value} {adValue.CurrencyCode}");
        };
    }
    
    public void ShowAd()
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show(reward =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.Continue();
                else
                    Debug.LogWarning("GameManager.Instance is null");

                if (_gameOverPanel != null)
                    _gameOverPanel.OnAdsClosed();
                else
                    Debug.LogWarning("_gameOverPanel is null");
            });
        }
        else
        {
            Debug.Log("โฆษณายังไม่พร้อมแสดง");
        }
    }
    
    private async void HandleAdClosed()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.OnAdFullScreenContentClosed -= HandleAdClosed;
            _rewardedAd.Destroy();
        }
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        LoadRewardedAd();
    }


    private async UniTask CountdownAdSession()
    {
        var token = this.GetCancellationTokenOnDestroy();
        await UniTask.Delay(TimeSpan.FromHours(1), cancellationToken: token);
        LoadRewardedAd();
    }

}
