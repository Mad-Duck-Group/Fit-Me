using Cysharp.Threading.Tasks;
using Spine;

namespace MadDuck.Scripts.Utils
{
    public static class SpineAnimationUtils
    {
        public static async UniTask ToUniTask(this TrackEntry trackEntry)
        {
            await UniTask.WaitUntil(() => trackEntry.IsComplete);
        }
    }
}