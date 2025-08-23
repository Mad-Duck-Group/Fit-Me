using System;
using MadDuck.Scripts.Managers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Others
{
    public class ScoreBlock : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text fitMeText;
        
        public void SetData(PlayerRecordData.RunData runData)
        {
            dateText.text = runData.dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            scoreText.text = runData.score.ToString("N0");
            fitMeText.text = runData.fitMe.ToString("N0");
        }
    }
}