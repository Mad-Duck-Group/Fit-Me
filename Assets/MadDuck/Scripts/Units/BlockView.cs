using System;
using System.Collections.Generic;
using MadDuck.Scripts.Managers;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.Units
{
    public class BlockView : MonoBehaviour
    {
        #region Inspectors
        [Title("Settings")]
        [SerializeField] private int idleVariantCount = 2;
        [SerializeField] private Color originalColor = Color.white;
        [SerializeField] private float pickUpScaleMultiplier = 1.2f;
        #endregion

        #region Fields and Properties
        private Animator _animator;
        private List<SpriteRenderer> _spriteRenderers = new();
        private Vector3 _originalScale;
        private Tween _pickUpTween;
        private static readonly int IsPickedUp = Animator.StringToHash("IsPickedUp");
        private static readonly int IdleIndex = Animator.StringToHash("IdleIndex");
        #endregion
        
        #region Initalization
        private void Awake()
        {
            _originalScale = transform.localScale;
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("Animator not found in BlockView!");
            }
            _spriteRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
            if (_spriteRenderers.Count == 0)
            {
                Debug.LogError("No SpriteRenderer found in BlockView!");
            }
        }
        #endregion

        #region Utils
        public void PickUp()
        {
            _pickUpTween = Tween.Scale(transform, _originalScale * pickUpScaleMultiplier, 0.2f);
            _animator.SetBool(IsPickedUp, true);
        }
        
        public void Place()
        {
            _pickUpTween.Stop();
            _pickUpTween = Tween.Scale(transform, _originalScale, 0.2f);
            _animator.SetBool(IsPickedUp, false);
            _animator.SetInteger(IdleIndex, UnityEngine.Random.Range(0, idleVariantCount));
        }

        public void SetSortingLayer(int layer)
        {
            _spriteRenderers.ForEach(x => x.sortingLayerID = layer);
        }
        
        public void SetSortingOrder(int order)
        {
            _spriteRenderers.ForEach(x => x.sortingOrder = order);
        }

        public void ChangeSortingOrder(int change)
        {
            _spriteRenderers.ForEach(x => x.sortingOrder += change);
        }

        public void SetColor(Color color)
        {
            _spriteRenderers.ForEach(x => x.color = color);
        }
        #endregion
    }
}
