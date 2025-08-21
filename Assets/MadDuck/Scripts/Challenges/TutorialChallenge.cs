using System;

namespace MadDuck.Scripts.Challenges
{
    [Serializable]
    public struct TutorialChallengeData
    {
    }

    [Serializable]
    public class TutorialChallenge : Challenge<TutorialChallengeData>
    {
        public override void OnChallengeUpdate(ChallengeUpdateEvent<TutorialChallengeData> challengeUpdateEvent)
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