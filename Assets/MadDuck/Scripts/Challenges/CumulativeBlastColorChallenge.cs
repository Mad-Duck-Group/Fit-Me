using System;
using MadDuck.Scripts.Units;
using UnityEngine;

namespace MadDuck.Scripts.Challenges
{
    
    public struct CumulativeBlastColorChallengeData
    {
        public readonly BlockTypes blockType;
        public readonly uint cumulativeBlastCount;

        public CumulativeBlastColorChallengeData(BlockTypes blockType, uint cumulativeBlastCount)
        {
            this.blockType = blockType;
            this.cumulativeBlastCount = cumulativeBlastCount;
        }
    }
    
    [Serializable]
    public class CumulativeBlastColorChallenge : Challenge<CumulativeBlastColorChallengeData>
    {
        [SerializeField] private BlockTypes targetBlockType = BlockTypes.Red;
        [SerializeField] private uint targetBlastCount = 10;
        
        public override void OnChallengeUpdate(ChallengeUpdateEvent<CumulativeBlastColorChallengeData> challengeUpdateEvent)
        {
            ChallengeData = challengeUpdateEvent.challengeData;
            if (Completed || ChallengeData.blockType != targetBlockType ||
                ChallengeData.cumulativeBlastCount < targetBlastCount)
            {
                SaveChallengeData();
                return;
            }
            Completed = true;
            Complete();
        }
    }
}