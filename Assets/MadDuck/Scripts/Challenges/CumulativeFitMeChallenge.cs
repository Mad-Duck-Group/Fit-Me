using System;
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