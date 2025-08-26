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
    
    [Serializable]
    public struct NotificationDisplayEvent
    {
        public NotificationType notificationType;
        [OdinSerialize] public INotificationData data;

        public NotificationDisplayEvent(NotificationType notificationType, INotificationData data)
        {
            this.notificationType = notificationType;
            this.data = data;
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
        [OdinSerialize] public INotificationView notificationViewPrefab;
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
            var view = prefabData.notificationViewPrefab.Instantiate(transform, prefabData.initialPosition);
            view.Initialize();
            view.SetData(notificationEvent.data);
            AudioManager.Instance.PlayAudioOneShot(prefabData.soundEffect, transform.position);
            await view.Show();
            await UniTask.WhenAll(UniTask.WaitForSeconds(notificationStayDuration),
                view.PlayAnimation());
            await view.Hide();
            view.Destroy();
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
