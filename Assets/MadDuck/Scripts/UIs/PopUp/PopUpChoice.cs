using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.PopUp
{
    public struct PopUpChoiceData
    {
        public readonly string choiceText;
        public readonly int id;
        public readonly Sprite backgroundSprite;
        public Color? backgroundColor;
        
        public PopUpChoiceData(string choiceText, int id, Sprite backgroundSprite = null, Color? backgroundColor = null)
        {
            this.choiceText = choiceText;
            this.id = id;
            this.backgroundSprite = backgroundSprite;
            this.backgroundColor = backgroundColor;
        }
    }
    public class PopUpChoice : MonoBehaviour
    {
        [Title("Pop Up Choice References")]
        [SerializeField] private TMP_Text choiceText;
        [SerializeField] private Image background;
        [SerializeField] private Button button;

        public void Initialize(PopUpChoiceData choiceData, Action<int> onChoiceSelected)
        {
            choiceText.text = choiceData.choiceText;
            if (choiceData.backgroundSprite) background.sprite = choiceData.backgroundSprite;
            if (choiceData.backgroundColor.HasValue) background.color = choiceData.backgroundColor.Value;

            button.onClick.AddListener(() => onChoiceSelected?.Invoke(choiceData.id));
        }
    }
}