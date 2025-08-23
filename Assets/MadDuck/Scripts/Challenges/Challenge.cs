using System;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.Utils.Inspectors;
using MessagePipe;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MadDuck.Scripts.Challenges
{

    public struct ChallengeUpdateEvent<T>
    {
        public readonly T challengeData;
        public ChallengeUpdateEvent(T challengeData)
        {
            this.challengeData = challengeData;
        }
    }
    
    public interface IChallenge : IDisposable
    {
        public Guid ChallengeGuid { get; set; }
        public bool Completed { get; }
        public string ChallengeName { get; }
        public string ChallengeDescription { get; }
        public Sprite ChallengeIcon { get; }
        public Vector2 GetProgress();
        public void Initialize();
        public void Complete();
        public void SetChallengeData(ISavable savable);
        public IChallenge Clone();
    }

    public interface ISavable : IJTokenDeserializer{}

    public record SavableChallengeData : ISavable
    {
        public bool completed;
        
        public SavableChallengeData() { } // Parameterless constructor for deserialization
        
        public SavableChallengeData(bool completed)
        {
            this.completed = completed;
        }

        public virtual void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(completed), out completed);
        }
    }
    public record SavableChallengeData<T> : SavableChallengeData
    {
        public T challengeData;
        public SavableChallengeData(bool completed, T challengeData) : base(completed)
        {
            this.completed = completed;
            this.challengeData = challengeData;
        }
        
        public override void DeserializeJToken(JToken jToken)
        {
            jToken.TryGetAndConvertTo(nameof(challengeData), out challengeData);
        }
    }

    [Serializable]
    public abstract class Challenge<T> : IChallenge
    {
        [HideLabel, ShowInInspector] 
        [ShowIf("@this.ChallengeGuid == default(Guid)")]
        [InfoBox("Please generate a GUID for this challenge. It will be used to save and load the challenge data.", InfoMessageType.Error)]
        private InspectorVoid _challengeGuidWarning;
        
        [field: SerializeField] public bool Completed { get; protected set; }
        [field: SerializeField] public string ChallengeName { get; protected set; }
        [field: SerializeField, TextArea] public string ChallengeDescription { get; protected set; }
        [field: SerializeField] public Sprite ChallengeIcon { get; protected set; }

        [field: SerializeField] private bool saveChallengeData;
        
        [field: ShowIf(nameof(saveChallengeData))]
        [field: SerializeField, ReadOnly] protected T ChallengeData { get; set; }
        [Button("Test Complete Challenge")]
        [HideInEditorMode]
        private void TestCompleteChallenge()
        {
            Complete();
        }
        
        [OdinSerialize] public Guid ChallengeGuid { get; set; } = Guid.NewGuid();
        protected IDisposable challengeUpdateSubscription;
        protected IPublisher<NotificationDisplayEvent> notificationPublisher;

        public abstract Vector2 GetProgress();

        public virtual void Initialize()
        {
            challengeUpdateSubscription = GlobalMessagePipe.GetSubscriber<ChallengeUpdateEvent<T>>()
                .Subscribe(OnChallengeUpdate);
            notificationPublisher = GlobalMessagePipe.GetPublisher<NotificationDisplayEvent>();
        }
        
        public virtual void Dispose()
        {
            challengeUpdateSubscription?.Dispose();
        }

        public abstract void OnChallengeUpdate(ChallengeUpdateEvent<T> challengeUpdateEvent);

        public virtual void SaveChallengeData()
        {
            var savable = saveChallengeData ? new SavableChallengeData<T>(Completed, ChallengeData) : new SavableChallengeData(Completed);
            PlayerDataManager.Instance.SaveChallenges(ChallengeGuid, savable);
        }
        
        public virtual void Complete()
        {
            notificationPublisher.Publish(new NotificationDisplayEvent(NotificationType.Challenge, ChallengeDescription, ChallengeIcon));
            SaveChallengeData();
        }

        public void SetChallengeData(ISavable savable)
        {
            if (savable == null) return;
            if (saveChallengeData)
            {
                if (savable is not SavableChallengeData<T> challengeData)
                {
                    Debug.LogError($"Invalid challenge data type for challenge {ChallengeName}({ChallengeGuid}). " +
                                   $"Expected {typeof(SavableChallengeData<T>)} but got {savable.GetType()}");
                    return;
                }

                Completed = challengeData.completed;
                ChallengeData = challengeData.challengeData;
            }
            else
            {
                if (savable is not SavableChallengeData challengeData)
                {
                    Debug.LogError($"Invalid challenge data type for challenge {ChallengeName}({ChallengeGuid}). " +
                                   $"Expected {typeof(SavableChallengeData)} but got {savable.GetType()}");
                    return;
                }

                Completed = challengeData.completed;
                ChallengeData = default; // No specific data to set when not saving challenge data
            }
        }

        public IChallenge Clone()
        {
            var clone = (Challenge<T>)MemberwiseClone();
            return clone;
        }
    }
}