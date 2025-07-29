using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu.SplashScreenPages
{
    public class StudioLogoPage : UIPanel, ISplashPage
    {
        [Title("References")]
        [SerializeField] private CanvasGroup madduckLogo;
        
        [Title("Tween")]
        [SerializeField] private TweenSettings<float> logoAlphaTweenSettings;
        
        [Title("Settings")]
        [SerializeField] private float splashScreenDuration = 2f;
        
        public event Action<ISplashPage> OnSplashCompleted;

        private Sequence _logoSequence;
        
        public override void Initialize()
        {
            base.Initialize();
            madduckLogo.alpha = 0f; // Start with the logo invisible
        }
        
        public override void OnPanelReady()
        {
            base.OnPanelReady();
            TweenLogo().Forget();
        }
        
        public void Skip()
        {
            _logoSequence.Complete();
            OnSplashCompleted?.Invoke(this);
        }
        
        private async UniTaskVoid TweenLogo()
        {
            _logoSequence = Sequence.Create()
                .Group(Tween.Alpha(madduckLogo, logoAlphaTweenSettings))
                .ChainDelay(splashScreenDuration)
                .Chain(Tween.Alpha(madduckLogo, logoAlphaTweenSettings.WithDirection(false))); // Fade out the logo after the duration
            await _logoSequence.ToUniTask();
            OnSplashCompleted?.Invoke(this);
        }
    }
}