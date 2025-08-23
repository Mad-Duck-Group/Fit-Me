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

        public void SetData(IChallenge challenge)
        {
            challengeNameText.text = challenge.ChallengeName;
            challengeDescriptionText.text = challenge.ChallengeDescription;
            var progress = challenge.GetProgress();
            progressText.text = $"{(int)progress.x} / {(int)progress.y}";
            progressSlider.maxValue = progress.y;
            progressSlider.value = progress.x;
            challengeIcon.sprite = challenge.ChallengeIcon;
        }
    }
}