
using KSP.UI.Screens.Flight;

namespace BasicDeltaV
{
    public class BasicDeltaV_TWRGauge : GeeGauge
    {
        private void LateUpdate()
        {
            if (!FlightGlobals.ready)
                return;

            if (BasicDeltaV.Instance == null)
                return;

            double twr = BasicDeltaV.Instance.ActiveStageTWR;

            twr *= 10;

            if (twr < 1)
                twr = 1;

            gauge.SetValue(twr);
        }
    }
}
