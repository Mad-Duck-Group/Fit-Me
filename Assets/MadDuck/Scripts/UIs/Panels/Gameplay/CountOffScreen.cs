using System;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Panels.Gameplay
{
    public class CountOffScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private TMP_Text countOffText;

        [Title("Settings")] 
        [SerializeField] private bool useCountOff = true;
        [SerializeField] private float warmUp = 0.25f;
        [SerializeField] private float readyFitInterval = 2f;
        [SerializeField] private float exitDelay = 1f;

        [Title("Audios")] 
        [SerializeField] private EventReference readySfx;
        [SerializeField] private EventReference fitSfx;

        public Action OnCountOffComplete { private get; set; } = null;

        public override void OnPanelReady()
        {
            base.OnPanelReady();
            if (!useCountOff)
            {
                OnCountOffComplete?.Invoke();
                return;
            }
            StartCountOff().Forget();
        }

        private async UniTaskVoid StartCountOff()
        {
            AudioManager.Instance.PlayAudioOneShot(readySfx, transform.position);
            countOffText.text = "Ready?";
            await UniTask.WaitForSeconds(readyFitInterval - warmUp);
            AudioManager.Instance.PlayAudioOneShot(fitSfx, transform.position);
            await UniTask.WaitForSeconds(warmUp);
            countOffText.text = "FIT!";
            await UniTask.WaitForSeconds(exitDelay);
            OnCountOffComplete?.Invoke();
        }
    }
}