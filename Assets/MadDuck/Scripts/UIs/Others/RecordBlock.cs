using System;
using System.Globalization;
using MadDuck.Scripts.Managers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Others
{
    public class RecordBlock : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text fitMeText;
        
        public void SetData(PlayerRecordData.RunData runData)
        {
            //var currentUICulture = CultureInfo.CurrentUICulture;
            dateText.text = runData.dateTime.ToString("G", CultureInfo.InvariantCulture);
            scoreText.text = runData.score.ToString("N0");
            fitMeText.text = runData.fitMe.ToString("N0");
        }
    }
}