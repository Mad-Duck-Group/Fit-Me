using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ShowOdinSerializedPropertiesInInspector]
public class ResultPanel : UIPanel
{
    [Title("References")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private TMP_Text fitScoreText;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button tryAgainButton;
    
    [Title("Panels")]
    [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule gameplayCrossFadeRule = new();

    public override void Initialize()
    {
        base.Initialize();
        homeButton.onClick.AddListener(OnHomeButtonClicked);
        tryAgainButton.onClick.AddListener(OnTryAgainButtonClicked);
    }
    
    private void  OnHomeButtonClicked()
    {
        GameManager.Instance.BackToMenu();
    }
    
    private void OnTryAgainButtonClicked()
    {
        GameManager.Instance.Retry();
    }
}
