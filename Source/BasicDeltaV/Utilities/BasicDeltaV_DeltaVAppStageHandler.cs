using UnityEngine;
using UnityEngine.Events;

namespace BasicDeltaV
{
    public class BasicDeltaV_DeltaVAppStageHandler : MonoBehaviour
    {
        public class DeltaVAppStageStart : UnityEvent<DeltaVAppStageInfo> { }
        public class DeltaVAppStageDestroy : UnityEvent<DeltaVAppStageInfo> { }

        public static DeltaVAppStageStart OnDVAppStageStart = new DeltaVAppStageStart();
        public static DeltaVAppStageDestroy OnDVAppStageDestroy = new DeltaVAppStageDestroy();

        private DeltaVAppStageInfo dvApp;

        private void Start()
        {
            dvApp = GetComponent<DeltaVAppStageInfo>();

            if (dvApp != null)
                OnDVAppStageStart.Invoke(dvApp);
        }

        private void OnDestroy()
        {
            if (dvApp != null)
                OnDVAppStageDestroy.Invoke(dvApp);
        }
    }
}
