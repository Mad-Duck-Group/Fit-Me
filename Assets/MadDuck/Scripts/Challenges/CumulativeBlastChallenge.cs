using System;
using UnityEngine;

namespace MadDuck.Scripts.Challenges
{
    public struct CumulativeBlastChallengeData
    {
        public readonly uint cumulativeBlast;
        
        public CumulativeBlastChallengeData(uint cumulativeBlast)
        {
            this.cumulativeBlast = cumulativeBlast;
        }
    }
    
    [Serializable]
    public class CumulativeBlastChallenge : Challenge<CumulativeBlastChallengeData>
    {
        [SerializeField] private uint targetBlast = 10;
        public override void OnChallengeUpdate(ChallengeUpdateEvent<CumulativeBlastChallengeData> challengeUpdateEvent)
        {
            ChallengeData = challengeUpdateEvent.challengeData;
            if (Completed || challengeUpdateEvent.challengeData.cumulativeBlast < targetBlast)
            {
                SaveChallengeData();
                return;
            }
            Completed = true;
            Complete();
        }
    }
}