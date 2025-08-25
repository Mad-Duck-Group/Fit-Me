using System;
using MadDuck.Scripts.Managers;
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
        public override Vector2 GetProgress()
        {
            return new Vector2((int)PlayerDataManager.Instance.GameData.cumulativeBlockDestroyed, (int)targetBlast);
        }

        public override void OnChallengeUpdate(ChallengeUpdateEvent<CumulativeBlastChallengeData> challengeUpdateEvent)
        {
            if (Completed) return;
            ChallengeData = challengeUpdateEvent.challengeData;
            if (challengeUpdateEvent.challengeData.cumulativeBlast < targetBlast)
            {
                SaveChallengeData();
                return;
            }
            Complete();
        }
    }
}