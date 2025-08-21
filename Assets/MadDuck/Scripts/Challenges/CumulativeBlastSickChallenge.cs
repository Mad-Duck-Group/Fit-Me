using System;
using UnityEngine;

namespace MadDuck.Scripts.Challenges
{
    public struct CumulativeBlastSickChallengeData
    {
        public readonly uint cumulativeSickBlast;
        
        public CumulativeBlastSickChallengeData(uint cumulativeSickBlast)
        {
            this.cumulativeSickBlast = cumulativeSickBlast;
        }
    }
    
    [Serializable]
    public class CumulativeBlastSickChallenge : Challenge<CumulativeBlastSickChallengeData>
    {
        [SerializeField] private uint targetSickBlast = 10;
        public override void OnChallengeUpdate(ChallengeUpdateEvent<CumulativeBlastSickChallengeData> challengeUpdateEvent)
        {
            ChallengeData = challengeUpdateEvent.challengeData;
            if (Completed || challengeUpdateEvent.challengeData.cumulativeSickBlast < targetSickBlast)
            {
                SaveChallengeData();
                return;
            }
            Completed = true;
            Complete();
        }
    }
}