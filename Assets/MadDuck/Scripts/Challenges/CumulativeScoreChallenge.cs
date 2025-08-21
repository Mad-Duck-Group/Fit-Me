using System;
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