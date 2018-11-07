#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_AppLauncher - Script for controlling the main toolbar panel
 * 
 * Copyright (C) 2016 DMagic
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version. 
 * 
 * This program is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 * GNU General Public License for more details. 
 * 
 * You should have received a copy of the GNU General Public License 
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. 
 * 
 * 
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using BasicDeltaV.Unity.Interface;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BasicDeltaV.Unity.Unity
{
    public class BasicDeltaV_AppWindow : CanvasFader, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
		[SerializeField]
		private float m_MinHeight = 179;
		[SerializeField]
		private float m_MaxHeight = 560;
        [SerializeField]
        private TextHandler m_VersionText = null;
		[SerializeField]
		private StateToggle m_DisplayToggle = null;
        [SerializeField]
        private StateToggle m_MoreBasicToggle = null;
        [SerializeField]
        private StateToggle m_ShowDVTextToggle = null;
        [SerializeField]
        private StateToggle m_BasicCurrentToggle = null;
        [SerializeField]
        private StateToggle m_BasicStandardToggle = null;
        [SerializeField]
		private StateToggle m_CurrentStage = null;
        [SerializeField]
        private StateToggle m_VectorThrustToggle = null;
        [SerializeField]
        private Toggle m_BodyToggle = null;
        [SerializeField]
        private Toggle m_AtmosphereToggle = null;
        [SerializeField]
        private TextHandler m_BodyTitle = null;
        [SerializeField]
        private GameObject m_BodyBar = null;
		[SerializeField]
		private GameObject m_BodyControlBar = null;
        [SerializeField]
        private GameObject m_BodyPrefab = null;
		[SerializeField]
		private ToggleGroup m_BodyToggleGroup = null;
        [SerializeField]
        private GameObject m_AtmosphereControlBar = null;
		[SerializeField]
		private GameObject m_AtmosphereBar = null;
        [SerializeField]
        private Slider m_AtmosphereSlider = null;
        [SerializeField]
		private TextHandler m_AtmosphereLabel = null;
        [SerializeField]
        private TextHandler m_AtmosphereLegend = null;
        [SerializeField]
		private Slider m_MachSlider = null;
		[SerializeField]
		private TextHandler m_MachLabel = null;
        [SerializeField]
        private GameObject m_ModuleBar = null;
        [SerializeField]
		private StateToggle m_DeltaVToggle = null;
        [SerializeField]
		private StateToggle m_TWRToggle = null;
        [SerializeField]
		private StateToggle m_BurnTimeToggle = null;
        [SerializeField]
		private StateToggle m_MassToggle = null;
        [SerializeField]
		private StateToggle m_ThrustToggle = null;
        [SerializeField]
		private StateToggle m_ISPToggle = null;
        [SerializeField]
        private GameObject m_SettingsBar = null;
        [SerializeField]
		private TextHandler m_AlphaText = null;
		[SerializeField]
		private TextHandler m_StageScaleText = null;
        [SerializeField]
		private TextHandler m_ToolbarScaleText = null;
		[SerializeField]
		private StateToggle m_StageScaleToggle = null;
        [SerializeField]
		private Slider m_AlphaSlider = null;
		[SerializeField]
		private Slider m_StageScaleSlider = null;
        [SerializeField]
        private Slider m_ToolbarScaleSlider = null;
		[SerializeField]
		private EventTrigger m_TopEventHandler = null;
		[SerializeField]
		private EventTrigger m_BottomEventHandler = null;
        
        private IBasicDeltaV basicInterface;
        private RectTransform rect;
        private Vector2 mouseStart;
        private Vector3 windowStart;

        private bool loaded;
        
        protected override void Awake()
        {
			base.Awake();

            rect = GetComponent<RectTransform>();

            Alpha(0);

            Fade(1, true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (rect == null)
                return;

            mouseStart = eventData.position;
            windowStart = rect.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rect == null)
                return;

            rect.position = windowStart + (Vector3)(eventData.position - mouseStart);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (rect == null)
                return;

            if (rect == null)
                return;

            if (basicInterface == null)
                return;

            basicInterface.ClampToScreen(rect);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (basicInterface != null)
                basicInterface.InMenu = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (basicInterface != null)
                basicInterface.InMenu = false;
        }

		public void OnPointerDown(PointerEventData eventData)
		{
			transform.SetAsLastSibling();
		}

		public void OnResize(BaseEventData eventData)
		{
			if (rect == null)
				return;

			if (!(eventData is PointerEventData))
				return;

			checkMaxResize(rect.sizeDelta.y + ((PointerEventData)eventData).delta.y);
		}

		public void OnResizeFlight(BaseEventData eventData)
		{
			if (rect == null)
				return;

			if (!(eventData is PointerEventData))
				return;

			checkMaxResize(rect.sizeDelta.y - ((PointerEventData)eventData).delta.y);
		}

		public void OnEndResize(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData))
				return;

			if (rect == null)
				return;

			checkMaxResize(rect.sizeDelta.y);

			if (basicInterface == null)
				return;

			if (basicInterface.Flight)
				basicInterface.FlightHeight = rect.sizeDelta.y;
			else
				basicInterface.Height = rect.sizeDelta.y;
		}

		private void checkMaxResize(float num)
		{
			if (rect == null)
				return;

			if (num < m_MinHeight)
				num = m_MinHeight;
			else if (num > m_MaxHeight)
				num = m_MaxHeight;

			rect.sizeDelta = new Vector2(rect.sizeDelta.x, num);
		}

        public void setBasic(IBasicDeltaV basic)
        {
            if (basic == null)
                return;

            basicInterface = basic;

            if (m_VersionText != null)
                m_VersionText.OnTextUpdate.Invoke(basic.Version);

			if (m_DisplayToggle != null)
				m_DisplayToggle.isOn = basic.DisplayActive;

            if (m_MoreBasicToggle != null)
            {
                m_MoreBasicToggle.isOn = basic.MoreBasicMode;

                m_MoreBasicToggle.gameObject.SetActive(basic.ShowCurrentStageBar);
            }

            if (m_ShowDVTextToggle != null)
            {
                m_ShowDVTextToggle.isOn = basic.ShowBasicDVText;

                m_ShowDVTextToggle.gameObject.SetActive(basic.ShowCurrentStageBar && basic.MoreBasicMode);
            }
            
            if (m_BasicCurrentToggle != null)
            {
                m_BasicCurrentToggle.isOn = basic.BasicShowCurrent;

                m_BasicCurrentToggle.gameObject.SetActive(basic.ShowCurrentStageBar && basic.MoreBasicMode);
            }

            if (m_BasicStandardToggle != null)
            {
                m_BasicStandardToggle.isOn = basic.BasicShowStandard;

                m_BasicStandardToggle.gameObject.SetActive(basic.ShowCurrentStageBar && basic.MoreBasicMode);
            }

            if (m_CurrentStage != null)
			{
				m_CurrentStage.isOn = basic.CurrentStageOnly;

				m_CurrentStage.gameObject.SetActive(basic.ShowCurrentStageBar);
            }

            if (m_VectorThrustToggle != null)
                m_VectorThrustToggle.isOn = basic.VectoredThrust;
            
            if (m_BodyTitle != null)
                m_BodyTitle.OnTextUpdate.Invoke(basic.CurrentBody);

            CreateCelestialBodies(basic.CelestialBodies);

            if (m_BodyBar != null)
                m_BodyBar.SetActive(basic.ShowBodies);

            if (m_BodyToggle != null)
                m_BodyToggle.isOn = basic.ShowBodies;

			if (m_BodyControlBar != null)
				m_BodyControlBar.SetActive(basic.ShowBody);

            if (m_AtmosphereLegend != null)
                m_AtmosphereLegend.OnTextUpdate.Invoke("|\n" + (basic.MaxDepth / 1000).ToString("N0") + "km");

            if (m_AtmosphereSlider != null)
            {
                m_AtmosphereSlider.maxValue = basic.MaxDepth / 1000;

				if (basic.AtmosphereDepth > basic.MaxDepth)
					basic.AtmosphereDepth = basic.MaxDepth;

                m_AtmosphereSlider.value = basic.AtmosphereDepth;
            }

			if (m_AtmosphereLabel != null)
				m_AtmosphereLabel.OnTextUpdate.Invoke((basic.AtmosphereDepth / 1000).ToString("N0") + "km");

			if (m_MachSlider != null)
				m_MachSlider.value = basic.Mach;

			if (m_MachLabel != null)
				m_MachLabel.OnTextUpdate.Invoke(basic.Mach.ToString("N2"));

            if (m_AtmosphereBar != null)
                m_AtmosphereBar.SetActive(basic.Atmosphere);

            if (m_AtmosphereToggle != null)
                m_AtmosphereToggle.isOn = basic.Atmosphere;

            if (m_AtmosphereControlBar != null)
                m_AtmosphereControlBar.SetActive(basic.ShowAtmosphere);

            if (m_DeltaVToggle != null)
            {
                m_DeltaVToggle.isOn = basic.ShowDeltaV;

                m_DeltaVToggle.gameObject.SetActive(!basic.MoreBasicMode);
            }

            if (m_TWRToggle != null)
            {
                m_TWRToggle.isOn = basic.ShowTWR;

                m_TWRToggle.gameObject.SetActive(!basic.MoreBasicMode);
            }

            if (m_BurnTimeToggle != null)
                m_BurnTimeToggle.isOn = basic.ShowBurnTime;

            if (m_MassToggle != null)
                m_MassToggle.isOn = basic.ShowMass;

            if (m_ThrustToggle != null)
                m_ThrustToggle.isOn = basic.ShowThrust;

            if (m_ISPToggle != null)
                m_ISPToggle.isOn = basic.ShowISP;

			if (m_ModuleBar != null)
				m_ModuleBar.SetActive(basic.Flight);

			if (m_AlphaSlider != null)
				m_AlphaSlider.value = (1 - basic.Alpha) * 50;

			if (m_AlphaText != null)
				m_AlphaText.OnTextUpdate.Invoke((1 - basic.Alpha).ToString("P0"));

			if (m_StageScaleToggle != null)
			{
				m_StageScaleToggle.isOn = basic.StageScaleEditorOnly;

				m_StageScaleToggle.gameObject.SetActive(!basic.Flight);
			}

			if (m_StageScaleSlider != null)
				m_StageScaleSlider.value = basic.StageScale * 100;

			if (m_StageScaleText != null)
				m_StageScaleText.OnTextUpdate.Invoke(basic.StageScale.ToString("P0"));

            if (m_ToolbarScaleSlider != null)
                m_ToolbarScaleSlider.value = basic.ToolbarScale * 10;

			if (m_ToolbarScaleText != null)
				m_ToolbarScaleText.OnTextUpdate.Invoke(basic.ToolbarScale.ToString("P0"));

			if (m_SettingsBar != null)
				m_SettingsBar.SetActive(basic.Flight);

			if (m_TopEventHandler != null)
			{
				if (basic.Flight)
					Destroy(m_TopEventHandler);
			}

			if (m_BottomEventHandler != null)
			{
				if (!basic.Flight)
					Destroy(m_BottomEventHandler);
			}

			if (basic.Flight)
			{
				if (rect != null)
				{
					rect.pivot = new Vector2(1, 1);

					rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + 60);

					basicInterface.ClampToScreen(rect);
				}
			}

            transform.localScale = Vector3.one * basic.ToolbarScale;

			checkMaxResize(basic.Flight ? basic.FlightHeight : basic.Height);

            loaded = true;
        }

        public void Close()
        {
            Fade(0, true, Kill, false);
        }

        private void Kill()
        {
            gameObject.SetActive(false);

            Destroy(gameObject);
        }

		public void SetDisplayToggle(bool isOn)
		{
			if (m_DisplayToggle != null)
				m_DisplayToggle.isOn = isOn;
		}

        public void SetAtmosphereToggle(bool isOn)
        {
            if (m_AtmosphereToggle != null)
                m_AtmosphereToggle.isOn = isOn;
        }

		public void SetStagingScale(float scale)
		{
			loaded = false;

			if (m_StageScaleSlider != null)
				m_StageScaleSlider.value = scale * 100;

			if (m_StageScaleText != null)
				m_StageScaleText.OnTextUpdate.Invoke(scale.ToString("P0"));

			loaded = true;
		}

		public void ToggleDisplayActive(bool isOn)
		{
			if (basicInterface == null || !loaded)
				return;

			basicInterface.DisplayActive = isOn;
		}
        
        public void ToggleBodySelection(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.ShowBodies = isOn;

            if (m_BodyBar != null)
                m_BodyBar.SetActive(isOn);
        }

        public void ToggleAtmospheric(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.Atmosphere = isOn;

            if (m_AtmosphereBar != null)
				m_AtmosphereBar.SetActive(isOn);
        }

        public void ToggleModules(bool isOn)
        {
            if (m_ModuleBar != null)
                m_ModuleBar.SetActive(isOn);
        }

        public void ToggleSettings(bool isOn)
        {
            if (m_SettingsBar != null)
                m_SettingsBar.SetActive(isOn);
        }

        private void CreateCelestialBodies(Dictionary<string, int> bodies)
        {
            if (m_BodyPrefab == null || m_BodyBar == null || bodies == null || basicInterface == null)
                return;

            for (int i = 0; i < bodies.Count; i++)
            {
				string body = bodies.ElementAt(i).Key;

				int offset = bodies[body];

                CreateCelestialBody(body, offset);
            }
        }

        private void CreateCelestialBody(string body, int offset)
        {
            BasicDeltaV_BodyElement element = Instantiate(m_BodyPrefab).GetComponent<BasicDeltaV_BodyElement>();

            if (element == null)
                return;

            element.transform.SetParent(m_BodyBar.transform, false);

            element.Setup(body, body == basicInterface.CurrentBody, offset, m_BodyToggleGroup);

            element.BodySelect.AddListener(SetCelestialBody);
        }

        private void SetCelestialBody(string selection)
        {
            if (basicInterface == null)
                return;

            basicInterface.CurrentBody = selection;
            
            if (m_BodyTitle != null)
                m_BodyTitle.OnTextUpdate.Invoke(selection);

            if (m_AtmosphereBar != null)
                m_AtmosphereBar.SetActive(basicInterface.Atmosphere);

            if (basicInterface.ShowAtmosphere)
            {
                if (m_AtmosphereLegend != null)
                    m_AtmosphereLegend.OnTextUpdate.Invoke("|\n" + (basicInterface.MaxDepth / 1000).ToString("N0") + "km");

                if (m_AtmosphereSlider != null)
                {
                    m_AtmosphereSlider.maxValue = basicInterface.MaxDepth / 1000;

                    if (basicInterface.AtmosphereDepth > basicInterface.MaxDepth)
                        m_AtmosphereSlider.value = basicInterface.MaxDepth;
                }
            }
            if (m_AtmosphereControlBar != null)
                m_AtmosphereControlBar.SetActive(basicInterface.ShowAtmosphere);
        }

        public void AtmosphereDepth(float value)
        {
            if (m_AtmosphereLabel != null)
                m_AtmosphereLabel.OnTextUpdate.Invoke(value.ToString("N0") + "km");

            if (basicInterface == null || !loaded)
                return;

            basicInterface.AtmosphereDepth = value * 1000;
        }

		public void Mach(float value)
		{
			if (m_MachLabel != null)
				m_MachLabel.OnTextUpdate.Invoke(value.ToString("N1"));

			if (basicInterface == null || !loaded)
				return;

			basicInterface.Mach = value;
		}

        public void ToggleDeltaV(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.ShowDeltaV = isOn;
        }

        public void ToggleTWR(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.ShowTWR = isOn;
        }

        public void ToggleBurnTime(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.ShowBurnTime = isOn;
        }

        public void ToggleMass(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.ShowMass = isOn;
        }

        public void ToggleThrust(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.ShowThrust = isOn;
        }

        public void ToggleISP(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.ShowISP = isOn;
        }

        public void ApplyAlpha(float alpha)
        {
            if (m_AlphaText != null)
                m_AlphaText.OnTextUpdate.Invoke((alpha / 50).ToString("P0"));

            if (basicInterface == null || !loaded)
                return;

			float a = alpha / 50;

			a = 1 - a;

			a = Mathf.Clamp(a, 0, 1);

			basicInterface.Alpha = a;
        }

        public void ToggleVectoredThrust(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.VectoredThrust = isOn;
        }

        public void ToggleCurrentStage(bool isOn)
		{
			if (basicInterface == null || !loaded)
				return;

			basicInterface.CurrentStageOnly = isOn;
		}

        public void ToggleMoreBasic(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.MoreBasicMode = isOn;
            
            m_DeltaVToggle.gameObject.SetActive(!isOn);
            
            m_TWRToggle.gameObject.SetActive(!isOn);

            m_ShowDVTextToggle.gameObject.SetActive(isOn);

            m_BasicCurrentToggle.gameObject.SetActive(isOn);

            m_BasicStandardToggle.gameObject.SetActive(isOn);
        }

        public void ToggleShowDVText(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.ShowBasicDVText = isOn;
        }

        public void ToggleBasicCurrent(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.BasicShowCurrent = isOn;
        }

        public void ToggleBasicStandard(bool isOn)
        {
            if (basicInterface == null || !loaded)
                return;

            basicInterface.BasicShowStandard = isOn;
        }

        public void StageScaleEditorOnly(bool isOn)
		{
			if (basicInterface == null || !loaded)
				return;

			basicInterface.StageScaleEditorOnly = isOn;
		}

		public void ApplyStageScale(float scale)
		{
			if (basicInterface == null || !loaded)
				return;

			if (m_StageScaleText != null)
				m_StageScaleText.OnTextUpdate.Invoke((scale / 100).ToString("P0"));

			basicInterface.StageScale = scale / 100;
		}

        public void ApplyToolbarScale(float scale)
		{
            if (m_ToolbarScaleText != null)
                m_ToolbarScaleText.OnTextUpdate.Invoke((scale / 10).ToString("P0"));
        }

        public void SetScale()
        {
            if (m_ToolbarScaleSlider == null)
                return;

            if (basicInterface == null)
                return;

            float scale = m_ToolbarScaleSlider.value / 10;

            transform.localScale = Vector3.one * scale;

            basicInterface.ToolbarScale = scale;
        }
    }
}
