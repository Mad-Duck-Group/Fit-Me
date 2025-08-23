using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Frameworks.MessagePipe;
using MadDuck.Scripts.UIs.Notifications;
using MessagePipe;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    public struct NotificationDisplayEvent
    {
        public readonly NotificationType notificationType;
        public readonly string message;
        public readonly Sprite icon;

        public NotificationDisplayEvent(NotificationType notificationType, string message, Sprite icon)
        {
            this.notificationType = notificationType;
            this.message = message;
            this.icon = icon;
        }
    }

    public enum NotificationType
    {
        General,
        Challenge
    }

    [Serializable]
    public struct NotificationPrefabData
    {
        public NotificationView notificationViewPrefab;
        public Vector2 initialPosition;
        [SerializeField] public EventReference soundEffect;
    }
    
    [ShowOdinSerializedPropertiesInInspector]
    public class NotificationManager : PersistentMonoSingleton<NotificationManager>, ISerializationCallbackReceiver, ISupportsPrefabSerialization
    {
        [Title("References")]
        [OdinSerialize] private SerializableDictionary<NotificationType, NotificationPrefabData> notificationPrefabDictionary = new();
        
        [Title("Settings")]
        [SerializeField] private float notificationStayDuration = 2f;
        
        [Title("Debug")]
        [Button("Test Notification")]
        private void TestNotification(NotificationDisplayEvent eventData)
        {
            EnqueueNotification(eventData);
        }
        
        private readonly Queue<NotificationDisplayEvent> _notificationQueue = new();
        private bool _showingNotification;
        private IDisposable _notificationSubscription;

        private void OnEnable()
        {
            MessagePipeLifetimeScope.OnGlobalMessagePipeSet += OnGlobalMessagePipeSet;
            _notificationSubscription = GlobalMessagePipe.GetSubscriber<NotificationDisplayEvent>().Subscribe(EnqueueNotification);
        }

        private void OnGlobalMessagePipeSet()
        {
         
        }

        private void OnDisable()
        {
            MessagePipeLifetimeScope.OnGlobalMessagePipeSet -= OnGlobalMessagePipeSet;
            _notificationSubscription?.Dispose();
        }
        
        private void EnqueueNotification(NotificationDisplayEvent eventData)
        {
            _notificationQueue.Enqueue(eventData);
            if (!_showingNotification)
            {
                ShowNextNotification().Forget();
            }
        }

        private async UniTaskVoid ShowNextNotification()
        {
            _showingNotification = true;
            var notificationEvent = _notificationQueue.Dequeue();
            if (!notificationPrefabDictionary.TryGetValue(notificationEvent.notificationType, out var prefabData))
            {
                Debug.LogWarning($"No notification prefab found for type: {notificationEvent.notificationType}");
                _showingNotification = false;
                return;
            }
            var view = Instantiate(prefabData.notificationViewPrefab, transform);
            ((RectTransform)view.transform).anchoredPosition = prefabData.initialPosition;
            view.Initialize();
            view.SetMessage(notificationEvent.message);
            view.SetIcon(notificationEvent.icon);
            AudioManager.Instance.PlayAudioOneShot(prefabData.soundEffect, view.transform.position);
            await view.Show();
            await UniTask.WhenAll(UniTask.WaitForSeconds(notificationStayDuration),
                view.PlayAnimation());
            await view.Hide();
            Destroy(view.gameObject);
            _showingNotification = false;
            if (_notificationQueue.Count > 0)
            {
                ShowNextNotification().Forget();
            }
        }
        
        #region Serialization
        public void OnBeforeSerialize()
        {
            UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
        }

        public void OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
        }

        [SerializeField, HideInInspector]
        private SerializationData serializationData;
        public SerializationData SerializationData 
        { 
            get => serializationData;
            set => serializationData = value;
        }
        #endregion
    }
}
