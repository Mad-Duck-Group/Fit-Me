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
    public class TermsAndConditionsScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button acceptButton;
        
        [Title("Screen")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();

        public override void Initialize()
        {
            base.Initialize();
            acceptButton.onClick.AddListener(OnAcceptButtonClicked);
        }

        private void OnAcceptButtonClicked()
        {
            var loadingPanel = LoadSceneManager.Instance.TransitionScreens.Values.GetRandomElement();
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanelWithTransition(loadingPanel,this, mainMenuCrossFadeRule.nextPanel, mainMenuCrossFadeRule.crossFadeSettings, 
                transitionCts.Token).Forget();
        }
    }
}