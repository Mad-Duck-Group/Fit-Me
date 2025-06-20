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

    public static class InspectorSettings
    {
        private static UIEditors.MadduckInspectorSettings instance;
        public const string GameDesignerModeKey = "@InspectorSettings.GameDesignerMode";

        public static bool GameDesignerMode
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<UIEditors.MadduckInspectorSettings>("UIEditors/MadduckInspectorSettings");
                }

                return instance != null && instance.gameDesignerMode;
            }
        }
    }
}