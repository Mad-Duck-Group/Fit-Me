using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class MainMenuScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private TMP_Text versionText;

        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule settingsCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule statsCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule achievementsCrossFadeRule = new();

        public override void Initialize()
        {
            base.Initialize();
            playButton.onClick.AddListener(() =>
            {
                DeactivateInput();
                LoadSceneManager.Instance.LoadScene(SceneType.Gameplay, LoadSceneMode.Single, false);
            });
            settingsButton.onClick.AddListener(() => OnButtonClicked(settingsCrossFadeRule));
            statsButton.onClick.AddListener(() => OnButtonClicked(statsCrossFadeRule));
            achievementsButton.onClick.AddListener(() => OnButtonClicked(achievementsCrossFadeRule));
            versionText.text = Application.version;
        }

        private void OnButtonClicked(CrossFadeRule rule)
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, rule.nextPanel, rule.crossFadeSettings, 
                transitionCts.Token).Forget();
        }
    }
}