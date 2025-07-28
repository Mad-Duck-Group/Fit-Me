using System;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Units;
using MadDuck.Scripts.Utils.Inspectors;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace MadDuck.Scripts.UIs.Others
{
    public class MainMenuBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Title("References")] 
        [SerializeField] private Transform destination;
        [SerializeField] private BlockPreset blockPreset;
        [SerializeField] private BlockView blockView;
        [SerializeField] private SpriteRenderer speechBubble;
        [SerializeField] private SpriteRenderer glow;
        
        [Title("Settings")]
        [SerializeField] private float placementDistance = 1.5f;
        [SortingLayer, SerializeField] private int originalSortingLayer;
        [SortingLayer, SerializeField] private int pickUpSortingLayer;

        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> speechBubbleAlphaTweenSettings;
        
        [Title("Audios")]
        [SerializeField] private EventReference placeSuccessSfx;
        [SerializeField] private EventReference placeFailSfx;
        
        [Title("Debug")]
        [ShowInInspector, ReadOnly] private bool _isPlaced;
        [ShowInInspector, ReadOnly] private bool _isDragging;
        
        private Vector3 _mousePositionDifference;
        private Vector3 _originalPosition;
        private Sequence _speechBubbleSequence;
        private Sequence _positionSequence;

        private void Awake()
        {
            _originalPosition = transform.position;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left) return;
            if (_isPlaced) return;
            var position = transform.position;
            var mousePosition = PointerManager.Instance.MouseWorldPosition;
            _mousePositionDifference = new Vector3(mousePosition.x - position.x,
                mousePosition.y - position.y, 0);
            AudioManager.Instance.PlayAudioOneShot(blockPreset.PickupSfx, transform.position);
            blockView.SetSortingLayer(pickUpSortingLayer);
            FadeOutBubble();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left) return;
            if (_isPlaced) return;
            var mousePosition = PointerManager.Instance.MouseWorldPosition;
            transform.position = mousePosition - _mousePositionDifference;
            if (_isDragging) return; //Prevent unnecessary calculations
            blockView.PickUp();
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button is not PointerEventData.InputButton.Left) return;
            if (!_isDragging || _isPlaced) return;
            if (Vector3.Distance(transform.position, destination.position) <= placementDistance)
            {
                Place().Forget();
            }
            else
            {
                ReturnToOriginal().Forget();
            }
            _isDragging = false;
        }

        private void FadeOutBubble()
        {
            _speechBubbleSequence.Stop();
            _speechBubbleSequence = Sequence.Create()
                .Group(Tween.Alpha(speechBubble, speechBubbleAlphaTweenSettings))
                .Group(Tween.Alpha(glow, speechBubbleAlphaTweenSettings))
                .OnComplete(() =>
                {
                    speechBubble.gameObject.SetActive(false);
                    glow.gameObject.SetActive(false);
                });
        }
        
        private void FadeInBubble()
        {
            _speechBubbleSequence.Stop();
            speechBubble.gameObject.SetActive(true);
            glow.gameObject.SetActive(true);
            _speechBubbleSequence = Sequence.Create()
                .Group(Tween.Alpha(speechBubble, speechBubbleAlphaTweenSettings.WithDirection(false)))
                .Group(Tween.Alpha(glow, speechBubbleAlphaTweenSettings.WithDirection(false)));
        }

        private async UniTaskVoid Place()
        {
            _positionSequence.Complete();
            _positionSequence = Sequence.Create()
                .Group(Tween.Position(transform, destination.position, 0.1f));
            await _positionSequence.ToUniTask();
            blockView.Place();
            blockView.SetSortingLayer(originalSortingLayer);
            _isPlaced = true;
            AudioManager.Instance.PlayAudioOneShot(placeSuccessSfx, transform.position);
            LoadSceneManager.Instance.LoadScene(SceneType.Gameplay, LoadSceneMode.Single, false).Forget();
        }
        
        private async UniTaskVoid ReturnToOriginal()
        {
            _positionSequence.Complete();
            _positionSequence = Sequence.Create()
                .Group(Tween.Position(transform, _originalPosition, 0.2f));
            await _positionSequence.ToUniTask();
            blockView.Place();
            blockView.SetSortingLayer(originalSortingLayer);
            AudioManager.Instance.PlayAudioOneShot(placeFailSfx, transform.position);
            FadeInBubble();
            _isPlaced = false;
        }
    }
}