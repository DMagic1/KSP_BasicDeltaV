
using BasicDeltaV.Simulation;
using UnityEngine;
using UnityEngine.UI;
using KSP.UI.Screens;
using System;

namespace BasicDeltaV
{
    public class BasicDeltaV_SliderGroup
    {
        private Slider _stageDVSlider;

        private StageGroup _group;

        private bool _active;
        
        public int StageIndex
        {
            get { return _group.inverseStageIndex; }
        }

        public Slider StageDVSlider
        {
            get { return _stageDVSlider; }
        }

        public BasicDeltaV_SliderGroup(StageGroup group)
        {
            var images = group.GetComponentsInChildren<Image>();

            Image deltaVImage = null;

            for (int i = images.Length - 1; i >= 0; i--)
            {
                if (images[i].name == "DeltaV")
                {
                    deltaVImage = images[i];

                    break;
                }
            }

            if (deltaVImage == null)
                return;

            GameObject dvBackground = new GameObject("DeltaVBackground", new Type[3] { typeof(Image), typeof(RectTransform), typeof(CanvasRenderer) });

            dvBackground.transform.SetParent(deltaVImage.transform, false);
            dvBackground.transform.SetAsFirstSibling();
            dvBackground.layer = 5;

            Image dvBackgroundImage = dvBackground.GetComponent<Image>();
            dvBackgroundImage.sprite = deltaVImage.sprite;
            dvBackgroundImage.color = new Color32(11, 166, 255, 255);
            dvBackgroundImage.rectTransform.anchorMax = deltaVImage.rectTransform.anchorMax;
            dvBackgroundImage.rectTransform.anchorMin = deltaVImage.rectTransform.anchorMin;
            dvBackgroundImage.rectTransform.pivot = deltaVImage.rectTransform.pivot;
            dvBackgroundImage.rectTransform.sizeDelta = deltaVImage.rectTransform.sizeDelta;
            dvBackgroundImage.rectTransform.anchoredPosition3D = deltaVImage.rectTransform.anchoredPosition3D;

            Slider dvSlider = deltaVImage.gameObject.AddComponent<Slider>();

            dvSlider.interactable = false;
            dvSlider.navigation = new Navigation() { mode = Navigation.Mode.None };
            dvSlider.fillRect = dvBackgroundImage.rectTransform;
            dvSlider.SetDirection(Slider.Direction.LeftToRight, false);
            dvSlider.minValue = 0;
            dvSlider.maxValue = 100;
            dvSlider.wholeNumbers = true;
            dvSlider.value = 0;

            deltaVImage.color = Color.gray;

            _stageDVSlider = dvSlider;
            _group = group;
            
            if (BasicDeltaV_Settings.Instance.ShowDVSliders)
            {
                _active = true;
                Stage stage = BasicDeltaV.Instance.GetStage(_group.inverseStageIndex);

                _stageDVSlider.maxValue = (float)stage.stageStartDeltaV;
                _stageDVSlider.value = (float)stage.deltaV;
            }
            else
            {
                _active = false;
                _stageDVSlider.maxValue = 1;
                _stageDVSlider.value = 1;
            }
        }

        public void UpdateSliderDV(float dv, float maxDV)
        {
            if (!_active)
                return;

            if (_stageDVSlider == null)
                return;

            _stageDVSlider.maxValue = maxDV;
            _stageDVSlider.value = dv;
        }

        public void ToggleSliderActivation(bool isOn)
        {
            _active = isOn;

            if (isOn)
            {
                Stage stage = BasicDeltaV.Instance.GetStage(_group.inverseStageIndex);

                _stageDVSlider.maxValue = (float)stage.stageStartDeltaV;
                _stageDVSlider.value = (float)stage.deltaV;
            }
            else
            {
                _stageDVSlider.maxValue = 1;
                _stageDVSlider.value = 1;
            }
        }
    }
}
