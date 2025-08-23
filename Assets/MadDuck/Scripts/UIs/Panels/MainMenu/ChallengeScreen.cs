using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Others;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class ChallengeScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Image profileImage;
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private TMP_Text highscoreText;
        [SerializeField] private TMP_Text mostFitText;
        [SerializeField] private ScoreBlock scoreBlockPrefab;
        [SerializeField] private ChallengeBlock challengeBlockPrefab;
        [SerializeField] private Button backButton;
        
        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();

        public override void Initialize()
        {
            base.Initialize();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        public override void Show()
        {
            base.Show();
            var recordData = PlayerDataManager.Instance.PlayerRecordData;
            highscoreText.text = recordData.highScore.score.ToString("N0");
            mostFitText.text = recordData.mostFitMe.fitMe.ToString("N0");
        }
        
        private void OnBackButtonClicked()
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, mainMenuCrossFadeRule.nextPanel, mainMenuCrossFadeRule.crossFadeSettings, 
                transitionCts.Token).Forget();
        }
    }
}