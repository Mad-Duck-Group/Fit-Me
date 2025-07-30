using FMODUnity;
using MadDuck.Scripts.Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Others
{
    [RequireComponent(typeof(Selectable))]
    public class ButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Title("Audios")] 
        [SerializeField] private EventReference clickSfx;
        
        private Selectable _selectable;
        
        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            
        }

        public void OnPointerUp(PointerEventData eventData)
        {
           
        }

        public void OnPointerClick(PointerEventData eventData)
        {
             if (!_selectable.interactable) return;
             AudioManager.Instance.PlayAudioOneShot(clickSfx, transform.position);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
           
        }

        public void OnPointerExit(PointerEventData eventData)
        {
          
        }
    }
}
