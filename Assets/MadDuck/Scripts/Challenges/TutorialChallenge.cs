using System;
using UnityEngine;

namespace MadDuck.Scripts.Challenges
{
    [Serializable]
    public struct TutorialChallengeData
    {
    }

    [Serializable]
    public class TutorialChallenge : Challenge<TutorialChallengeData>
    {
        public override Vector2 GetProgress()
        {
            return Completed ? new Vector2(1, 1) : new Vector2(0, 1);
        }
        public override void OnChallengeUpdate(ChallengeUpdateEvent<TutorialChallengeData> challengeUpdateEvent)
        {
            if (Completed)
            {
                return;
            }
            ChallengeData = challengeUpdateEvent.challengeData;
            Complete();
        }
    }
}