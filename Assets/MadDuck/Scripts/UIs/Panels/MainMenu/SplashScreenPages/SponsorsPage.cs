using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Panels.MainMenu.SplashScreenPages
{
    public class SponsorsPage : UIPanel, ISplashPage
    {
        [Title("References")]
        [SerializeField] private CanvasGroup sponsorsLogo;

        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> logoAlphaTweenSettings;
        
        [Title("Settings")]
        [SerializeField] private float splashScreenDuration = 2f;
        
        private Sequence _logoSequence;
        public event Action<ISplashPage> OnSplashCompleted;
        
        public override void Initialize()
        {
            base.Initialize();
            sponsorsLogo.alpha = 0f; // Start with the logo invisible
        }
        
        public override void OnPanelReady()
        {
            base.OnPanelReady();
            TweenLogo().Forget();
        }
        
        private async UniTaskVoid TweenLogo()
        {
            _logoSequence = Sequence.Create()
                .Group(Tween.Alpha(sponsorsLogo, logoAlphaTweenSettings))
                .ChainDelay(splashScreenDuration)
                .Chain(Tween.Alpha(sponsorsLogo, logoAlphaTweenSettings.WithDirection(false)));
            await _logoSequence.ToUniTask();
            OnSplashCompleted?.Invoke(this);
        }
        
        public void Skip()
        {
            _logoSequence.Complete();
            OnSplashCompleted?.Invoke(this);
        }
    }
}