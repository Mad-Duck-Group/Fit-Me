using System;

namespace MadDuck.Scripts.Challenges
{
    [Serializable]
    public struct FailChallengeData
    {
    }
    
    [Serializable]
    public class FailChallenge : Challenge<FailChallengeData>
    {
        public override void OnChallengeUpdate(ChallengeUpdateEvent<FailChallengeData> challengeUpdateEvent)
        {
            if (Completed)
            {
                SaveChallengeData();
                return;
            }
            ChallengeData = challengeUpdateEvent.challengeData;
            Completed = true;
            Complete();
        }
    }
}