using System;
using UnityEngine;

namespace MadDuck.Scripts.Challenges
{
    [Serializable]
    public struct FailChallengeData
    {
    }
    
    [Serializable]
    public class FailChallenge : Challenge<FailChallengeData>
    {
        public override Vector2 GetProgress()
        {
            return Completed ? new Vector2(1, 1) : new Vector2(0, 1);
        }
        
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