using System;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using PrimeTween;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
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
        [field: SerializeField] private SerializableDictionary<StatPage, UIPanel> PanelDictionary { get; set; } = new();
        [SerializeReference, HideReferenceObjectPicker, HideLabel]
        private UIPanelController panelController = new();
        
        [Title("Tween")]
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        
        [TitleGroup("Debug")]
        [SerializeField, DisplayAsString] private StatPage currentPage = StatPage.Score;

        public override void Initialize()
        {
            base.Initialize();
            backButton.onClick.AddListener(OnBackButtonClicked);
            nextPageButton.onClick.AddListener(() => OnPagingButtonClicked(1));
            previousPageButton.onClick.AddListener(() => OnPagingButtonClicked(-1));
            PanelDictionary.Values.ForEach(p =>
            {
                p.Initialize();
            });
            panelController.ShowPanel(PanelDictionary[StatPage.Score]).Forget();
            UpdatePagingButtons();
        }
        
        public override Sequence TransitionIn()
        {
            TransitionState = TransitionState.TransitioningIn;
            transitionSequence = Sequence.Create()
                .Group(Tween.Alpha(panelCanvasGroup, transitionInTweenSettings))
                .OnComplete(() =>
                {
                    TransitionState = TransitionState.Idle;
                });
            return transitionSequence;
        }
        
        public override Sequence TransitionOut()
        {
            TransitionState = TransitionState.TransitioningOut;
            transitionSequence = Sequence.Create()
                .Group(Tween.Alpha(panelCanvasGroup, transitionOutTweenSettings))
                .OnComplete(() =>
                {
                    TransitionState = TransitionState.Idle;
                });
            return transitionSequence;
        }
        
        private async void OnBackButtonClicked()
        {
            var mainMenuPanel = MainMenuManager.Instance.PanelDictionary[MainMenuPanelType.MainMenu];
            ChangePanel(this, mainMenuPanel, CrossFadeSettings);
        }

        private async void OnPagingButtonClicked(int change)
        {
            var next = (int)currentPage + change;
            if (next < 0 || next > Enum.GetNames(typeof(StatPage)).Length - 1) return;
            var previousPage = PanelDictionary[currentPage];
            currentPage += change;
            var nextPage = PanelDictionary[currentPage];
            pageTitleText.text = currentPage.ToString();
            panelCanvasGroup.interactable = false;
            await panelController.ChangePanel(previousPage, nextPage, previousPage.CrossFadeSettings);
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