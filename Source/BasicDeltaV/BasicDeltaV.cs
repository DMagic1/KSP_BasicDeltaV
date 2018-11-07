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
using TMPro;
using Expansions.Missions.Adjusters;
using UnityEngine.Profiling;

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
        private Stage _lastStage;

        private bool _updateDeltaV;
        private bool _updateSimulator;
        private bool _updatePanels;

        private static bool _vesselDeltaVFlagAssigned;

        private static FieldInfo _vesselDeltaVFlag;
        private static FieldInfo _vesselDeltaVTotalDVActual;
        private static FieldInfo _vesselDeltaVTotalDVASL;
        private static FieldInfo _vesselDeltaVTotalDVVac;

        public static string TextColor = ColorUtility.ToHtmlStringRGB(new Color(0.658825f, 1f, 0.015686f, 1f));

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

        public Stage LastStage
        {
            get { return _lastStage; }
        }

        public double ActiveStageTWR
        {
            get
            {
                if (_lastStage != null)
                    return _lastStage.thrustToWeight;

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

            _inFlight = _settings.AllowFlight || _settings.DisableStockDeltaV;

            if (!_vesselDeltaVFlagAssigned)
            {
                AssignDeltaVFlag();
                _vesselDeltaVFlagAssigned = true;
            }

			if (HighLogic.LoadedSceneIsEditor)
			{
				_simpleRestrictions = false;
				_complexRestrictions = false;
				_readoutsAvailable = true;

				_currentBody = BodyFromName(BasicDeltaV_Settings.Instance.CelestialBody);

                GameEvents.onEditorShipModified.Add(DeactivateEditorDeltaV);

                GameEvents.onPartPriorityChanged.Add(TriggerSimulator);
                GameEvents.onPartResourceListChange.Add(TriggerSimulator);
                GameEvents.onPartCrossfeedStateChange.Add(TriggerSimulator);
                GameEvents.onPartFuelLookupStateChange.Add(TriggerSimulator);

                GameEvents.onPartModuleAdjusterAdded.Add(TriggerSimulator);
                GameEvents.onPartModuleAdjusterRemoved.Add(TriggerSimulator);
                GameEvents.onPartResourceFlowStateChange.Add(TriggerSimulator);

                GameEvents.StageManager.OnGUIStageAdded.Add(TriggerSimulator);
                GameEvents.StageManager.OnGUIStageRemoved.Add(TriggerSimulator);
                GameEvents.StageManager.OnStagingSeparationIndices.Add(TriggerSimulator);
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
                
                StartCoroutine(WaitForFlightController());
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

            SimManager.vectoredThrust = BasicDeltaV_Settings.Instance.VectoredThrust;

            appLauncher = gameObject.AddComponent<BasicDeltaV_AppLauncher>();
			panelHandler = gameObject.AddComponent<BasicDeltaV_PanelManager>();

			StartCoroutine(WaitForStageManager());

			_loaded = true;
        }

        private void OnDestroy()
        {
            instance = null;

            GameEvents.onVesselCrewWasModified.Remove(CrewModified);
            GameEvents.onVesselChange.Remove(CrewModified);
            GameEvents.onStageActivate.Remove(OnStage);
            GameEvents.onEditorShipModified.Remove(DeactivateEditorDeltaV);
            GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnStageModify);

            GameEvents.onPartPriorityChanged.Remove(TriggerSimulator);
            GameEvents.onPartResourceListChange.Remove(TriggerSimulator);
            GameEvents.onPartCrossfeedStateChange.Remove(TriggerSimulator);
            GameEvents.onPartFuelLookupStateChange.Remove(TriggerSimulator);

            GameEvents.onPartModuleAdjusterAdded.Remove(TriggerSimulator);
            GameEvents.onPartModuleAdjusterRemoved.Remove(TriggerSimulator);

            GameEvents.StageManager.OnGUIStageAdded.Remove(TriggerSimulator);
            GameEvents.StageManager.OnGUIStageRemoved.Remove(TriggerSimulator);
            GameEvents.StageManager.OnStagingSeparationIndices.Remove(TriggerSimulator);

            if (appLauncher != null)
                Destroy(appLauncher);

            if (panelHandler != null)
                Destroy(panelHandler);

            if (BasicDeltaV_Settings.Instance.Save())
                BasicLogging("Settings file saved");
        }

        private void AssignDeltaVFlag()
        {
            var flags = typeof(VesselDeltaV).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).ToArray();

            for (int i = 0; i < flags.Length; i++)
            {
                string flag = flags[i].Name;

                if (flag == "_isReady")
                    _vesselDeltaVFlag = flags[i];
                else if (flag == "_totalDeltaVActual")
                    _vesselDeltaVTotalDVActual = flags[i];
                else if (flag == "_totalDeltaVASL")
                    _vesselDeltaVTotalDVASL = flags[i];
                else if (flag == "_totalDeltaVVac")
                    _vesselDeltaVTotalDVVac = flags[i];
            }
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
        
        private IEnumerator WaitForFlightController()
        {
            while (FlightUIModeController.Instance == null)
            {
                yield return null;
            }

            GameObject twrGaugeObj = new GameObject("TWR Linear Gauge Panel", new Type[] { typeof(RectTransform) });
            twrGaugeObj.transform.SetParent(FlightUIModeController.Instance.uiModeFrame.transform, false);

            RectTransform rect = twrGaugeObj.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(60, 134);
            rect.anchoredPosition = new Vector2(-8, 0);

            twrGaugeObj.layer = 5;

            Image twrGaugeImg = twrGaugeObj.AddComponent<Image>();

            twrGaugeImg.sprite = BasicDeltaV_Loader.TWRGaugeSprite;

            var tmps = FlightUIModeController.Instance.stagingQuadrant.GetComponentsInChildren<TextMeshProUGUI>();

            TextMeshProUGUI twrLabel = null;

            for (int i = tmps.Length - 1; i >= 0; i--)
            {
                if (tmps[i].gameObject.name == "Label_Pitch")
                {
                    twrLabel = Instantiate(tmps[i], twrGaugeObj.transform, false);
                    twrLabel.gameObject.name = "Label_TWR";
                    break;
                }
            }

            twrLabel.text = "<size=-1>TWR</size>";

            RectTransform twrRect = twrLabel.GetComponent<RectTransform>();

            twrRect.sizeDelta = new Vector2(5, 60);
            twrRect.anchoredPosition = new Vector2(8, 0);

            _twrGauge = twrGaugeObj.AddComponent<BasicDeltaV_TWRGauge>();

            _twrGauge.SetParent(twrGaugeObj);

            _twrGauge.gameObject.SetActive(BasicDeltaV_Settings.Instance.MoreBasicMode && !SimpleRestrictions);
        }

        private void Update()
        {
            if (!_loaded)
                return;
            
            if (!_readoutsAvailable && !_settings.DisableStockDeltaV)
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

            if (_updateDeltaV)
                UpdateStockDeltaV();

            if (HighLogic.LoadedSceneIsFlight)
                UpdateFlight();
            else if (HighLogic.LoadedSceneIsEditor)
                UpdateEditor();
        }

        private void UpdateEditor()
        {
            UpdatePanels();

            if (!_updateSimulator)
                return;

            _updateSimulator = false;

            try
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
            catch (Exception e)
            {
                BasicLogger.Exception(e, "BasicDeltaV.Update()");
            }
        }

        private void UpdateFlight()
        {
            try
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
            catch (Exception e)
            {
                BasicLogger.Exception(e, "BasicDeltaV.Update()");
            }

            UpdatePanels();
        }

        private void UpdatePanels()
        {
            if (!DisplayActive)
                return;

            if (_updatePanels)
            {
                _updatePanels = false;

                _panelRefreshTimer = 0;

                if (panelHandler != null)
                    panelHandler.UpdatePanels();

                return;
            }
            
            if (_panelRefreshTimer < _panelRefreshWait)
            {
                _panelRefreshTimer++;
                return;
            }

            _panelRefreshTimer = 0;

            if (panelHandler != null)
                panelHandler.UpdatePanels();
        }

        private void UpdateStockDeltaV()
        {
            Profiler.BeginSample("UpdateStockDV");
            if (!_settings.DisableStockDeltaV)
                return;
            
            _updateDeltaV = false;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (FlightGlobals.ActiveVessel == null)
                    return;
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (EditorLogic.fetch == null || EditorLogic.fetch.ship == null)
                    return;
            }

            if (_vesselDeltaV == null)
                return;

            if (stages == null)
                return;
            
            if (_vesselDeltaV.enabled)
                _vesselDeltaV.enabled = false;
            
            List<DeltaVStageInfo> stageInfo = _vesselDeltaV.stageInfo;
            
            stageInfo.Clear();
            
            for (int i = stages.Length - 1; i >= 0; i--)
            {
                if (HighLogic.LoadedSceneIsFlight)
                    stageInfo.Add(new DeltaVStageInfo((ShipConstruct)null, stages[i].number, _vesselDeltaV));
                else if (HighLogic.LoadedSceneIsEditor)
                    stageInfo.Add(new DeltaVStageInfo((Vessel)null, stages[i].number, _vesselDeltaV));
            }
            
            if (_vesselDeltaVTotalDVActual != null && _vesselDeltaVTotalDVASL != null && _vesselDeltaVTotalDVVac != null)
            {
                _vesselDeltaVTotalDVActual.SetValue(_vesselDeltaV, _lastStage.totalDeltaV);
                _vesselDeltaVTotalDVASL.SetValue(_vesselDeltaV, _lastStage.totalDeltaV);
                _vesselDeltaVTotalDVVac.SetValue(_vesselDeltaV, _lastStage.totalDeltaV);
            }

            _vesselDeltaV.lowestStageWithDeltaV = int.MaxValue;
            
            for (int i = _vesselDeltaV.stageInfo.Count - 1; i >= 0; i--)
            {
                DeltaVStageInfo info = _vesselDeltaV.stageInfo[i];
                
                Stage stage = GetStage(info.stage);

                if (stage == null)
                    continue;
                
                info.deltaVActual = (float)stage.deltaV;
                info.deltaVatASL = info.deltaVActual;
                info.deltaVinVac = info.deltaVActual;

                double gee = HighLogic.LoadedSceneIsEditor ? _currentBody.GeeASL : FlightGlobals.currentMainBody.GeeASL;

                info.TWRActual = (float)(stage.actualThrust / (stage.startMass * gee * GRAVITY));
                info.TWRASL = (float)(stage.thrust / (stage.startMass * gee * GRAVITY));
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

                if (stage.deltaV > 0)
                {
                    if (stage.number < _vesselDeltaV.lowestStageWithDeltaV)
                        _vesselDeltaV.lowestStageWithDeltaV = stage.number;
                }
            }
            Profiler.EndSample();
        }
        
        private void GetStageInfo()
        {
            stages = SimManager.Stages;

            if (stages != null && stages.Length > 0)
                _maxMach = stages[stages.Length - 1].maxMach;

            _lastStage = SimManager.LastStage;

            _updateDeltaV = true;
            _updatePanels = true;
        }

        private void TriggerSimulator()
        {
            _updateSimulator = true;
        }

        private void TriggerSimulator(int i)
        {
            _updateSimulator = true;
        }

        private void TriggerSimulator(Part p)
        {
            _updateSimulator = true;
        }

        private void TriggerSimulator(GameEvents.HostedFromToAction<bool, Part> action)
        {
            _updateSimulator = true;
        }

        private void TriggerSimulator(GameEvents.HostedFromToAction<PartResource, bool> action)
        {
            _updateSimulator = true;
        }

        private void TriggerSimulator(PartModule pm, AdjusterPartModuleBase adj)
        {
            _updateSimulator = true;
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

            _twrGauge.gameObject.SetActive(BasicDeltaV_Settings.Instance.MoreBasicMode && !SimpleRestrictions);

            if (panelHandler != null)
				panelHandler.RefreshPanels();
		}
        
        private void DeactivateDeltaV(Vessel v)
        {
            if (!_settings.DisableStockDeltaV)
                return;

            if (v != FlightGlobals.ActiveVessel)
                return;

            if (v.isEVA)
                return;

            if (v.VesselDeltaV == null)
                return;

            _vesselDeltaV = v.VesselDeltaV;
            _vesselDeltaV.StopAllCoroutines();

            v.VesselDeltaV.enabled = false;

            if (_vesselDeltaVFlag != null)
                _vesselDeltaVFlag.SetValue(_vesselDeltaV, true);
        }

        private void DeactivateEditorDeltaV(ShipConstruct v)
        {
            _updateSimulator = true;

            if (!_settings.DisableStockDeltaV)
                return;

            StartCoroutine(WaitForEditorShip());
        }

        private IEnumerator WaitForEditorShip()
        {
            while (EditorLogic.fetch == null)
                yield return null;

            while (EditorLogic.fetch.ship == null)
                yield return null;

            int timer = 0;

            while (EditorLogic.fetch.ship.vesselDeltaV == null)
            {
                timer++;

                if (timer > 120)
                    yield break;

                yield return null;
            }

            _vesselDeltaV = EditorLogic.fetch.ship.vesselDeltaV;
            
            if (_vesselDeltaV.enabled)
            {
                _vesselDeltaV.StopAllCoroutines();
                _vesselDeltaV.enabled = false;
            }

            if (_vesselDeltaVFlag != null)
                _vesselDeltaVFlag.SetValue(_vesselDeltaV, true);
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

			//BasicLogging("Crew Status Check - Simple Restrictions: {0}, Complex Restrictions: {1}", _simpleRestrictions, _complexRestrictions);

			if (_simpleRestrictions && _complexRestrictions)
				_readoutsAvailable = false;
			else
				_readoutsAvailable = true;
		}

		private void OnStageModify()
		{
            _updateSimulator = true;

            if (panelHandler != null)
				panelHandler.RefreshPanels();
		}

		private void OnStage(int stage)
		{
			CheckVesselCrew(FlightGlobals.ActiveVessel);

			appLauncher.ToggleButtonState(_readoutsAvailable);

            _twrGauge.gameObject.SetActive(BasicDeltaV_Settings.Instance.MoreBasicMode && !SimpleRestrictions);

            if (panelHandler != null)
				panelHandler.RefreshPanels();
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

                _updateSimulator = true;
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

                _updateSimulator = true;
            }
        }

        public bool VectoredThrust
        {
            get { return BasicDeltaV_Settings.Instance.VectoredThrust; }
            set
            {
                BasicDeltaV_Settings.Instance.VectoredThrust = value;

                SimManager.vectoredThrust = value;

                _updateSimulator = true;
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
                    _twrGauge.gameObject.SetActive(value && !SimpleRestrictions);
            }
        }

        public bool ShowBasicDVText
        {
            get { return BasicDeltaV_Settings.Instance.ShowDVText; }
            set
            {
                BasicDeltaV_Settings.Instance.ShowDVText = value;

                if (panelHandler != null)
                    panelHandler.ToggleDVText(value);
            }
        }

        public bool BasicShowCurrent
        {
            get { return BasicDeltaV_Settings.Instance.BasicCurrentOnly; }
            set
            {
                BasicDeltaV_Settings.Instance.BasicCurrentOnly = value;

                if (panelHandler != null)
                    panelHandler.RefreshPanels();
            }
        }

        public bool BasicShowStandard
        {
            get { return BasicDeltaV_Settings.Instance.BasicShowStandard; }
            set
            {
                BasicDeltaV_Settings.Instance.BasicShowStandard = value;

                if (panelHandler != null)
                    panelHandler.RefreshPanels();
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
            set
            {
                _atmosphereDepth = value;

                _updateSimulator = true;
            }
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
			set
            {
                _mach = value;

                _updateSimulator = true;
            }
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