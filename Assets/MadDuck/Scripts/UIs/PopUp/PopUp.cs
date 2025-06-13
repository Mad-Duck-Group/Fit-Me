using System;
using MadDuck.Scripts.Managers;
using MessagePipe;
using PrimeTween;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.PopUp
{
    public struct PopUpData
    {
        public readonly string title;
        public readonly Sprite backgroundSprite;
        public Color? backgroundColor;
        public readonly bool allowClose;
        public readonly PopUpChoiceData[] choices;
        
        public PopUpData(string title, bool allowClose, PopUpChoiceData[] choices, Sprite backgroundSprite = null, Color? backgroundColor = null)
        {
            this.title = title;
            this.backgroundSprite = backgroundSprite;
            this.backgroundColor = backgroundColor;
            this.allowClose = allowClose;
            this.choices = choices;
        }
    }
    
    public class PopUp : MonoBehaviour
    {
        [Title("Pop Up References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private PopUpChoice popUpChoicePrefab;
        [SerializeField] private RectTransform choicesContainer;
        [SerializeField] private Button closeButton;

        [Title("Pop Up Debug")] [SerializeField, ReadOnly]
        private SerializableDictionary<int, PopUpChoice> popUpChoices = new();

        public void Initialize(Guid guid, PopUpData popUpData, Action<PopUpResultEvent> onPopUpResult)
        {
            titleText.text = popUpData.title;
            if (popUpData.backgroundSprite) background.sprite = popUpData.backgroundSprite;
            if (popUpData.backgroundColor.HasValue) background.color = popUpData.backgroundColor.Value;

            if (popUpData.allowClose)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.onClick.AddListener(() =>
                {
                    onPopUpResult?.Invoke(new PopUpResultEvent(guid, PopUpResult.Cancel, null));
                    Hide();
                });
            }
            else
            {
                closeButton.gameObject.SetActive(false);
            }
            
            foreach (var choiceData in popUpData.choices)
            {
                PopUpChoice choice = Instantiate(popUpChoicePrefab, choicesContainer);
                choice.Initialize(choiceData, OnChoiceSelected);
                popUpChoices.Add(choiceData.id, choice);
            }
            Show();
            
            void OnChoiceSelected(int id)
            {
                onPopUpResult?.Invoke(new PopUpResultEvent(guid, PopUpResult.Success, id));
                Hide();
            }
        }

        public void Show()
        {
            canvasGroup.transform.localScale = Vector3.zero;
            canvasGroup.gameObject.SetActive(true);
            Tween.Scale(canvasGroup.transform, Vector3.one, 0.3f);
        }
        
        public void Hide()
        {
            Tween.Scale(canvasGroup.transform, Vector3.zero, 0.3f).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
    }
}