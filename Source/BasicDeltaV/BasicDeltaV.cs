﻿#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV - Primary MonoBehaviour for controlling the addon
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BasicDeltaV.Simulation;
using BasicDeltaV.Unity.Interface;
using UnityEngine;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;

namespace BasicDeltaV
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class BasicDeltaV : MonoBehaviour, IBasicDeltaV
    {
        private static BasicDeltaV instance = null;
        
        private BasicDeltaV_AppLauncher appLauncher;
		private BasicDeltaV_PanelHandler panelHandler;
        private string _version;
        private float _atmosphereDepth;
		private float _mach;
		private float _maxMach;
        private bool _inMenu;
		private bool _loaded;
        private CelestialBody _currentBody;
		private float _stageHeight;

		private int numberOfStages;
		private int stagesCount;
		private Stage[] stages;
        
        public static BasicDeltaV Instance
        {
            get { return instance; }
        }

        private void Awake()
        {
			if (instance != null)
			{
				Destroy(gameObject);
				return;
			}

            instance = this;

			SimManager.UpdateModSettings();
			SimManager.OnReady -= GetStageInfo;
			SimManager.OnReady += GetStageInfo;
        }

        private void Start()
        {
            Assembly assembly = AssemblyLoader.loadedAssemblies.GetByAssembly(Assembly.GetExecutingAssembly()).assembly;
            var ainfoV = Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
            switch (ainfoV == null)
            {
                case true: _version = ""; break;
                default: _version = ainfoV.InformationalVersion; break;
            }

            _currentBody = BodyFromName(BasicDeltaV_Settings.Instance.CelestialBody);
			
            appLauncher = gameObject.AddComponent<BasicDeltaV_AppLauncher>();
			panelHandler = gameObject.AddComponent<BasicDeltaV_PanelHandler>();

			StartCoroutine(WaitForStageManager());

			_loaded = true;
        }

		private IEnumerator WaitForStageManager()
		{
			while (StageManager.Instance == null)
				yield return null;

			RectTransform stageManager = StageManager.Instance.GetComponent<RectTransform>();

			if (stageManager != null)
			{
				stageManager.SetSizeWithCurrentAnchors(0, 350);

				RectTransform rect = StageManager.Instance.scrollRect.GetComponent<RectTransform>();

				rect.pivot = new Vector2(1, 0);

				_stageHeight = StageManager.Instance.GetComponent<RectTransform>().rect.height;

				float scale = StageScaleEditorOnly ? BasicDeltaV_Settings.Instance.StageScale : GameSettings.UI_SCALE_STAGINGSTACK;

				StageManager.Instance.scrollRect.gameObject.transform.localScale = Vector3.one * scale;

				StageManager.Instance.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _stageHeight / scale);
			}

		}

        private void OnDestroy()
        {
            instance = null;
            
            if (appLauncher != null)
                Destroy(appLauncher);

			if (panelHandler != null)
				Destroy(panelHandler);

            if (BasicDeltaV_Settings.Instance.Save())
                BasicLogging("Settings file saved");
        }

		private void Update()
		{
			if (!_loaded)
				return;

			if (stages != null)
			{
				stagesCount = 0;

				for (int i = stages.Length - 1; i >= 0; i--)
				{
					if (stages[i].deltaV > 0f)
						stagesCount += 1;
				}				

				if (stagesCount != numberOfStages)
					numberOfStages = stagesCount;
			}

			try
			{
				SimManager.Gravity = _currentBody.GeeASL * 9.81;

				if (Atmosphere)
					SimManager.Atmosphere = _currentBody.GetPressure(AtmosphereDepth) * PhysicsGlobals.KpaToAtmospheres;
				else
					SimManager.Atmosphere = 0;

				SimManager.Mach = _mach;

				SimManager.RequestSimulation();
				SimManager.TryStartSimulation();
			}
			catch (Exception e)
			{
				BasicLogger.Exception(e, "BasicDeltaV.Update()");
			}

			if (panelHandler != null)
				panelHandler.UpdatePanels();
		}

		private void GetStageInfo()
		{
			stages = SimManager.Stages;
			if (stages != null && stages.Length > 0)
				_maxMach = stages[stages.Length - 1].maxMach;
		}

		public CelestialBody CurrentCelestialBody
		{
			get { return _currentBody; }
		}
        
		public Stage GetStage(int index)
		{
			if (stages == null)
				return null;

			for (int i = stages.Length - 1; i >= 0; i--)
			{
				Stage stage = stages[i];

				if (stage.number == index)
					return stage;
			}

			return null;
		}

        public string Version
        {
            get { return _version; }
        }

        public string CurrentBody
        {
            get { return GetDisplayNameFromBody(BasicDeltaV_Settings.Instance.CelestialBody); }
            set
            {
                BasicDeltaV_Settings.Instance.CelestialBody = GetBodyNameFromDisplay(value);

                _currentBody = BodyFromName(BasicDeltaV_Settings.Instance.CelestialBody);
            }
        }
        
        public bool ShowDeltaV
        {
            get { return BasicDeltaV_Settings.Instance.ShowDeltaV; }
			set
			{
				BasicDeltaV_Settings.Instance.ShowDeltaV = value;

				if (panelHandler != null)
					panelHandler.RefreshPanels();
			}
        }

        public bool ShowTWR
        {
            get { return BasicDeltaV_Settings.Instance.ShowTWR; }
			set
			{
				BasicDeltaV_Settings.Instance.ShowTWR = value;

				if (panelHandler != null)
					panelHandler.RefreshPanels();
			}
        }

        public bool ShowBurnTime
        {
            get { return BasicDeltaV_Settings.Instance.ShowBurnTime; }
			set
			{
				BasicDeltaV_Settings.Instance.ShowBurnTime = value;

				if (panelHandler != null)
					panelHandler.RefreshPanels();
			}
        }

        public bool ShowISP
        {
            get { return BasicDeltaV_Settings.Instance.ShowISP; }
			set
			{
				BasicDeltaV_Settings.Instance.ShowISP = value;

				if (panelHandler != null)
					panelHandler.RefreshPanels();
			}
        }

        public bool ShowMass
        {
            get { return BasicDeltaV_Settings.Instance.ShowMass; }
			set
			{
				BasicDeltaV_Settings.Instance.ShowMass = value;

				if (panelHandler != null)
					panelHandler.RefreshPanels();
			}
        }

        public bool ShowThrust
        {
            get { return BasicDeltaV_Settings.Instance.ShowThrust; }
			set
			{
				BasicDeltaV_Settings.Instance.ShowThrust = value;

				if (panelHandler != null)
					panelHandler.RefreshPanels();
			}
        }

        public bool ShowBodies
        {
            get { return BasicDeltaV_Settings.Instance.ShowBodies; }
            set { BasicDeltaV_Settings.Instance.ShowBodies = value; }
        }

        public bool Atmosphere
        {
            get { return BasicDeltaV_Settings.Instance.ShowAtmosphere; }
            set
            {
                BasicDeltaV_Settings.Instance.ShowAtmosphere = value;
            }
        }

        public bool ShowAtmosphere
        {
            get { return _currentBody == null ? false : _currentBody.atmosphere; }
        }

        public bool InMenu
        {
            get { return _inMenu; }
            set
            {
                _inMenu = value;

                if (!value && BasicDeltaV_AppLauncher.Instance != null)
                    BasicDeltaV_AppLauncher.Instance.StartCoroutine(BasicDeltaV_AppLauncher.Instance.MenuHoverOutWait());
            }
        }

		public bool StageScaleEditorOnly
		{
			get { return BasicDeltaV_Settings.Instance.StageScaleEditorOnly; }
			set
			{
				BasicDeltaV_Settings.Instance.StageScaleEditorOnly = value;

				if (value)
				{

					if (StageManager.Instance != null)
					{
						StageManager.Instance.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _stageHeight / BasicDeltaV_Settings.Instance.StageScale);

						StageManager.Instance.scrollRect.gameObject.transform.localScale = Vector3.one * BasicDeltaV_Settings.Instance.StageScale;
					}

					if (BasicDeltaV_AppLauncher.Instance != null && BasicDeltaV_AppLauncher.Instance.Launcher != null)
						BasicDeltaV_AppLauncher.Instance.Launcher.SetStagingScale(BasicDeltaV_Settings.Instance.StageScale);
				}
				else
				{

					if (StageManager.Instance != null)
					{
						StageManager.Instance.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _stageHeight / GameSettings.UI_SCALE_STAGINGSTACK);

						StageManager.Instance.scrollRect.gameObject.transform.localScale = Vector3.one * GameSettings.UI_SCALE_STAGINGSTACK;
					}

					if (BasicDeltaV_AppLauncher.Instance != null && BasicDeltaV_AppLauncher.Instance.Launcher != null)
						BasicDeltaV_AppLauncher.Instance.Launcher.SetStagingScale(GameSettings.UI_SCALE_STAGINGSTACK);
				}

			}
		}

        public float AtmosphereDepth
        {
            get { return _atmosphereDepth; }
            set { _atmosphereDepth = value; }
        }

        public float MaxDepth
        {
            get
            {
                if (_currentBody == null)
                    return 0;

                if (!_currentBody.atmosphere)
                    return 0;

                return (float)_currentBody.atmosphereDepth;
            }
        }

		public float Mach
		{
			get { return _mach; }
			set { _mach = value; }
		}

		public float MaxMach
		{
			get { return _maxMach; }
		}

        public float Alpha
        {
            get { return BasicDeltaV_Settings.Instance.PanelAlpha; }
            set
            {
                BasicDeltaV_Settings.Instance.PanelAlpha = value;

				if (panelHandler != null)
					panelHandler.UpdateAlpha(value);
            }
        }

		public float StageScale
		{
			get { return StageScaleEditorOnly ? BasicDeltaV_Settings.Instance.StageScale : GameSettings.UI_SCALE_STAGINGSTACK; }
			set
			{
				if (StageManager.Instance != null)
				{
					StageManager.Instance.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _stageHeight / value);

					StageManager.Instance.scrollRect.gameObject.transform.localScale = Vector3.one * value;
				}

				if (!StageScaleEditorOnly)
					GameSettings.UI_SCALE_STAGINGSTACK = value;
				else
					BasicDeltaV_Settings.Instance.StageScale = value;
			}
		}

        public float ToolbarScale
        {
            get { return BasicDeltaV_Settings.Instance.ToolbarScale; }
            set { BasicDeltaV_Settings.Instance.ToolbarScale = value; }
        }

        public float Height
        {
			get { return BasicDeltaV_Settings.Instance.WindowHeight; }
			set { BasicDeltaV_Settings.Instance.WindowHeight = value; }
        }

        public Dictionary<string, int> CelestialBodies
        {
            get
            {
				Dictionary<string, int> orderedBodies = new Dictionary<string, int>();

                var planets = FlightGlobals.Bodies.Where(b => b.referenceBody == Planetarium.fetch.Sun && b.referenceBody != b);

                var orderedPlanets = planets.OrderBy(p => p.orbit.semiMajorAxis).ToList();

                for (int i = 0; i < orderedPlanets.Count; i++)
                {
                    CelestialBody body = orderedPlanets[i];

                    orderedBodies.Add(body.displayName, 4);

                    for (int j = 0; j < body.orbitingBodies.Count; j++)
                    {
                        CelestialBody moon = body.orbitingBodies[j];

                        orderedBodies.Add(moon.displayName, 12);

                        for (int k = 0; k < moon.orbitingBodies.Count; k++)
                        {
                            CelestialBody subMoon = moon.orbitingBodies[k];

							orderedBodies.Add(subMoon.displayName, 18);

                            for (int l = 0; l < subMoon.orbitingBodies.Count; l++)
                            {
                                CelestialBody subSubMoon = subMoon.orbitingBodies[l];

								orderedBodies.Add(subSubMoon.displayName, 24);
                            }
                        }
                    }
                }

                return orderedBodies;
            }
        }

        public void ClampToScreen(RectTransform rect)
        {
            UIMasterController.ClampToScreen(rect, Vector2.zero);
        }
        
        public static void BasicLogging(string s, params object[] m)
        {
            Debug.Log(string.Format("[Basic_DeltaV] " + s, m));
        }

        private string GetBodyNameFromDisplay(string display)
        {
            for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
            {
                CelestialBody b = FlightGlobals.Bodies[i];

                if (b.displayName == display)
                    return b.bodyName;
            }

            return display;
        }

        private string GetDisplayNameFromBody(string bodyName)
        {
            for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
            {
                CelestialBody b = FlightGlobals.Bodies[i];

                if (b.bodyName == bodyName)
                    return b.displayName;
            }

            return bodyName;
        }

        private CelestialBody BodyFromName(string bodyName)
        {
            for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
            {
                CelestialBody b = FlightGlobals.Bodies[i];

                if (b.bodyName == bodyName)
                    return b;
            }

            return null;
        }
    }
}