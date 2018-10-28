
using BasicDeltaV.Simulation;
using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;

namespace BasicDeltaV
{
    public class BasicDeltaV_SimpleDeltaVGauge : MonoBehaviour
    {
        private RectTransform parent;
        private Stage stage;
        private int index;

        private bool display;

        private StageIconInfoBox infoBar;
        private TooltipController_Text tooltip;

        private Tooltip_Text toolText;

        public Stage Stage
        {
            get { return stage; }
            set { stage = value; }
        }

        public int Index
        {
            get { return index; }
            set { index = value; }
        }
        
        public void Initialize(RectTransform rect, int i, StageIconInfoBox iconInfo, bool disp)
        {
            parent = rect;
            index = i;

            display = disp;

            infoBar = iconInfo;
            
            stage = BasicDeltaV.Instance.GetStage(i);
            
            if (stage != null)
                infoBar.SetValue((float)stage.deltaV, 0, (float)stage.totalDeltaV);
        }

        private void Awake()
        {
            tooltip = GetComponent<TooltipController_Text>();
            tooltip.continuousUpdate = true;
        }

        private void Start()
        {
            //infoBar = Instantiate(BasicDeltaV_Loader.PanelInfoBarPrefab, transform);

            //if (infoBar == null)
            //    return;

            //Destroy(infoBar.GetComponentInChildren<TextMeshProUGUI>());
            //Destroy(infoBar.GetComponent<Image>());

            //if (BasicDeltaV_Settings.Instance.MoreBasicMode && display)
            //    infoBar.Expand();

            GameEvents.onTooltipSpawned.Add(TooltipSpawned);
            GameEvents.onTooltipDespawned.Add(TooltipDespawned);
        }

        private void OnDestroy()
        {
            GameEvents.onTooltipSpawned.Remove(TooltipSpawned);
            GameEvents.onTooltipDespawned.Remove(TooltipDespawned);
        }

        public void Expand(bool isOn, bool disp, Stage st, int i)
        {
            stage = st;
            index = i;
            display = disp;

            if (isOn && display)
            {
                if (!infoBar.expanded)
                    infoBar.Expand();
            }
            else if (infoBar.expanded)
                infoBar.Collapse();
        }

        private void Update()
        {
            if (!display)
                return;

            if (stage == null)
                return;

            double dv = stage.deltaV;
            double totDv = stage.totalDeltaV;
            
            if (infoBar != null)
                infoBar.SetValue((float)dv, 0, (float)totDv);

            if (toolText != null)
                toolText.label.text = dvText(dv, totDv);
        }

        private string dvText(double dv, double tot)
        {
            if (dv >= 10000f || tot >= 10000f)
                return string.Format("{0:N2}/{1:N2}km/s", dv / 1000, tot / 1000);

            return string.Format("{0:N0}/{1:N0}m/s", dv, tot);
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
