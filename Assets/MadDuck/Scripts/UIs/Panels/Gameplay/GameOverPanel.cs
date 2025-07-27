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
    [Title("References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Button continueButton;
    
    [Title("Panels")]
    [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule resultCrossFadeRule = new();

    
    public override void Initialize()
    {
        base.Initialize();
        continueButton.onClick.AddListener(OnContinueButtonClicked);
    }

    private void OnContinueButtonClicked()
    {
        GameManager.Instance.ToResultScreen();
        transitionCts = new CancellationTokenSource();
        PanelController.ChangePanel(this, resultCrossFadeRule.nextPanel, resultCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
    }
}
