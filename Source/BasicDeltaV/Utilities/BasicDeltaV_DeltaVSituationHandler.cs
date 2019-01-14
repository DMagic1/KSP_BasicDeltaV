using UnityEngine;
using UnityEngine.Events;

namespace BasicDeltaV
{
    public class BasicDeltaV_DeltaVSituationHandler : MonoBehaviour
    {
        public class DeltaVAppSituationAwake: UnityEvent<DeltaVAppSituation> { }
        public class DeltaVAppSituationStart : UnityEvent<DeltaVAppSituation> { }
        public class DeltaVAppSituationDestroy : UnityEvent<DeltaVAppSituation> { }

        public static DeltaVAppSituationAwake OnDVAppSituationAwake = new DeltaVAppSituationAwake();
        public static DeltaVAppSituationStart OnDVAppSituationStart = new DeltaVAppSituationStart();
        public static DeltaVAppSituationDestroy OnDVAppSituationDestroy = new DeltaVAppSituationDestroy();

        private DeltaVAppSituation dvApp;

        private void Awake()
        {
            dvApp = GetComponent<DeltaVAppSituation>();

            if (dvApp != null)
                OnDVAppSituationAwake.Invoke(dvApp);
        }

        private void Start()
        {
            if (dvApp != null)
                OnDVAppSituationStart.Invoke(dvApp);
        }

        private void OnDestroy()
        {
            if (dvApp != null)
                OnDVAppSituationDestroy.Invoke(dvApp);
        }
    }
}
