using System;
using MadDuck.Scripts.Managers;
using UnityEngine;

namespace MadDuck.Scripts.Challenges
{
    [Serializable]
    public struct CumulativeScoreChallengeData
    {
        public readonly uint cumulativeScore;

        public CumulativeScoreChallengeData(uint cumulativeScore)
        {
            this.cumulativeScore = cumulativeScore;
        }
    }

    [Serializable]
    public class CumulativeScoreChallenge : Challenge<CumulativeScoreChallengeData>
    {
        [field: SerializeField] private uint targetScore = 1000;
        
        public override Vector2 GetProgress()
        {
            return new Vector2((int)PlayerDataManager.Instance.PlayerRecordData.cumulativeScore, (int)targetScore);
        }

        public override void OnChallengeUpdate(ChallengeUpdateEvent<CumulativeScoreChallengeData> challengeUpdateEvent)
        {
            ChallengeData = challengeUpdateEvent.challengeData;
            if (Completed || challengeUpdateEvent.challengeData.cumulativeScore < targetScore)
            {
                SaveChallengeData();
                return;
            }
            Completed = true;
            Complete();
        }
    }
}