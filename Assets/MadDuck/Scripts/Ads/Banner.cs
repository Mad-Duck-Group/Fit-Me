using System;
using GoogleMobileAds.Api;
using UnityEngine;

public class Banner : MonoBehaviour
{
    private BannerView bannerView;
    
    public void Start()
    {
        MobileAds.Initialize(iniStatus => { });
        this.RequestBanner();
    }
    
    private void RequestBanner()
    {
#if UNITY_ANDROID
            string adUnitId = "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IPHONE
            string adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
        string adUnitId = "unexpected_platform";
#endif

        // Create a 320x50 banner at the top of the screen.
        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Top);
        
        // Create an empty ad request.
        AdRequest request = new AdRequest();
        
        // Load the banner with the request.
        bannerView.LoadAd(request);
    }
}
