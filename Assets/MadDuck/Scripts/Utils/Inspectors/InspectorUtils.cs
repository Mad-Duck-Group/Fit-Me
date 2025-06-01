using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MadDuck.Scripts.Utils.Inspectors
{
    [Serializable]
    public struct InspectorVoid {}

    [Serializable]
    public struct PercentageMultiplier
    {
        [MinValue(0), MaxValue(1)] public float percentage;
    }
}