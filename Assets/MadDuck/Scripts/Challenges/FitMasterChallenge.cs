using System;

namespace MadDuck.Scripts.Challenges
{
    [Serializable]
    public struct FitMasterChallengeData
    {
    }
    
    [Serializable]
    public class FitMasterChallenge : Challenge<FitMasterChallengeData>
    {
        public override void OnChallengeUpdate(ChallengeUpdateEvent<FitMasterChallengeData> challengeUpdateEvent)
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