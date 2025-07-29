using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

[ShowOdinSerializedPropertiesInInspector]
public class PausePanel : UIPanel
{
    [Title("References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button closeSFXButton;
    [SerializeField] private Button closeMusicButton;
    
    [Title("Panels")]
    [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule gameplayCrossFadeRule = new();

    public override void Initialize()
    {
        base.Initialize();
        resumeButton.onClick.AddListener(OnResumeButtonClicked);
        helpButton.onClick.AddListener(OnHelpButtonClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuButton);
        closeSFXButton.onClick.AddListener(OnToggleMuteSFX);
        closeMusicButton.onClick.AddListener(OnToggleMuteBGM);
    }

    private void OnResumeButtonClicked()
    {
        GameManager.Instance.ResumeGame();
        transitionCts = new CancellationTokenSource();
        PanelController.ChangePanel(this, gameplayCrossFadeRule.nextPanel, gameplayCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
    }

    private void OnHelpButtonClicked()
    {
        Debug.Log("Help button clicked");
    }
    
    private void OnMainMenuButton()
    {
        GameManager.Instance.BackToMenu();
    }
    
    public void OnToggleMuteSFX()
    {
        AudioManager.Instance.ToggleMuteBus(BusType.SFX);
    }

    public void OnToggleMuteBGM()
    {
        AudioManager.Instance.ToggleMuteBus(BusType.BGM);
    }
}
