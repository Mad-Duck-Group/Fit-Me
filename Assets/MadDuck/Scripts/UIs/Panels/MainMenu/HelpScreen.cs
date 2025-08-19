using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class HelpScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        
        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();
        
        public override void Initialize()
        {
            base.Initialize();
            yesButton.onClick.AddListener(OnYesButtonClicked);
            noButton.onClick.AddListener(OnNoButtonClicked);
        }

        private void OnNoButtonClicked()
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, mainMenuCrossFadeRule.nextPanel, mainMenuCrossFadeRule.crossFadeSettings, 
                transitionCts.Token).Forget();
        }

        private void OnYesButtonClicked()
        {
            LoadSceneManager.Instance.LoadScene(SceneType.Tutorial, LoadSceneMode.Single, false).Forget();
        }
    }
}