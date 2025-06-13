using System;
using MadDuck.Scripts.Frameworks.MessagePipe;
using MadDuck.Scripts.UIs;
using MadDuck.Scripts.UIs.PopUp;
using MessagePipe;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadDuck.Scripts.Managers
{
    public enum PopUpResult
    {
        Cancel,
        Success
    }

    public struct PopUpResultEvent : IGuidIdentifier
    {
        public Guid Id { get; }
        public readonly PopUpResult result;
        public int? choiceId;
        
        public PopUpResultEvent(Guid id, PopUpResult result, int? choiceId)
        {
            Id = id;
            this.result = result;
            this.choiceId = choiceId;
        }
    }
    public class PopUpManager : PersistentMonoSingleton<PopUpManager>
    {
        [Title("Pop Up References")]
        [SerializeField] private PopUp popUpPrefab;

        [Title("Pop Up Settings")] 
        [SerializeField] private int maxPopUps = 3;
        [SerializeField] private int maxPopUpChoices = 4;
        
        [Title("Pop Up Debug")]
        [SerializeField, ReadOnly] private SerializableDictionary<Guid, PopUp> activePopUps = new();
        
        private IPublisher<PopUpResultEvent> _popUpResultPublisher;
        
        protected override void Awake()
        {
            base.Awake();
            _popUpResultPublisher = GlobalMessagePipe.GetPublisher<PopUpResultEvent>();
        }

        public bool TryCreatePopUp(PopUpData popUpData, out Guid guid)
        {
            guid = Guid.Empty;
            if (popUpData.choices.Length > maxPopUpChoices)
            {
                Debug.LogWarning($"Pop Up has more than {maxPopUpChoices} choices, aborting creation.");
                return false;
            }
            if (activePopUps.Count >= maxPopUps)
            {
                Debug.LogWarning($"Maximum number of pop-ups ({maxPopUps}) reached, cannot create more.");
                return false;
            }
            var popUp = Instantiate(popUpPrefab, transform);
            guid = Guid.NewGuid();
            popUp.Initialize(guid, popUpData, OnPopUpResult);
            activePopUps.Add(guid, popUp);
            return true;
        }
        
        private void OnPopUpResult(PopUpResultEvent result)
        {
            _popUpResultPublisher.Publish(result);
            if (activePopUps.TryGetValue(result.Id, out _))
            {
                activePopUps.Remove(result.Id);
            }
            else
            {
                Debug.LogWarning($"Pop Up with ID {result.Id} not found.");
            }
        }
    }
}