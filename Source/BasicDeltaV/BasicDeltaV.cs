#region License
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
using BasicDeltaV.Unity;
using KSP.UI;
using KSP.UI.Screens;
using KSP.UI.Screens.Flight;

namespace BasicDeltaV
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class BasicDeltaV : MonoBehaviour, IBasicDeltaV
    {
        public const double GRAVITY = 9.80665;

        private static BasicDeltaV instance = null;
		private static bool _inFlight;
		private static bool _readoutsAvailable;
        
        private BasicDeltaV_AppLauncher appLauncher;
		private BasicDeltaV_PanelManager panelHandler;

        private BasicDVPanelManager panelManager;

        private string _version;
        private float _atmosphereDepth;
		private float _mach;
		private float _maxMach;
        private bool _inMenu;
		private bool _loaded;
		private bool _simpleRestrictions;
		private bool _complexRestrictions;
        private CelestialBody _currentBody;
		private float _stageHeight;
		private BasicDeltaV_GameParameters _settings;
		private int _panelRefreshTimer;
		private const int _panelRefreshWait = 5;

		private int numberOfStages;
		private int stagesCount;
		private Stage[] stages;

        private VesselDeltaV _vesselDeltaV;

        private BasicDeltaV_TWRGauge _twrGauge;
        
        public static BasicDeltaV Instance
        {
            get { return instance; }
        }

		public static bool AvailableInFlight
		{
			get { return _inFlight; }
		}

		public static bool ReadoutsAvailable
		{
			get { return _readoutsAvailable; }
		}

        public double ActiveStageTWR
        {
            get
            {
                if (SimManager.LastStage != null)
                {
                    double grav = FlightGlobals.currentMainBody.gravParameter / Math.Pow(FlightGlobals.currentMainBody.Radius, 2);

                    return SimManager.LastStage.thrust / (SimManager.LastStage.totalMass * grav);
                }

                return 0;
            }
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
			_settings = HighLogic.CurrentGame.Parameters.CustomParams<BasicDeltaV_GameParameters>();

            panelManager = gameObject.AddComponent<BasicDVPanelManager>();

            _inFlight = _settings.AllowFlight;

			if (HighLogic.LoadedSceneIsEditor)
			{
				_simpleRestrictions = false;
				_complexRestrictions = false;
				_readoutsAvailable = true;

				_currentBody = BodyFromName(BasicDeltaV_Settings.Instance.CelestialBody);
			}
			else if (HighLogic.LoadedSceneIsFlight)
			{
				if (!_inFlight)
				{
					Destroy(gameObject);
					return;
				}

				GameEvents.onVesselCrewWasModified.Add(CrewModified);
				GameEvents.onVesselChange.Add(CrewModified);
				GameEvents.onStageActivate.Add(OnStage);

				CheckVesselCrew(FlightGlobals.ActiveVessel);

                DeactivateDeltaV(FlightGlobals.ActiveVessel);

                _currentBody = FlightGlobals.currentMainBody;

                StartCoroutine(WaitForNavball(BasicDeltaV_Settings.Instance.MoreBasicMode));
			}

			GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnStageModify);

			if (_currentBody == null)
				_currentBody = Planetarium.fetch.Home;
			
            Assembly assembly = AssemblyLoader.loadedAssemblies.GetByAssembly(Assembly.GetExecutingAssembly()).assembly;
            var ainfoV = Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
            switch (ainfoV == null)
            {
                case true: _version = ""; break;
                default: _version = ainfoV.InformationalVersion; break;
            }

            appLauncher = gameObject.AddComponent<BasicDeltaV_AppLauncher>();
			panelHandler = gameObject.AddComponent<BasicDeltaV_PanelManager>();

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
				stageManager.SetSizeWithCurrentAnchors(0, 390);

				if (HighLogic.LoadedSceneIsEditor)
				{
					RectTransform rect = StageManager.Instance.scrollRect.GetComponent<RectTransform>();

					rect.pivot = new Vector2(1, 0);

					_stageHeight = StageManager.Instance.GetComponent<RectTransform>().rect.height;

					float scale = StageScaleEditorOnly ? BasicDeltaV_Settings.Instance.StageScale : GameSettings.UI_SCALE_STAGINGSTACK;

					StageManager.Instance.scrollRect.gameObject.transform.localScale = Vector3.one * scale;

					StageManager.Instance.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _stageHeight / scale);
				}
			}
		}

        private IEnumerator WaitForNavball(bool active)
        {
            NavBall navball = null;

            yield return null;

            while (navball == null)
            {
                navball = FindObjectOfType<NavBall>();

                yield return null;
            }

            GeeGauge gauge = navball.transform.parent.GetComponentInChildren<GeeGauge>();

            GameObject twr = Instantiate(gauge, gauge.transform.parent).gameObject;
            twr.name = "TWRGaugePointer";
            
            RotationalGauge rotGeeGauge = twr.GetComponent<RotationalGauge>();
            
            Destroy(twr.GetComponent<GeeGauge>());
            
            _twrGauge = twr.AddComponent<BasicDeltaV_TWRGauge>();
            
            _twrGauge.gauge = rotGeeGauge;

            //BasicLogging("Gee Gauge Rot: {0:F6}", new Vector2(rotGeeGauge.minRot, rotGeeGauge.maxRot));

            _twrGauge.gauge.minValue = 0;
            _twrGauge.gauge.maxValue = 3;
            _twrGauge.gauge.logarithmic = 10;

            _twrGauge.gauge.minRot = -27.5f;

            Image gaugeImage = _twrGauge.GetComponentInChildren<Image>();

            gaugeImage.gameObject.name = "TWRGaugePointerImage";

            RectTransform rotRect = gaugeImage.rectTransform;

            rotRect.anchoredPosition = new Vector2(rotRect.anchoredPosition.x + 20, rotRect.anchoredPosition.y);
            
            gaugeImage.sprite = BasicDeltaV_Loader.TWRGaugeSprite;

            _twrGauge.gameObject.SetActive(active);
        }

        private void OnDestroy()
        {
            instance = null;

			GameEvents.onVesselCrewWasModified.Remove(CrewModified);
			GameEvents.onVesselChange.Remove(CrewModified);
			GameEvents.onStageActivate.Remove(OnStage);
			GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnStageModify);

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

			if (!DisplayActive && !MoreBasicMode)
				return;

			if (!_readoutsAvailable)
				return;

			if (HighLogic.LoadedSceneIsFlight && MapView.MapIsEnabled)
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
                if (HighLogic.LoadedSceneIsEditor)
                {
                    SimManager.Gravity = _currentBody.GeeASL * GRAVITY;

                    if (Atmosphere)
                        SimManager.Atmosphere = _currentBody.GetPressure(_atmosphereDepth) * PhysicsGlobals.KpaToAtmospheres;
                    else
                        SimManager.Atmosphere = 0;

                    SimManager.Mach = _mach;

                    SimManager.RequestSimulation();
                    SimManager.TryStartSimulation();
                }
                else
                {
                    SimManager.RequestSimulation();
                    SimManager.TryStartSimulation();

                    if (SimManager.ResultsReady())
                    {
                        if (FlightGlobals.ActiveVessel != null)
                        {
                            SimManager.Gravity = FlightGlobals.ActiveVessel.mainBody.gravParameter / Math.Pow(FlightGlobals.ActiveVessel.mainBody.Radius + FlightGlobals.ActiveVessel.mainBody.GetAltitude(FlightGlobals.ActiveVessel.CoM), 2);

                            SimManager.Mach = FlightGlobals.ActiveVessel.mach;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BasicLogger.Exception(e, "BasicDeltaV.Update()");
            }

            if (!DisplayActive)
                return;

            if (_panelRefreshTimer < _panelRefreshWait)
            {
                _panelRefreshTimer++;
                return;
            }

            _panelRefreshTimer = 0;

            if (panelHandler != null)
                panelHandler.UpdatePanels();
        }

        private void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (FlightGlobals.ActiveVessel == null)
                return;

            if (_vesselDeltaV == null)
                return;

            if (_vesselDeltaV.enabled)
                _vesselDeltaV.enabled = false;

            for (int i = _vesselDeltaV.stageInfo.Count - 1; i >= 0; i--)
            {
                DeltaVStageInfo info = _vesselDeltaV.stageInfo[i];

                Stage stage = GetStage(info.stage);

                if (stage == null)
                    continue;

                info.deltaVActual = (float)stage.deltaV;
                info.deltaVatASL = info.deltaVActual;
                info.deltaVinVac = info.deltaVActual;

                info.TWRActual = (float)(stage.actualThrust / (stage.startMass * FlightGlobals.currentMainBody.GeeASL * GRAVITY));
                info.TWRASL = (float)(stage.thrust / (stage.startMass * FlightGlobals.currentMainBody.GeeASL * GRAVITY));
                info.TWRVac = info.TWRASL;

                info.stageMass = (float)stage.mass;
                info.startMass = (float)stage.startMass;
                info.endMass = (float)stage.endMass;
                info.fuelMass = (float)stage.resourceMass;

                info.thrustActual = (float)stage.actualSimpleThrust;
                info.thrustASL = (float)stage.simpleThrust;
                info.thrustVac = info.thrustASL;

                info.vectoredThrustActual = (float)stage.actualThrust;
                info.vectoredThrustASL = (float)stage.thrust;
                info.vectoredThrustVac = info.vectoredThrustASL;

                info.ispActual = (float)stage.isp;
                info.ispASL = (float)stage.isp;
                info.ispVac = info.ispASL;

                info.totalExhaustVelocityActual = (float)stage.totalActualExhaustVelocity;
                info.totalExhaustVelocityASL = (float)stage.totalExhaustVelocity;
                info.totalExhaustVelocityVAC = info.totalExhaustVelocityASL;

                info.vectoredExhaustVelocityActual = stage.totalVectoredActualExhaustVelocity;
                info.vectoredExhaustVelocityASL = stage.totalVectoredExhaustVelocity;
                info.vectoredExhaustVelocityVAC = info.vectoredExhaustVelocityASL;

                info.stageBurnTime = (float)stage.time;
            }
        }

        private void CrewModified(Vessel v)
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (v == null)
				return;

			if (v != FlightGlobals.ActiveVessel)
				return;

			CheckVesselCrew(v);

            DeactivateDeltaV(v);

			appLauncher.ToggleButtonState(_readoutsAvailable);

			if (panelHandler != null)
				panelHandler.RefreshPanels();
		}
        
        private void DeactivateDeltaV(Vessel v)
        {
            if (v != FlightGlobals.ActiveVessel)
                return;

            _vesselDeltaV = v.VesselDeltaV;
            _vesselDeltaV.StopAllCoroutines();

            v.VesselDeltaV.enabled = false;
        }

        private void CheckVesselCrew(Vessel v)
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (!_inFlight)
			{
				_simpleRestrictions = true;
				_complexRestrictions = true;
				_readoutsAvailable = false;
				return;
			}

			if (!_settings.CrewRestrictions)
			{
				_simpleRestrictions = false;
				_complexRestrictions = false;
				_readoutsAvailable = true;
				return;
			}

			_simpleRestrictions = true;
			_complexRestrictions = true;

			if (v != null)
			{
				List<ProtoCrewMember> crew = v.GetVesselCrew();

				for (int i = crew.Count - 1; i >= 0; i--)
				{
					ProtoCrewMember kerbal = crew[i];

					if (_settings.CrewTypeRestrictions)
					{
						for (int j = BasicDeltaV_Settings.Instance.SkillTypes.Count - 1; j >= 0; j--)
						{
							string skill = BasicDeltaV_Settings.Instance.SkillTypes[j];

							if (!kerbal.HasEffect(skill))
								continue;

							if (_settings.CrewLevelRestrictions)
							{
								int level = kerbal.experienceLevel;

								if (level >= _settings.SimpleRestrictionLevel)
									_simpleRestrictions = false;

								if (level >= _settings.ComplexRestrictionLevel)
									_complexRestrictions = false;

								if (!_simpleRestrictions && !_complexRestrictions)
									break;
							}
							else
							{
								_simpleRestrictions = false;
								_complexRestrictions = false;
								break;
							}
						}
					}
					else
					{
						if (_settings.CrewLevelRestrictions)
						{
							int level = kerbal.experienceLevel;

							if (level >= _settings.SimpleRestrictionLevel)
								_simpleRestrictions = false;

							if (level >= _settings.ComplexRestrictionLevel)
								_complexRestrictions = false;

							if (!_simpleRestrictions && !_complexRestrictions)
								break;
						}
						else
						{
							_simpleRestrictions = false;
							_complexRestrictions = false;
							break;
						}
					}

					if (!_simpleRestrictions && !_complexRestrictions)
						break;
				}
			}
			else
			{
				//_simpleRestrictions = _settings.CareerRestrictions && ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation) < 0.5f;
				//_complexRestrictions = _settings.CareerRestrictions && ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation) < 0.75f;
				_simpleRestrictions = true;
				_complexRestrictions = true;
			}

			BasicLogging("Crew Status Check - Simple Restrictions: {0}, Complex Restrictions: {1}", _simpleRestrictions, _complexRestrictions);

			if (_simpleRestrictions && _complexRestrictions)
				_readoutsAvailable = false;
			else
				_readoutsAvailable = true;
		}

		private void OnStageModify()
		{
			if (panelHandler != null)
				panelHandler.RefreshPanels();
		}

		private void OnStage(int stage)
		{
			CheckVesselCrew(FlightGlobals.ActiveVessel);

			appLauncher.ToggleButtonState(_readoutsAvailable);

			if (panelHandler != null)
				panelHandler.RefreshPanels();
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
				if (stages[i].number == index)
					return stages[i];
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

				if (_currentBody == null)
					_currentBody = Planetarium.fetch.Home;
            }
        }

		public bool Flight
		{
			get { return HighLogic.LoadedSceneIsFlight; }
		}

		public bool DisplayActive
		{
			get
            {
                if (HighLogic.LoadedSceneIsEditor)
                    return BasicDeltaV_Settings.Instance.DisplayActive;
                else
                    return BasicDeltaV_Settings.Instance.DisplayActiveFlight;
            }
			set
			{
                if (HighLogic.LoadedSceneIsEditor)
                    BasicDeltaV_Settings.Instance.DisplayActive = value;
                else
                    BasicDeltaV_Settings.Instance.DisplayActiveFlight = value;

				if (panelHandler != null && value == false)
					panelHandler.PanelHideDisplay();
			}
		}

		public bool SimpleRestrictions
		{
			get { return _simpleRestrictions; }
		}

		public bool ComplexRestrictions
		{
			get { return _complexRestrictions; }
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

		public bool ShowBody
		{
			get { return HighLogic.LoadedSceneIsEditor; }
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
            get { return _currentBody == null || HighLogic.LoadedSceneIsFlight ? false : _currentBody.atmosphere; }
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

		public bool CurrentStageOnly
		{
			get { return BasicDeltaV_Settings.Instance.ShowCurrentStageOnly; }
			set
			{
				BasicDeltaV_Settings.Instance.ShowCurrentStageOnly = value;

				if (panelHandler != null)
					panelHandler.RefreshPanels();
			}
		}

        public bool MoreBasicMode
        {
            get { return BasicDeltaV_Settings.Instance.MoreBasicMode; }
            set
            {
                BasicDeltaV_Settings.Instance.MoreBasicMode = value;

                if (panelHandler != null)
                    panelHandler.RefreshPanels();

                if (_twrGauge != null)
                    _twrGauge.gameObject.SetActive(value);
            }
        }

        public bool ShowCurrentStageBar
		{
			get { return HighLogic.LoadedSceneIsFlight; }
		}

		public bool StageScaleEditorOnly
		{
			get { return BasicDeltaV_Settings.Instance.StageScaleEditorOnly; }
			set
			{
				BasicDeltaV_Settings.Instance.StageScaleEditorOnly = value;

				if (!HighLogic.LoadedSceneIsEditor)
					return;

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
			get { return StageScaleEditorOnly && HighLogic.LoadedSceneIsEditor ? BasicDeltaV_Settings.Instance.StageScale : GameSettings.UI_SCALE_STAGINGSTACK; }
			set
			{
				if (StageManager.Instance != null && HighLogic.LoadedSceneIsEditor)
				{
					StageManager.Instance.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _stageHeight / value);

					StageManager.Instance.scrollRect.gameObject.transform.localScale = Vector3.one * value;
				}
				else if (HighLogic.LoadedSceneIsFlight)
					FlightUIModeController.Instance.SetUIElementScale(FlightUIElements.STAGING, value);

				if (!StageScaleEditorOnly || HighLogic.LoadedSceneIsFlight)
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

		public float FlightHeight
		{
			get { return BasicDeltaV_Settings.Instance.FlightWindowHeight; }
			set { BasicDeltaV_Settings.Instance.FlightWindowHeight = value; }
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

                    orderedBodies.Add(body.displayName.LocalizeBodyName(), 4);

                    for (int j = 0; j < body.orbitingBodies.Count; j++)
                    {
                        CelestialBody moon = body.orbitingBodies[j];

						orderedBodies.Add(moon.displayName.LocalizeBodyName(), 12);

                        for (int k = 0; k < moon.orbitingBodies.Count; k++)
                        {
                            CelestialBody subMoon = moon.orbitingBodies[k];

							orderedBodies.Add(subMoon.displayName.LocalizeBodyName(), 18);

                            for (int l = 0; l < subMoon.orbitingBodies.Count; l++)
                            {
                                CelestialBody subSubMoon = subMoon.orbitingBodies[l];

								orderedBodies.Add(subSubMoon.displayName.LocalizeBodyName(), 24);
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

				if (b.displayName.LocalizeBodyName() == display)
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
					return b.displayName.LocalizeBodyName();
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