
using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;

namespace BasicDeltaV
{
    public class BasicDeltaV_SimpleDeltaVGauge : MonoBehaviour
    {
        private bool display;

        private BasicDeltaV_StagePanel panel;

        private StageIconInfoBox infoBar;
        private TooltipController_Text tooltip;

        private bool showText;

        private Tooltip_Text toolText;
        
        public void Initialize(StageIconInfoBox iconInfo, bool disp, BasicDeltaV_StagePanel pan)
        {
            infoBar = iconInfo;
            display = disp;
            panel = pan;

            showText = BasicDeltaV_Settings.Instance.ShowDVText;

            if (!showText)
                infoBar.SetCaption(string.Format("<color=#{0}> ΔV</color>", BasicDeltaV.TextColor));
            
            if (panel.Stage != null)
                infoBar.SetValue((float)panel.Stage.deltaV, 0, (float)panel.Stage.totalDeltaV);
        }
        
        private void Awake()
        {
            tooltip = GetComponent<TooltipController_Text>();
            tooltip.continuousUpdate = true;
        }

        private void Start()
        {
            GameEvents.onTooltipSpawned.Add(TooltipSpawned);
            GameEvents.onTooltipDespawned.Add(TooltipDespawned);
        }

        private void OnDestroy()
        {
            GameEvents.onTooltipSpawned.Remove(TooltipSpawned);
            GameEvents.onTooltipDespawned.Remove(TooltipDespawned);
        }

        public void Expand(bool isOn)
        {
            display = isOn;
            
            if (display)
            {
                if (!infoBar.gameObject.activeSelf)
                    infoBar.Expand();
            }
            else if (infoBar.gameObject.activeSelf)
                infoBar.Collapse();
        }

        public void ToggleText(bool isOn)
        {
            showText = isOn;

            if (!showText)
                infoBar.SetCaption(string.Format("<color=#{0}> ΔV</color>", BasicDeltaV.TextColor));
            else
                infoBar.SetCaption(string.Format("<color=#FFFFFFFF> {0}</color>", dvText(panel.Stage.deltaV)));
        }

        private void Update()
        {
            if (!display)
                return;

            if (panel.Stage == null)
                return;

            double dv = panel.Stage.deltaV;
            double stageDv = panel.Stage.stageStartDeltaV;
            
            if (infoBar != null)
            {
                infoBar.SetValue((float)dv, 0, (float)stageDv);
            }

            if (toolText != null)
            {
                if (panel.Index == StageManager.LastStage && !BasicDeltaV_Settings.Instance.BasicCurrentOnly)
                    toolText.label.text = dvText(dv, stageDv);
                else
                    toolText.label.text = dvText(dv, stageDv, BasicDeltaV.Instance.LastStage.totalStartDeltaV);
            }

            if (showText)
            {
                infoBar.SetCaption(string.Format("<color=#FFFFFFFF> {0}</color>", dvText(panel.Stage.deltaV)));
            }
        }

        private string dvText(double dv)
        {
            if (dv >= 10000f)
                return string.Format("ΔV: {0} km/s", (dv / 1000).ToString("N2"));

            return string.Format("ΔV: {0} m/s", dv.ToString("N0"));
        }
        
        private string dvText(double dv, double tot)
        {
            if (dv >= 10000f || tot >= 10000f)
                return string.Format("{0} / {1}km/s", (dv / 1000).ToString("N2"), (tot / 1000).ToString("N2"));

            return string.Format("{0} / {1}m/s", dv.ToString("N0"), tot.ToString("N0"));
        }

        private string dvText(double dv, double stagedV, double tot)
        {
            if (dv >= 10000f || tot >= 10000f || stagedV >= 10000f)
                return string.Format("Stage ΔV: {0} / {1} km/s\nVessel ΔV: {2} km/s"
                    , (dv / 1000).ToString("N2"), (stagedV / 1000).ToString("N2"), (tot / 1000).ToString("N2"));

            return string.Format("Stage ΔV: {0} / {1}m/s\nVessel ΔV: {2} m/s"
                , dv.ToString("N0"), stagedV.ToString("N0"), tot.ToString("N0"));
        }

        private void TooltipSpawned(ITooltipController controller, Tooltip tool)
        {
            if (controller == (tooltip as ITooltipController))
                toolText = tool as Tooltip_Text;
        }

        private void TooltipDespawned(Tooltip tool)
        {
            toolText = null;
        }
    }
}
