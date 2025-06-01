using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;

namespace MadDuck.Scripts.Utils
{
    public static class PrimeTweenUtils
    {
        public static Sequence ApplyDirection(this Sequence seq, bool isForwardDirection) 
        {
            Assert.IsTrue(seq.isAlive);
            Assert.AreNotEqual(0f, seq.durationTotal);
            Assert.AreNotEqual(-1, seq.cyclesTotal);
            seq.timeScale = Mathf.Abs(seq.timeScale) * (isForwardDirection ? 1f : -1f);
            if (isForwardDirection) 
            {
                if (seq.progressTotal >= 1f) 
                {
                    seq.progressTotal = 0f;
                }
            } 
            else 
            {
                if (seq.progressTotal == 0f) 
                {
                    seq.progressTotal = 1f;
                }
            }
            return seq;
        }
    }
}