
using UnityEngine;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.TooltipTypes;

namespace BasicDeltaV
{
    public class BasicDeltaV_TWRGauge : MonoBehaviour
    {
        private KSP.UI.Screens.LinearGauge _gauge;

        private TooltipController_Text _tooltip;
        private Tooltip_Text _toolText;

        private GameObject _parentObject;

        private void Awake()
        {
            GameObject mask = new GameObject("GuageTWR");

            mask.transform.SetParent(transform, false);
            mask.layer = 5;

            RectTransform maskRect = mask.AddComponent<RectTransform>();
            
            mask.AddComponent<RectMask2D>();

            _tooltip = mask.AddComponent<TooltipController_Text>();

            _tooltip.continuousUpdate = true;
            _tooltip.prefab = BasicDeltaV_Loader.TooltipPrefab.prefab;
            _tooltip.RequireInteractable = false;
            _tooltip.TooltipPrefabType = BasicDeltaV_Loader.TooltipPrefab.TooltipPrefabType;

            maskRect.anchorMin = new Vector2(0.5f, 0.5f);
            maskRect.anchorMax = new Vector2(0.5f, 0.5f);
            maskRect.pivot = new Vector2(0.5f, 0.5f);
            maskRect.sizeDelta = new Vector2(31, 111.5f);
            maskRect.anchoredPosition = new Vector2(0, 0);

            var gauges = FlightUIModeController.Instance.stagingQuadrant.GetComponentsInChildren<KSP.UI.Screens.LinearGauge>();

            for (int i = gauges.Length - 1; i >= 0; i--)
            {
                if (gauges[i].gameObject.name == "GaugePitchPointer")
                {
                    //BasicDeltaV.BasicLogging("Pitch Gauge Located\nMin Position: {0:F3} - Max Position: {1:F3}", gauges[i].minValuePosition, gauges[i].maxValuePosition);
                    _gauge = Instantiate(gauges[i], mask.transform);
                    break;
                }
            }
            
            _gauge.gameObject.name = "GaugeTWRPointer";

            _gauge.minValue = 0;
            _gauge.maxValue = 2;
            _gauge.logarithmic = 10;
            _gauge.exponential = 0;

            _gauge.minValuePosition = new Vector2(-1.3f, -55.5f);
            _gauge.maxValuePosition = new Vector2(-1.3f, 52);

            _gauge.Value = 1;

            GameEvents.onTooltipSpawned.Add(TooltipSpawned);
            GameEvents.onTooltipDespawned.Add(TooltipDespawned);

            GameEvents.OnMapEntered.Add(OnMapEnter);
            GameEvents.OnMapExited.Add(OnMapExit);
            GameEvents.OnFlightUIModeChanged.Add(OnFlightModeChange);
        }

        private void OnDestroy()
        {
            GameEvents.onTooltipSpawned.Remove(TooltipSpawned);
            GameEvents.onTooltipDespawned.Remove(TooltipDespawned);

            GameEvents.OnMapEntered.Remove(OnMapEnter);
            GameEvents.OnMapExited.Remove(OnMapExit);
            GameEvents.OnFlightUIModeChanged.Remove(OnFlightModeChange);
        }

        private void LateUpdate()
        {
            if (!FlightGlobals.ready)
                return;

            if (BasicDeltaV.Instance == null)
                return;

            double twr = BasicDeltaV.Instance.ActiveStageTWR;

            float logTWR = (float)twr * 10;

            if (logTWR < 1)
                logTWR = 1;

            if (logTWR > 100)
                logTWR = 100;

            _gauge.Value = logTWR;

            if (_toolText != null)
            {
                _toolText.label.text = string.Format("TWR: {0}", twr.ToString("F2"));
            }
        }

        public void SetParent(GameObject parent)
        {
            _parentObject = parent;
        }
        
        private void TooltipSpawned(ITooltipController controller, Tooltip tool)
        {
            if (controller == (_tooltip as ITooltipController))
                _toolText = tool as Tooltip_Text;
        }

        private void TooltipDespawned(Tooltip tool)
        {
            _toolText = null;
        }

        private void OnMapEnter()
        {
            if (_parentObject.activeSelf)
                _parentObject.SetActive(false);

            _toolText = null;
        }

        private void OnMapExit()
        {
            if (!_parentObject.activeSelf && BasicDeltaV_Settings.Instance.ShowTWRGauge)
                _parentObject.SetActive(true);
        }

        private void OnFlightModeChange(FlightUIMode mode)
        {
            if (mode == FlightUIMode.DOCKING)
            {
                if (_parentObject.activeSelf)
                    _parentObject.SetActive(false);
            }
            else if (mode == FlightUIMode.STAGING)
            {
                if (!_parentObject.activeSelf && BasicDeltaV_Settings.Instance.ShowTWRGauge)
                    _parentObject.SetActive(true);
            }
        }
    }
}
