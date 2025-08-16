using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Others;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public interface ISplashPage
    {
        public event Action<ISplashPage> OnSplashCompleted;
        public event Action<ISplashPage> OnSplashFinishedTransitionIn;
        public void Skip(bool retain = false);
    }
    
    [ShowOdinSerializedPropertiesInInspector]
    public class SplashScreen : UIPanel
    {
        private enum SplashPage
        {
            Studio = 0,
            Sponsors = 1,
        }

        [Title("References")] 
        [SerializeField] private ClickableArea skipArea;
        [field: OdinSerialize, HideReferenceObjectPicker] private SerializableDictionary<SplashPage, PageCrossFadeRule> PanelDictionary { get; set; } = new();
        [SerializeReference, HideReferenceObjectPicker, HideLabel]
        private UIPanelController pageController = new();

        [Title("Panel")] 
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule termsAndConditionsCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();

        [Title("Settings")] 
        [SerializeField] private int skipAfterPage = 1;
        [SerializeField] private bool skipAll = true;
        [SerializeField] private bool retainOnLastSkip = true;
        [SerializeField] private SplashPage initialSplashPage = SplashPage.Studio;

        [Title("Debug")] 
        [SerializeField, DisplayAsString] private SplashPage currentPage = SplashPage.Studio;
        
        private int _shownCount;

        private void OnEnable()
        {
            skipArea.OnClicked += SkipCurrentPage;
        }
        
        private void OnDisable()
        {
            skipArea.OnClicked -= SkipCurrentPage;
        }
        
        private void SkipCurrentPage()
        {
            if (_shownCount < skipAfterPage) return;
            if (PanelDictionary.TryGetValue(currentPage, out var pageRule) && pageRule.thisPanel is ISplashPage splashPage)
            {
                if (!skipAll)
                {
                    var nextPage = (int)currentPage + 1;
                    var lastPage = nextPage >= PanelDictionary.Count;
                    splashPage.Skip(lastPage && retainOnLastSkip);
                }
                else
                {
                    splashPage.OnSplashCompleted -= OnPageCompleted;
                    splashPage.OnSplashFinishedTransitionIn -= OnFinishedTransitionIn;
                    splashPage.Skip(retainOnLastSkip);
                    ToMainMenu();
                }
            }
            else
            {
                Debug.LogError($"The current splash page {currentPage} does not implement ISplashPage interface.");
            }
        }
        
        public override void Initialize()
        {
            base.Initialize();
            PanelDictionary.Values.ForEach(p =>
            {
                p.thisPanel.Initialize();
                p.thisPanel.PanelController = pageController;
            });
        }

        public override void OnPanelReady()
        {
            base.OnPanelReady();
            _shownCount = 0;
            var initialPage = PanelDictionary[initialSplashPage];
            if (initialPage.thisPanel is not ISplashPage page)
            {
                Debug.LogError($"The splash page {initialSplashPage} does not implement ISplashPage interface.");
                return;
            }
            currentPage = initialSplashPage;
            pageController.ShowPanel(initialPage.thisPanel).Forget();
            page.OnSplashCompleted += OnPageCompleted;
            page.OnSplashFinishedTransitionIn += OnFinishedTransitionIn;
        }

        private void OnFinishedTransitionIn(ISplashPage page)
        {
            _shownCount++;
        }

        private void OnPageCompleted(ISplashPage completedPage)
        {
            completedPage.OnSplashCompleted -= OnPageCompleted;
            completedPage.OnSplashFinishedTransitionIn -= OnFinishedTransitionIn;
            var nextPage = (int)currentPage + 1;
            if (nextPage >= PanelDictionary.Count)
            {
                // All splash pages completed, transition to main menu
               ToMainMenu();
            }
            else
            {
                // Show the next splash page
                var previousPanel = PanelDictionary[currentPage];
                currentPage = (SplashPage)nextPage;
                var nextPanel = PanelDictionary[currentPage];
                if (nextPanel.thisPanel is ISplashPage nextPageInstance)
                {
                    pageController.ChangePanel(completedPage as UIPanel, nextPanel.thisPanel, previousPanel.crossFadeSettings).Forget();
                    nextPageInstance.OnSplashCompleted += OnPageCompleted;
                    nextPageInstance.OnSplashFinishedTransitionIn += OnFinishedTransitionIn;
                }
                else
                {
                    Debug.LogError($"The splash page {currentPage} does not implement ISplashPage interface.");
                }
            }
        }

        private void ToMainMenu()
        {
            var transitionScreen = LoadSceneManager.Instance.TransitionScreens.Values.GetRandomElement();
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanelWithTransition(transitionScreen, this, mainMenuCrossFadeRule.nextPanel, 
                mainMenuCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
        }
    }
}