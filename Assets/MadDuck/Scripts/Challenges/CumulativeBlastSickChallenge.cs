using System;
using MadDuck.Scripts.Managers;
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
        
        public override Vector2 GetProgress()
        {
            return new Vector2((int)PlayerDataManager.Instance.GameData.cumulativePreInfectBlockDestroyed, (int)targetSickBlast);
        }
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