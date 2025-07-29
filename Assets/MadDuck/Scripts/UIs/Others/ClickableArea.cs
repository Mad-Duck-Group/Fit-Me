using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MadDuck.Scripts.UIs.Others
{
    public class ClickableArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public event Action OnClicked;
        public event Action OnEntered;
        public event Action OnExited;
        public event Action OnDown;
        public event Action OnUp;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left)
                return;
            OnEntered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left)
                return;
            OnExited?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left)
                return;
            OnClicked?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left)
                return;
            OnDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left)
                return;
            OnUp?.Invoke();
        }
    }
}