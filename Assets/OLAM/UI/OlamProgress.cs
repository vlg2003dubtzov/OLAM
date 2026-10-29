using System.Collections.Generic;
using UnityEngine;

namespace Olam.UI
{
    /// <summary>
    /// Пространства, пройденные за этот запуск игры, — для отметки в хабе. На диск не пишется:
    /// за компьютером в классе сменяются ученики.
    /// </summary>
    public static class OlamProgress
    {
        static readonly HashSet<string> done = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => done.Clear();

        public static void MarkDone(string spaceScene)
        {
            if (!string.IsNullOrEmpty(spaceScene)) done.Add(spaceScene);
        }

        public static bool IsDone(string spaceScene) => done.Contains(spaceScene);
    }
}
