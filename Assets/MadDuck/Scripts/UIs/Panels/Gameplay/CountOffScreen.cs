using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Panels.Gameplay
{
    [ShowOdinSerializedPropertiesInInspector]
    public class CountOffScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private TMP_Text countOffText;

        [Title("Settings")] 
        [SerializeField] private bool useCountOff = true;
        [SerializeField] private float startDelay = 1f;
        [SerializeField] private float warmUp = 0.25f;
        [SerializeField] private float readyFitInterval = 2f;
        [SerializeField] private float exitDelay = 1f;

        [Title("Audios")] 
        [SerializeField] private EventReference readySfx;
        [SerializeField] private EventReference fitSfx;
        
        [Title("Panels")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule gameplayCrossFadeRule = new();

        public override void OnPanelReady()
        {
            base.OnPanelReady();
            if (!useCountOff)
            {
                CountOffComplete().Forget();
                return;
            }
            StartCountOff().Forget();
        }

        private async UniTaskVoid StartCountOff()
        {
            countOffText.text = string.Empty;
            await UniTask.WaitForSeconds(startDelay);
            AudioManager.Instance.PlayAudioOneShot(readySfx, transform.position);
            countOffText.text = "Ready?";
            await UniTask.WaitForSeconds(readyFitInterval - warmUp);
            AudioManager.Instance.PlayAudioOneShot(fitSfx, transform.position);
            await UniTask.WaitForSeconds(warmUp);
            countOffText.text = "FIT!";
            await UniTask.WaitForSeconds(exitDelay);
            CountOffComplete().Forget();
        }

        private async UniTaskVoid CountOffComplete()
        {
            transitionCts = new CancellationTokenSource();
            await PanelController.ChangePanel(this, gameplayCrossFadeRule.nextPanel, gameplayCrossFadeRule.crossFadeSettings, transitionCts.Token);
            GameManager.Instance.GameStart();
        }
    }
}