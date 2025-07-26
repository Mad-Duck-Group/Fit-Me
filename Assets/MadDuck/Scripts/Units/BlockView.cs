using System;
using System.Collections.Generic;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Utils.Inspectors;
using PrimeTween;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;
using Animation = Spine.Animation;

namespace MadDuck.Scripts.Units
{
    [ShowOdinSerializedPropertiesInInspector]
    public class BlockView : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        private record SkinWrapper
        {
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly] private SkeletonRenderer _skeletonRenderer;
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
            public BlockTypes blockType;
            [SpineSkin(dataField: nameof(_skeletonRenderer))] public string skinName;
            
            public SkinWrapper(SkeletonRenderer skeletonRenderer, BlockTypes blockType, string skinName)
            {
                _skeletonRenderer = skeletonRenderer;
                this.blockType = blockType;
                this.skinName = skinName;
            }
        }
        #region Inspectors
        [TitleGroup("References")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        
        [Title("Settings")]
        [SerializeField] private Color originalColor = Color.white;
        [SerializeField] private float pickUpScaleMultiplier = 1.2f;
        
        [Title("Animations")]
        [SerializeField, SpineAnimation] string[] idleAnimations;
        [SerializeField, SpineAnimation] string pickUpAnimation;
        [SerializeField, SpineAnimation] string explodeAnimation;
        
        [TitleGroup("Skins")]
        [ShowInInspector, HideLabel]
        [DetailedInfoBox("Read Me",
            "Due to a certain limitation of Spine handling of the attributes, the skin selector cannot be drawn under dictionary, " +
            "and has to be deconstructed into a list of SkinWrapper objects.\n" +
            "You can deconstruct the dictionary into a list by clicking the 'Deconstruct' button, " +
            "and then save the changes back to the dictionary by clicking the 'Save Changes' button.",
            InfoMessageType.Warning)]
        private InspectorVoid _skinDictionaryInfo;
        [TitleGroup("Skins")]
        [SerializeField] 
        private SerializableDictionary<BlockTypes, string> skinDictionary = new();
        [TitleGroup("Skins")]
        [SerializeField, HideIf("@deconstructed.Count == 0")] private List<SkinWrapper> deconstructed = new();
        [TitleGroup("Skins")]
        [Button("Deconstruct")]
        private void Deconstruct()
        {
            deconstructed = new List<SkinWrapper>();
            foreach (var kvp in skinDictionary)
            {
                deconstructed.Add(new SkinWrapper(skeletonAnimation, kvp.Key, kvp.Value));
            }
        }
        [TitleGroup("Skins")]
        [Button("Save Changes")]
        private void SaveChanges()
        {
            skinDictionary = new SerializableDictionary<BlockTypes, string>();
            foreach (var wrapper in deconstructed)
            {
                skinDictionary.Add(wrapper.blockType, wrapper.skinName);
            }
            deconstructed.Clear();
        }
        #endregion

        #region Fields and Properties
        private MeshRenderer _meshRenderer;
        private Vector3 _originalScale;
        private Tween _pickUpTween;
        #endregion
        
        #region Initalization
        private void Awake()
        {
            if (!skeletonAnimation)
            {
                Debug.LogError("SkeletonAnimation is not assigned in BlockView.");
                return;
            }
            if (!skeletonAnimation.TryGetComponent(out _meshRenderer))
            {
                Debug.LogError("MeshRenderer is not found on SkeletonAnimation.");
                return;
            }
            _originalScale = transform.localScale;
            skeletonAnimation.AnimationState.SetAnimation(0, idleAnimations.GetRandomElement(), true);
        }
        #endregion

        #region Utils
        public void PickUp()
        {
            _pickUpTween = Tween.Scale(transform, _originalScale * pickUpScaleMultiplier, 0.2f);
            skeletonAnimation.AnimationState.SetAnimation(0, pickUpAnimation, true);
        }
        
        public void Place()
        {
            _pickUpTween.Stop();
            _pickUpTween = Tween.Scale(transform, _originalScale, 0.2f);
            skeletonAnimation.AnimationState.SetAnimation(0, idleAnimations.GetRandomElement(), true);
        }
        
        public void Explode()
        {
            skeletonAnimation.AnimationState.SetAnimation(0, explodeAnimation, false);
        }

        public void SetType(BlockTypes type)
        {
            if (!skinDictionary.TryGetValue(type, out var skin))
            {
                Debug.LogWarning($"No skin found for block type: {type}");
                 return;
            }
            skeletonAnimation.Skeleton.SetSkin(skin);
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();
        }

        public void SetSortingLayer(int layer)
        {
            _meshRenderer.sortingLayerID = layer;
        }
        
        public void SetSortingOrder(int order)
        {
            _meshRenderer.sortingOrder = order;
        }

        public void ChangeSortingOrder(int change)
        {
            _meshRenderer.sortingOrder += change;
        }

        public void SetColor(Color color)
        {
            skeletonAnimation.Skeleton.SetColor(color);
        }
        #endregion
        
        #region Serialization
        [SerializeField, HideInInspector]
        private SerializationData serializationData;
        public void OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
        }

        public void OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
        }
        #endregion
    }
}
