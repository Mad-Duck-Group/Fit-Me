using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    [ShowOdinSerializedPropertiesInInspector]
    public class SplashScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private RectTransform madduckLogo;
        [SerializeField] private float splashScreenDuration = 3f;

        [Title("Tween")]
        [SerializeField] private TweenSettings<Vector3> logoScaleTweenSettings;

        [Title("Panel")] 
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule termsAndConditionsCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();
        
        private Sequence _logoSequence;

        public override void Initialize()
        {
            base.Initialize();
            madduckLogo.localScale = Vector3.zero; // Start with the logo scaled down
        }

        public override void OnPanelReady()
        {
            base.OnPanelReady();
            TweenLogo().Forget();
        }

        private async UniTaskVoid TweenLogo()
        {
            _logoSequence = Sequence.Create()
                .Group(Tween.Scale(madduckLogo, logoScaleTweenSettings));
            await _logoSequence.ToUniTask();
            await UniTask.WaitForSeconds(splashScreenDuration);
            transitionCts = new CancellationTokenSource();
            // PanelController.ChangePanel(this, termsAndConditionsCrossFadeRule.nextPanel, termsAndConditionsCrossFadeRule.crossFadeSettings,
            //     transitionCts.Token).Forget();
            var transitionScreen = LoadSceneManager.Instance.TransitionScreens.Values.GetRandomElement();
            await PanelController.ChangePanelWithTransition(transitionScreen, this, mainMenuCrossFadeRule.nextPanel,
                mainMenuCrossFadeRule.crossFadeSettings);
        }

    }
}