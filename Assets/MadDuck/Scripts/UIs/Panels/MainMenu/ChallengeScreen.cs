using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private RectTransform recordParent;
        [SerializeField] private RecordBlock recordBlockPrefab;
        [SerializeField] private RectTransform recordDivider;
        [SerializeField] private RectTransform challengeParent;
        [SerializeField] private ChallengeBlock challengeBlockPrefab;
        [SerializeField] private Button backButton;
        
        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();

        private readonly List<ChallengeBlock> _challengeBlocks = new();
        private readonly List<RecordBlock> _recordBlocks = new();
        private bool _isDataLoaded;

        public override void Initialize()
        {
            base.Initialize();
            backButton.onClick.AddListener(OnBackButtonClicked);
            JsonSaveManager.OnLoadCompleted += OnPlayerDataLoaded;
        }

        private void OnPlayerDataLoaded()
        {
            var records = PlayerDataManager.Instance.PlayerRecordData.runData;
            foreach (var record in records)
            {
                var recordBlock = Instantiate(recordBlockPrefab, recordParent);
                _recordBlocks.Add(recordBlock);
                recordBlock.SetData(record);
            }
            recordDivider.SetAsLastSibling();
            var challenges = PlayerDataManager.Instance.ChallengeDictionary.Values.ToList();
            foreach (var challenge in challenges)
            {
                var challengeBlock = Instantiate(challengeBlockPrefab, challengeParent);
                _challengeBlocks.Add(challengeBlock);
                challengeBlock.SetData(challenge);
            }
            _isDataLoaded = true;
        }
        
        private void OnDestroy()
        {
            JsonSaveManager.OnLoadCompleted -= OnPlayerDataLoaded;
        }

        public override void Show()
        {
            base.Show();
            if (!_isDataLoaded) OnPlayerDataLoaded();
            SetPlayerInfo();
            SetRecords();
            SetChallenges();
        }
        
        private void OnBackButtonClicked()
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, mainMenuCrossFadeRule.nextPanel, mainMenuCrossFadeRule.crossFadeSettings, 
                transitionCts.Token).Forget();
        }

        private void SetPlayerInfo()
        {
            var recordData = PlayerDataManager.Instance.PlayerRecordData;
            highscoreText.text = recordData.highScore.score.ToString("N0");
            mostFitText.text = recordData.mostFitMe.fitMe.ToString("N0");
        }

        private void SetChallenges()
        {
            var challenges = PlayerDataManager.Instance.ChallengeDictionary.Values.ToList();
            for (var i = 0; i < _challengeBlocks.Count; i++)
            {
                if (i >= challenges.Count)
                {
                    Debug.LogWarning($"Not enough challenges to fill the challenge blocks. " +
                                     $"Total challenges: {challenges.Count}, Total blocks: {_challengeBlocks.Count}");
                    break;
                }
                _challengeBlocks[i].SetData(challenges[i]);
            }
        }
        
        private void SetRecords()
        {
            var records = PlayerDataManager.Instance.PlayerRecordData.runData;
            for (var i = 0; i < _recordBlocks.Count; i++)
            {
                if (i >= records.Count)
                {
                    Debug.LogWarning($"Not enough records to fill the record blocks. " +
                                     $"Total records: {records.Count}, Total blocks: {_recordBlocks.Count}");
                    break;
                }
                _recordBlocks[i].SetData(records[i]);
            }
        }
    }
}