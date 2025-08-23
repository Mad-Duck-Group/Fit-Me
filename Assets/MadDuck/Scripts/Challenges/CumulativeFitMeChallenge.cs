using System;
using MadDuck.Scripts.Managers;
using UnityEngine;

namespace MadDuck.Scripts.Challenges
{
    [Serializable]
    public struct CumulativeFitMeChallengeData
    {
        public uint cumulativeFitMe;
        
        public CumulativeFitMeChallengeData(uint cumulativeFitMe)
        {
            this.cumulativeFitMe = cumulativeFitMe;
        }
    }
    
    [Serializable]
    public class CumulativeFitMeChallenge : Challenge<CumulativeFitMeChallengeData>
    {
        [field: SerializeField] private uint targetFitMe = 10;
        
        public override Vector2 GetProgress()
        {
            return new Vector2((int)PlayerDataManager.Instance.PlayerRecordData.cumulativeFitMe, (int)targetFitMe);
        }
        public override void OnChallengeUpdate(ChallengeUpdateEvent<CumulativeFitMeChallengeData> challengeUpdateEvent)
        {
            ChallengeData = challengeUpdateEvent.challengeData;
            if (Completed || challengeUpdateEvent.challengeData.cumulativeFitMe < targetFitMe)
            {
                SaveChallengeData();
                return;
            }
            Completed = true;
            Complete();
        }
    }
}