using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ShowOdinSerializedPropertiesInInspector]
public class GameOverPanel : UIPanel
{
    [Title("References")] [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button adsButton;

    [Title("Panels")] 
    [OdinSerialize, HideReferenceObjectPicker]
    private CrossFadeRule resultCrossFadeRule = new();
    [OdinSerialize, HideReferenceObjectPicker]
    private CrossFadeRule gameplayUIPanelCrossFadeRule = new();


    public override void Initialize()
    {
        base.Initialize();
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        adsButton.onClick.AddListener(OnAdsButtonClicked);
    }

    private void OnContinueButtonClicked()
    {
        GameManager.Instance.ToResultScreen();
        transitionCts = new CancellationTokenSource();
        PanelController.ChangePanel(this, resultCrossFadeRule.nextPanel, resultCrossFadeRule.crossFadeSettings,
            transitionCts.Token).Forget();
    }

    private void OnAdsButtonClicked()
    {
        GameManager.Instance.Continue();
        transitionCts = new CancellationTokenSource();
        PanelController.ChangePanel(this, gameplayUIPanelCrossFadeRule.nextPanel, gameplayUIPanelCrossFadeRule.crossFadeSettings,
            transitionCts.Token).Forget();
    }

}
