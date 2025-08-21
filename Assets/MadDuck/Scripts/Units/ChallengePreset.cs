using System;
using System.Collections.Generic;
using System.Linq;
using MadDuck.Scripts.Challenges;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MadDuck.Scripts.Units
{
    [CreateAssetMenu(fileName = "ChallengePreset", menuName = "MadDuck/Challenge Preset", order = 1)]
    public class ChallengePreset : SerializedScriptableObject
    {
        [Title("Challenges")]
        [OdinSerialize] public List<IChallenge> Challenges { get; private set; } = new();
        [Button("Regenerate All Guid")]
        public void RegenerateAllGuid()
        {
            Challenges.ForEach(c => c.ChallengeGuid = Guid.NewGuid());
        }

        public ChallengePreset Clone()
        {
            var clone = CreateInstance<ChallengePreset>();
            clone.Challenges = new List<IChallenge>(Challenges.Select(x => x.Clone()));
            return clone;
        }
    }
}