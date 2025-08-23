using System;
using MadDuck.Scripts.Managers;
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
        
        public override Vector2 GetProgress()
        {
            var gameData = PlayerDataManager.Instance.GameData;
            return gameData.cumulativeColorBlastDictionary.TryGetValue(targetBlockType, out var count) 
                ? new Vector2((int)count, (int)targetBlastCount) 
                : new Vector2(0, (int)targetBlastCount);
        }
        
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