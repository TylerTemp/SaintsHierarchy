using System;

namespace SaintsHierarchy.Editor
{
    public class DisableUnityLogScoop: IDisposable
    {
        public DisableUnityLogScoop()
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;
        }

        public void Dispose()
        {
            UnityEngine.Debug.unityLogger.logEnabled = true;
        }
    }
}
