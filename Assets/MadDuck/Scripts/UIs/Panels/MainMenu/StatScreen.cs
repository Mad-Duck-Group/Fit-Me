using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class StatScreen : UIPanel
    {
        private enum StatPage
        {
            Score = 0,
            FitMe = 1,
            Achievement = 2,
        }

        
        
        [Title("References")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button previousPageButton;
        [SerializeField] private TMP_Text pageTitleText;
        [field: OdinSerialize, HideReferenceObjectPicker] private SerializableDictionary<StatPage, PageCrossFadeRule> PanelDictionary { get; set; } = new();
        [FormerlySerializedAs("panelController")] [SerializeReference, HideReferenceObjectPicker, HideLabel]
        private UIPanelController pageController = new();
        
        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();

        [TitleGroup("Debug")]
        [SerializeField, DisplayAsString] private StatPage currentPage = StatPage.Score;

        public override void Initialize()
        {
            base.Initialize();
            backButton.onClick.AddListener(OnBackButtonClicked);
            nextPageButton.onClick.AddListener(() => OnPagingButtonClicked(1).Forget());
            previousPageButton.onClick.AddListener(() => OnPagingButtonClicked(-1).Forget());
            PanelDictionary.Values.ForEach(p =>
            {
                p.thisPanel.Initialize();
            });
            pageController.ShowPanel(PanelDictionary[StatPage.Score].thisPanel).Forget();
            UpdatePagingButtons();
        }

        private void OnBackButtonClicked()
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, mainMenuCrossFadeRule.nextPanel, 
                mainMenuCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
        }

        private async UniTaskVoid OnPagingButtonClicked(int change)
        {
            var next = (int)currentPage + change;
            if (next < 0 || next > Enum.GetNames(typeof(StatPage)).Length - 1) return;
            var previousPage = PanelDictionary[currentPage];
            currentPage += change;
            var nextPage = PanelDictionary[currentPage];
            pageTitleText.text = currentPage.ToString();
            panelCanvasGroup.interactable = false;
            await pageController.ChangePanel(previousPage.thisPanel, nextPage.thisPanel, previousPage.crossFadeSettings);
            UpdatePagingButtons();
            panelCanvasGroup.interactable = true;
        }

        private void UpdatePagingButtons()
        {
            nextPageButton.interactable = (int)currentPage != Enum.GetNames(typeof(StatPage)).Length - 1;
            previousPageButton.interactable = currentPage != 0;
        }
    }
}