using System.Globalization;
using MadDuck.Scripts.Challenges;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Others
{
    public class ChallengeBlock : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private TMP_Text challengeNameText;
        [SerializeField] private TMP_Text challengeDescriptionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image challengeIcon;
        [SerializeField] private Image completedIconOverlay;
        [SerializeField] private Image completedOverlay;

        public void SetData(IChallenge challenge)
        {
            challengeNameText.text = challenge.ChallengeName;
            challengeDescriptionText.text = challenge.ChallengeDescription;
            var progress = challenge.GetProgress();
            progress.x = Mathf.Clamp(progress.x, progress.x, progress.y);
            var isInt = progress.x % 1 == 0 && progress.y % 1 == 0;
            var format = isInt ? "N0" : "N2";
            progressText.text = $"{progress.x.ToString(format)} / {progress.y.ToString(format)}";
            progressSlider.maxValue = progress.y;
            progressSlider.value = progress.x;
            challengeIcon.sprite = challenge.ChallengeIcon;
            completedIconOverlay.gameObject.SetActive(challenge.Completed);
            completedOverlay.gameObject.SetActive(challenge.Completed);
        }
    }
}