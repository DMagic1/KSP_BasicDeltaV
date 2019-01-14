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
using UnityEngine;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;
using KSP.Localization;
using TMPro;
using Expansions.Missions.Adjusters;
using UnityEngine.Profiling;
using UnityEngine.Events;

namespace BasicDeltaV
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class BasicDeltaV : MonoBehaviour
    {
        private const string LABEL = "Basic ΔV";
        public const double GRAVITY = 9.80665;

        private static BasicDeltaV instance = null;

        private string _version;
        private float _mach;
        private bool _loaded;

        private static bool _rcsProcessed;
        
        private int _panelRefreshTimer;
        private const int _panelRefreshWait = 5;

        private int numberOfStages;
        private int stagesCount;
        private Stage[] stages;
        private Stage _lastStage;

        private bool _updateDeltaV;
        private bool _updateStages;
        private bool _updatePanels;

        private DictionaryValueList<StageGroup, BasicDeltaV_SliderGroup> _dvSliders = new DictionaryValueList<StageGroup, BasicDeltaV_SliderGroup>();

        private TMP_InputField _machSliderLabel;
        private Slider _machSlider;
        private DeltaVAppSituation _deltaVSituationApp;
        private CanvasGroup _deltaVMachControls;

        private DeltaVAppStageInfo _deltaVStageApp;

        private Dictionary<CelestialBody, int> _orderedBodies;

        private TMP_Dropdown.DropdownEvent _oldBodyEvent;

        private static bool _vesselDeltaVFlagAssigned;

        private static FieldInfo _vesselDeltaVFlag;
        private static FieldInfo _vesselDeltaVDirty;
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

            if (!_vesselDeltaVFlagAssigned)
            {
                AssignDeltaVFlag();
            }

            if (!_rcsProcessed)
                StartCoroutine(WaitForDVGlobals());

            SimManager.UpdateModSettings();
            SimManager.OnReady -= GetStageInfo;
            SimManager.OnReady += GetStageInfo;
        }

        private void Start()
        {
            //_settings = HighLogic.CurrentGame.Parameters.CustomParams<BasicDeltaV_GameParameters>();

            DeltaVGlobals.DisableStockSimluations();

            if (HighLogic.LoadedSceneIsEditor)
            {
                DeactivateEditorDeltaV(EditorLogic.fetch.ship);

                GameEvents.onEditorStarted.Add(EditorStart);
                
                GameEvents.onEditorLoad.Add(DeactivateEditorDeltaV);
                GameEvents.onEditorShipModified.Add(DeactivateEditorDeltaV);
                GameEvents.onPartPriorityChanged.Add(TriggerSimulator);
                GameEvents.onPartResourceListChange.Add(TriggerSimulator);
                GameEvents.onPartCrossfeedStateChange.Add(TriggerSimulator);
                GameEvents.onPartFuelLookupStateChange.Add(TriggerSimulator);

                BasicDeltaV_DeltaVSituationHandler.OnDVAppSituationAwake.AddListener(new UnityAction<DeltaVAppSituation>(DeltaVSituationAwake));
                BasicDeltaV_DeltaVSituationHandler.OnDVAppSituationStart.AddListener(new UnityAction<DeltaVAppSituation>(DeltaVSituationStart));
                BasicDeltaV_DeltaVSituationHandler.OnDVAppSituationDestroy.AddListener(new UnityAction<DeltaVAppSituation>(DeltaVSituationDestroy));
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselChange.Add(VesselChange);
                GameEvents.onStageActivate.Add(OnStage);
                GameEvents.onVesselWasModified.Add(VesselChange);
                GameEvents.onDockingComplete.Add(VesselChange);
                GameEvents.onVesselsUndocking.Add(VesselChange);
                GameEvents.onVesselGoOffRails.Add(VesselChange);

                BasicDeltaV_StageGroupHandler.OnStageGroupAwake.AddListener(new UnityAction<StageGroup>(StageAwake));
                BasicDeltaV_StageGroupHandler.OnStageGroupDestroy.AddListener(new UnityAction<StageGroup>(StageDestroy));

                BasicDeltaV_DeltaVAppStageHandler.OnDVAppStageStart.AddListener(new UnityAction<DeltaVAppStageInfo>(DeltaVAppStageStart));
                BasicDeltaV_DeltaVAppStageHandler.OnDVAppStageDestroy.AddListener(new UnityAction<DeltaVAppStageInfo>(DeltaVAppStageDestroy));

                DeactivateDeltaV(FlightGlobals.ActiveVessel);

                StartCoroutine(WaitForFlightController());
            }

            GameEvents.onEngineThrustPercentageChanged.Add(TriggerSimulator);
            GameEvents.onMultiModeEngineSwitchActive.Add(TriggerSimulator);
            GameEvents.StageManager.OnGUIStageAdded.Add(TriggerSimulator);
            GameEvents.StageManager.OnGUIStageRemoved.Add(TriggerSimulator);
            GameEvents.StageManager.OnStagingSeparationIndices.Add(TriggerSimulator);
            GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnStageModify);
            GameEvents.onPartModuleAdjusterAdded.Add(TriggerSimulator);
            GameEvents.onPartModuleAdjusterRemoved.Add(TriggerSimulator);
            GameEvents.onPartResourceFlowStateChange.Add(TriggerSimulator);

            Assembly assembly = AssemblyLoader.loadedAssemblies.GetByAssembly(Assembly.GetExecutingAssembly()).assembly;
            var ainfoV = Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
            switch (ainfoV == null)
            {
                case true: _version = ""; break;
                default: _version = ainfoV.InformationalVersion; break;
            }

            SimManager.vectoredThrust = BasicDeltaV_Settings.Instance.VectoredThrust;

            _loaded = true;
        }

        private void OnDestroy()
        {
            instance = null;

            GameEvents.onEngineThrustPercentageChanged.Remove(TriggerSimulator);
            GameEvents.onMultiModeEngineSwitchActive.Remove(TriggerSimulator);
            GameEvents.onPartResourceFlowStateChange.Remove(TriggerSimulator);

            GameEvents.onDockingComplete.Remove(VesselChange);
            GameEvents.onVesselsUndocking.Remove(VesselChange);
            GameEvents.onVesselGoOffRails.Remove(VesselChange);
            GameEvents.onVesselWasModified.Remove(VesselChange);

            GameEvents.onEditorStarted.Remove(EditorStart);

            GameEvents.onVesselChange.Remove(VesselChange);
            GameEvents.onStageActivate.Remove(OnStage);
            GameEvents.onEditorLoad.Remove(DeactivateEditorDeltaV);
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

            GameEvents.onDeltaVAppAtmosphereChanged.Remove(SetMachSituation);

            BasicDeltaV_StageGroupHandler.OnStageGroupAwake.RemoveListener(new UnityAction<StageGroup>(StageAwake));
            BasicDeltaV_StageGroupHandler.OnStageGroupDestroy.RemoveListener(new UnityAction<StageGroup>(StageDestroy));

            BasicDeltaV_DeltaVSituationHandler.OnDVAppSituationAwake.RemoveListener(new UnityAction<DeltaVAppSituation>(DeltaVSituationAwake));
            BasicDeltaV_DeltaVSituationHandler.OnDVAppSituationStart.RemoveListener(new UnityAction<DeltaVAppSituation>(DeltaVSituationStart));
            BasicDeltaV_DeltaVSituationHandler.OnDVAppSituationDestroy.RemoveListener(new UnityAction<DeltaVAppSituation>(DeltaVSituationDestroy));

            BasicDeltaV_DeltaVAppStageHandler.OnDVAppStageStart.RemoveListener(new UnityAction<DeltaVAppStageInfo>(DeltaVAppStageStart));
            BasicDeltaV_DeltaVAppStageHandler.OnDVAppStageDestroy.RemoveListener(new UnityAction<DeltaVAppStageInfo>(DeltaVAppStageDestroy));
            
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
                if (flag == "calcsDirty")
                    _vesselDeltaVDirty = flags[i];
                else if (flag == "_totalDeltaVActual")
                    _vesselDeltaVTotalDVActual = flags[i];
                else if (flag == "_totalDeltaVASL")
                    _vesselDeltaVTotalDVASL = flags[i];
                else if (flag == "_totalDeltaVVac")
                    _vesselDeltaVTotalDVVac = flags[i];
            }

            _vesselDeltaVFlagAssigned = true;
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

            _twrGauge.gameObject.SetActive(BasicDeltaV_Settings.Instance.ShowTWRGauge);
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

            if (_updateDeltaV)
                UpdateStockDeltaV();

            if (HighLogic.LoadedSceneIsFlight)
                UpdateFlight();
            else if (HighLogic.LoadedSceneIsEditor)
                UpdateEditor();
        }

        private void UpdateEditor()
        {
            if (_vesselDeltaVDirty == null)
                return;

            if (_vesselDeltaV == null)
                return;

            if (!(bool)_vesselDeltaVDirty.GetValue(_vesselDeltaV))
                return;

            _vesselDeltaVDirty.SetValue(_vesselDeltaV, false);
            
            try
            {
                SimManager.Gravity = DeltaVGlobals.DeltaVAppValues.body.GeeASL * GRAVITY;

                switch (DeltaVGlobals.DeltaVAppValues.situation)
                {
                    case DeltaVSituationOptions.Vaccum:
                        SimManager.Atmosphere = 0;
                        break;
                    case DeltaVSituationOptions.SeaLevel:
                        SimManager.Atmosphere = DeltaVGlobals.DeltaVAppValues.body.GetPressure(0) * PhysicsGlobals.KpaToAtmospheres;
                        break;
                    case DeltaVSituationOptions.Altitude:
                        SimManager.Atmosphere = DeltaVGlobals.DeltaVAppValues.body.GetPressure(AtmosphereDepth) * PhysicsGlobals.KpaToAtmospheres;
                        break;
                }

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
            if (!BasicDeltaV_Settings.Instance.ShowDVSliders)
            {
                _updatePanels = false;
                return;
            }

            if (_updatePanels)
            {
                _updatePanels = false;

                _panelRefreshTimer = 0;
                
                for (int i = _dvSliders.Count - 1; i >= 0; i--)
                {
                    Stage stage = GetStage(_dvSliders.At(i).StageIndex);

                    _dvSliders.At(i).UpdateSliderDV((float)stage.deltaV, (float)stage.stageStartDeltaV);
                }

                return;
            }

            if (_panelRefreshTimer < _panelRefreshWait)
            {
                _panelRefreshTimer++;
                return;
            }

            _panelRefreshTimer = 0;
            
            for (int i = _dvSliders.Count - 1; i >= 0; i--)
            {
                Stage stage = GetStage(_dvSliders.At(i).StageIndex);

                _dvSliders.At(i).UpdateSliderDV((float)stage.deltaV, (float)stage.stageStartDeltaV);
            }
        }

        private void UpdateStockDeltaV()
        {
            //Profiler.BeginSample("UpdateStockDV");
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

            DeltaVEngineStageSet engineStageSet = _vesselDeltaV.engineStageSet;

            engineStageSet.workingIndex = 0;
            engineStageSet.operatingIndex = 0;
            engineStageSet.UpdateStageInfo();
            
            if (_vesselDeltaVTotalDVActual != null && _vesselDeltaVTotalDVASL != null && _vesselDeltaVTotalDVVac != null)
            {
                _vesselDeltaVTotalDVActual.SetValue(_vesselDeltaV, _lastStage.totalDeltaV);
                _vesselDeltaVTotalDVASL.SetValue(_vesselDeltaV, _lastStage.totalDeltaV);
                _vesselDeltaVTotalDVVac.SetValue(_vesselDeltaV, _lastStage.totalDeltaV);
            }

            _vesselDeltaV.lowestStageWithDeltaV = int.MaxValue;

            var workingSet = engineStageSet.WorkingStageInfo;

            for (int i = workingSet.Count - 1; i >= 0; i--)
            {
                DeltaVStageInfo info = workingSet[i];

                Stage stage = GetStage(info.stage);

                if (stage == null)
                    continue;

                info.deltaVActual = (float)stage.deltaV;
                info.deltaVatASL = info.deltaVActual;
                info.deltaVinVac = info.deltaVActual;
                
                info.TWRActual = (float)(stage.actualThrustToWeight);
                info.TWRASL = (float)(stage.thrustToWeight);
                info.TWRVac = info.TWRASL;

                info.stageMass = (float)stage.mass;
                info.startMass = (float)stage.startMass;
                info.endMass = (float)stage.endMass;
                info.fuelMass = (float)stage.resourceMass;

                info.thrustActual = (float)stage.actualVectoredThrust;
                info.thrustASL = (float)stage.vectoredThrust;
                info.thrustVac = info.thrustASL;

                info.vectoredThrustActual = (float)stage.actualVectoredThrust;
                info.vectoredThrustASL = (float)stage.vectoredThrust;
                info.vectoredThrustVac = info.vectoredThrustASL;

                info.ispActual = (float)stage.isp;
                info.ispASL = info.ispActual;
                info.ispVac = info.ispActual;

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
            
            //Profiler.EndSample();

            if (_updateStages)
            {
                //Profiler.BeginSample("DV Event");
                GameEvents.onDeltaVCalcsCompleted.Fire();
                //Profiler.EndSample();

                _updateStages = false;
            }
        }

        private void GetStageInfo()
        {
            stages = SimManager.Stages;

            //if (stages != null && stages.Length > 0)
            //    _maxMach = stages[stages.Length - 1].maxMach;

            _lastStage = SimManager.LastStage;

            _updateDeltaV = true;
            _updatePanels = true;
        }

        private void StageAwake(StageGroup group)
        {
            if (HighLogic.LoadedSceneIsFlight)
                _dvSliders.Add(group, new BasicDeltaV_SliderGroup(group));
        }

        private void StageDestroy(StageGroup group)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (_dvSliders.Contains(group))
                {
                    _dvSliders[group] = null;
                    _dvSliders.Remove(group);
                }
            }
        }

        private void DeltaVSituationAwake(DeltaVAppSituation dvApp)
        {
            _deltaVSituationApp = dvApp;

            if (_deltaVSituationApp != null)
            {
                Slider atmosphereSlider = _deltaVSituationApp.GetComponentInChildren<Slider>();

                atmosphereSlider.onValueChanged.AddListener(new UnityAction<float>(AltitudeSliderChange));
                
                if (atmosphereSlider != null)
                {
                    GameObject atmosphereControls = atmosphereSlider.transform.parent.parent.gameObject;

                    if (atmosphereControls != null)
                    {
                        _deltaVMachControls = Instantiate(atmosphereControls).GetComponent<CanvasGroup>();
                        _deltaVMachControls.transform.SetParent(atmosphereControls.transform.parent, false);
                        _deltaVMachControls.transform.SetAsLastSibling();
                        _deltaVMachControls.name = "MachControls";

                        _machSliderLabel = _deltaVMachControls.GetComponentInChildren<TMP_InputField>();

                        _machSliderLabel.onValueChanged.RemoveAllListeners();
                        _machSliderLabel.onValueChanged.AddListener(new UnityAction<string>(MachTextInputChanged));

                        var text = _deltaVMachControls.GetComponentsInChildren<TextMeshProUGUI>();

                        TextMeshProUGUI machLabel = text[0];
                        machLabel.text = Localizer.Format("#autoLOC_357350", "");

                        TextMeshProUGUI machText = text[3];
                        machText.text = "";

                        LayoutElement machTextLayout = machText.GetComponent<LayoutElement>();
                        machTextLayout.minWidth = 0;
                        machTextLayout.preferredWidth = 0;

                        _machSlider = _deltaVMachControls.GetComponentInChildren<Slider>();
                        _machSlider.maxValue = 2.5f;
                        _machSlider.value = 0;
                        _machSlider.wholeNumbers = false;

                        _machSlider.onValueChanged.RemoveAllListeners();
                        _machSlider.onValueChanged.AddListener(new UnityAction<float>(MachSliderChanged));

                        var toggleGroups = _deltaVSituationApp.GetComponentsInChildren<ToggleGroup>();

                        int situation = EditorDriver.editorFacility == EditorFacility.VAB ? BasicDeltaV_Settings.Instance.VABSituation : BasicDeltaV_Settings.Instance.SPHSituation;

                        for (int i = toggleGroups.Length - 1; i >= 0; i--)
                        {
                            if (toggleGroups[i].name == "AtmosphereButtons")
                            {
                                var toggles = toggleGroups[i].GetComponentsInChildren<Toggle>();

                                for (int j = toggles.Length - 1; j >= 0; j--)
                                {
                                    if (j == situation)
                                        toggles[j].isOn = true;
                                    else
                                        toggles[j].isOn = false;
                                }
                                
                                BasicLogging("Setting dv toggle situation: {0}", situation);

                                break;
                            }
                        }

                        _deltaVSituationApp.SetAtmosphereSituation((DeltaVSituationOptions)situation);

                        DeltaVGlobals.DeltaVAppValues.situation = (DeltaVSituationOptions)situation;

                        GameEvents.onDeltaVAppAtmosphereChanged.Add(SetMachSituation);

                        SetMachSituation((DeltaVSituationOptions)situation);
                    }
                }
            }
        }

        private void EditorStart()
        {
            if (_deltaVSituationApp == null)
                return;

            var toggleGroups = _deltaVSituationApp.GetComponentsInChildren<ToggleGroup>();

            int situation = EditorDriver.editorFacility == EditorFacility.VAB ? BasicDeltaV_Settings.Instance.VABSituation : BasicDeltaV_Settings.Instance.SPHSituation;

            for (int i = toggleGroups.Length - 1; i >= 0; i--)
            {
                if (toggleGroups[i].name == "AtmosphereButtons")
                {
                    var toggles = toggleGroups[i].GetComponentsInChildren<Toggle>();

                    for (int j = toggles.Length - 1; j >= 0; j--)
                    {
                        if (j == situation)
                            toggles[j].isOn = true;
                        else
                            toggles[j].isOn = false;
                    }

                    BasicLogging("Setting dv toggle situation after switch: {0}", situation);

                    break;
                }
            }

            _deltaVSituationApp.SetAtmosphereSituation((DeltaVSituationOptions)situation);

            DeltaVGlobals.DeltaVAppValues.situation = (DeltaVSituationOptions)situation;

            GameEvents.onDeltaVAppAtmosphereChanged.Add(SetMachSituation);

            SetMachSituation((DeltaVSituationOptions)situation);
        }

        private void DeltaVSituationStart(DeltaVAppSituation dvApp)
        {
            if (_deltaVSituationApp == null)
                _deltaVSituationApp = dvApp;

            if (_deltaVSituationApp != dvApp)
                return;

            GenericAppFrame frame = _deltaVSituationApp.GetComponentInParent<GenericAppFrame>();

            TextMeshProUGUI headerText = frame.GetComponentInChildren<TextMeshProUGUI>();

            TextMeshProUGUI basicDVText = Instantiate(headerText).GetComponent<TextMeshProUGUI>();
            basicDVText.transform.SetParent(headerText.transform.parent, false);
            basicDVText.rectTransform.anchorMin = headerText.rectTransform.anchorMin;
            basicDVText.rectTransform.anchorMax = headerText.rectTransform.anchorMax;
            basicDVText.rectTransform.pivot = headerText.rectTransform.pivot;
            basicDVText.rectTransform.sizeDelta = headerText.rectTransform.sizeDelta;
            basicDVText.rectTransform.anchoredPosition3D = headerText.rectTransform.anchoredPosition3D;

            basicDVText.alignment = TextAlignmentOptions.MidlineRight;
            basicDVText.text = string.Format("{0}: {1}", LABEL, _version);

            StartCoroutine(WaitForDVBodyList());
        }

        private IEnumerator WaitForDVBodyList()
        {
            TMP_Dropdown bodySelectionDropdown = _deltaVSituationApp.GetComponentInChildren<TMP_Dropdown>();

            while (bodySelectionDropdown.options.Count < FlightGlobals.Bodies.Count)
            {
                yield return null;
            }

            bodySelectionDropdown.ClearOptions();

            _oldBodyEvent = bodySelectionDropdown.onValueChanged;

            bodySelectionDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            bodySelectionDropdown.onValueChanged.RemoveAllListeners();

            _orderedBodies = OrderedBodies;

            for (int i = 0; i < _orderedBodies.Count; i++)
            {
                var body = _orderedBodies.ElementAt(i);

                bodySelectionDropdown.options.Add(new TMP_Dropdown.OptionData(string.Format("{0}{1}"
                    , GetSpaces(body.Value), body.Key.displayName.LocalizeRemoveGender())));

                if (body.Key.name == FlightGlobals.GetHomeBodyName())
                {
                    bodySelectionDropdown.value = i;
                }
            }

            bodySelectionDropdown.onValueChanged.AddListener(new UnityAction<int>(SetBodySelection));
        }

        private string GetSpaces(int spaces)
        {
            return new string(' ', spaces);
        }

        private void DeltaVSituationDestroy(DeltaVAppSituation dvApp)
        {
            if (dvApp == _deltaVSituationApp)
                _deltaVSituationApp = null;
        }

        private void SetBodySelection(int value)
        {
            if (value > _orderedBodies.Count)
                return;

            var body = _orderedBodies.ElementAt(value);

            _oldBodyEvent.Invoke(body.Key.flightGlobalsIndex);
        }

        private void MachTextInputChanged(string input)
        {
            if (_machSlider == null)
                return;

            float value = 0;

            if (float.TryParse(input, out value))
            {
                _machSlider.value = Mathf.Clamp(value, 0, 2.5f);
            }
        }

        private void MachSliderChanged(float value)
        {
            _machSliderLabel.text = value.ToString("N2");

            _mach = value;

            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void AltitudeSliderChange(float value)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void SetMachSituation(DeltaVSituationOptions situation)
        {
            if (EditorDriver.editorFacility == EditorFacility.VAB)
                BasicDeltaV_Settings.Instance.VABSituation = (int)situation;
            else
                BasicDeltaV_Settings.Instance.SPHSituation = (int)situation;

            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;

            if (_deltaVMachControls == null)
                return;

            if (situation == DeltaVSituationOptions.Vaccum)
            {
                _deltaVMachControls.alpha = 0.4f;
                _deltaVMachControls.interactable = false;
            }
            else
            {
                if (_deltaVSituationApp == null)
                {
                    _deltaVMachControls.alpha = 0.4f;
                    _deltaVMachControls.interactable = false;

                    return;
                }

                if (!_deltaVSituationApp.SelectedBody.atmosphere)
                {
                    _deltaVMachControls.alpha = 0.4f;
                    _deltaVMachControls.interactable = false;

                    return;
                }

                if (_deltaVSituationApp.SelectedBody == Planetarium.fetch.Sun)
                {
                    _deltaVMachControls.alpha = 0.4f;
                    _deltaVMachControls.interactable = false;

                    return;
                }

                _deltaVMachControls.alpha = 1;
                _deltaVMachControls.interactable = true;
            }
        }

        private void DeltaVAppStageStart(DeltaVAppStageInfo appStage)
        {
            _deltaVStageApp = appStage;

            if (_deltaVStageApp != null)
            {
                StartCoroutine(WaitForAppToggles());

                GenericAppFrame frame = _deltaVStageApp.GetComponentInParent<GenericAppFrame>();

                TextMeshProUGUI headerText = frame.GetComponentInChildren<TextMeshProUGUI>();

                TextMeshProUGUI basicDVText = Instantiate(headerText).GetComponent<TextMeshProUGUI>();
                basicDVText.transform.SetParent(headerText.transform.parent, false);
                basicDVText.rectTransform.anchorMin = headerText.rectTransform.anchorMin;
                basicDVText.rectTransform.anchorMax = headerText.rectTransform.anchorMax;
                basicDVText.rectTransform.pivot = headerText.rectTransform.pivot;
                basicDVText.rectTransform.sizeDelta = headerText.rectTransform.sizeDelta;
                basicDVText.rectTransform.anchoredPosition3D = headerText.rectTransform.anchoredPosition3D;

                basicDVText.alignment = TextAlignmentOptions.MidlineRight;
                basicDVText.text = string.Format("{0}: {1}", LABEL, _version);
            }
        }

        private IEnumerator WaitForAppToggles()
        {
            GridLayoutGroup grid = _deltaVStageApp.GetComponentInChildren<GridLayoutGroup>();

            if (grid != null)
            {
                while (grid.transform.childCount < DeltaVGlobals.DeltaVAppValues.infoLines.Count)
                {
                    yield return null;
                }

                DeltaVAppStageInfoToggle toggle = _deltaVStageApp.GetComponentInChildren<DeltaVAppStageInfoToggle>();

                if (toggle != null)
                {
                    DeltaVAppStageInfoToggle twrToggle = Instantiate(toggle).GetComponent<DeltaVAppStageInfoToggle>();
                    twrToggle.transform.SetParent(toggle.transform.parent);
                    twrToggle.transform.SetAsLastSibling();
                    twrToggle.gameObject.name = "DeltaVAppStageInfoTWRToggle";
                    twrToggle.transform.localScale = Vector3.one;

                    RectTransform twrRect = (RectTransform)twrToggle.transform;

                    twrRect.anchoredPosition3D = new Vector3(twrRect.anchoredPosition3D.x, twrRect.anchoredPosition3D.y, -624);

                    twrToggle.checkbox.onValueChanged.RemoveAllListeners();
                    twrToggle.checkbox.isOn = BasicDeltaV_Settings.Instance.ShowTWRGauge;
                    twrToggle.checkbox.onValueChanged.AddListener(new UnityAction<bool>(ToggleTWRGauge));

                    twrToggle.label.text = "TWR Gauge";

                    Destroy(twrToggle);

                    DeltaVAppStageInfoToggle dvSliderToggle = Instantiate(toggle).GetComponent<DeltaVAppStageInfoToggle>();
                    dvSliderToggle.transform.SetParent(toggle.transform.parent);
                    dvSliderToggle.transform.SetAsLastSibling();
                    dvSliderToggle.gameObject.name = "DeltaVAppStageInfoDVSliderToggle";
                    dvSliderToggle.transform.localScale = Vector3.one;

                    RectTransform dvRect = (RectTransform)dvSliderToggle.transform;

                    dvRect.anchoredPosition3D = new Vector3(dvRect.anchoredPosition3D.x, dvRect.anchoredPosition3D.y, -624);

                    dvSliderToggle.checkbox.onValueChanged.RemoveAllListeners();
                    dvSliderToggle.checkbox.isOn = BasicDeltaV_Settings.Instance.ShowDVSliders;
                    dvSliderToggle.checkbox.onValueChanged.AddListener(new UnityAction<bool>(ToggleDVSliders));

                    dvSliderToggle.label.text = "ΔV Sliders";

                    Destroy(dvSliderToggle);
                }
            }
        }

        private void DeltaVAppStageDestroy(DeltaVAppStageInfo appStage)
        {
            if (appStage == _deltaVStageApp)
                _deltaVStageApp = null;
        }

        private void ToggleTWRGauge(bool isOn)
        {
            if (_twrGauge != null)
                _twrGauge.gameObject.SetActive(isOn);

            BasicDeltaV_Settings.Instance.ShowTWRGauge = isOn;
        }

        private void ToggleDVSliders(bool isOn)
        {
            for (int i = _dvSliders.Count - 1; i >= 0; i--)
            {
                _dvSliders.At(i).ToggleSliderActivation(isOn);
            }

            BasicDeltaV_Settings.Instance.ShowDVSliders = isOn;
        }
        
        private IEnumerator WaitForDVGlobals()
        {
            while (!DeltaVGlobals.ready)
                yield return null;

            DeltaVAppValues dvAppValues = DeltaVGlobals.DeltaVAppValues;

            dvAppValues.infoLines.Add(new DeltaVAppValues.InfoLine(
                "RCS", string.Format("{0} {1}", Localizer.Format("#autoLOC_6003004"), Localizer.Format("#autoLOC_8003206")), Localizer.Format("#autoLOC_6003004")
                , ((DeltaVStageInfo s, DeltaVSituationOptions situation) => GetStageRCS(s).ToString("0"))
                , "m/s" , BasicDeltaV_Settings.Instance.ShowRCS));

            _rcsProcessed = true;
        }
        
        private float GetStageRCS(DeltaVStageInfo s)
        {
            Stage stage = GetStage(s.stage);

            if (stage == null)
                return 0;
            else
                return (float)stage.RCSdeltaVStart;
        }

        private void TriggerSimulator()
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);
            
            _updateStages = true;
        }

        private void TriggerSimulator(int i)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void TriggerSimulator(Part p)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void TriggerSimulator(MultiModeEngine m)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void TriggerSimulator(ModuleEngines m)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void TriggerSimulator(GameEvents.HostedFromToAction<bool, Part> action)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void TriggerSimulator(GameEvents.HostedFromToAction<PartResource, bool> action)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void TriggerSimulator(PartModule pm, AdjusterPartModuleBase adj)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void VesselChange(GameEvents.FromToAction<Part, Part> PP)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (PP.from.vessel == null || PP.to.vessel == null)
                return;

            if (PP.from.vessel == FlightGlobals.ActiveVessel)
                DeactivateDeltaV(PP.from.vessel);
            else if (PP.to.vessel == FlightGlobals.ActiveVessel)
                DeactivateDeltaV(PP.to.vessel);
        }

        private void VesselChange(Vessel v, Vessel v2)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (v == null || v2 == null)
                return;

            if (v == FlightGlobals.ActiveVessel)
                DeactivateDeltaV(v);
            else if (v2 == FlightGlobals.ActiveVessel)
                DeactivateDeltaV(v2);
        }

        private void VesselChange(Vessel v)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (v == null)
                return;

            if (v != FlightGlobals.ActiveVessel)
                return;

            DeactivateDeltaV(v);
        }

        private void DeactivateDeltaV(Vessel v)
        {
            if (v != FlightGlobals.ActiveVessel)
                return;

            if (v.isEVA)
                return;

            if (v.VesselDeltaV == null)
                return;

            _vesselDeltaV = v.VesselDeltaV;

            if (v.VesselDeltaV.enabled)
            {
                _vesselDeltaV.StopAllCoroutines();
                _vesselDeltaV.DisableStockSimluation();
                v.VesselDeltaV.enabled = false;
            }

            _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            if (_vesselDeltaVFlag != null)
                _vesselDeltaVFlag.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void DeactivateEditorDeltaV(ShipConstruct ship, CraftBrowserDialog.LoadType loadType)
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;
            
            StartCoroutine(WaitForEditorShip(ship));
        }

        private void DeactivateEditorDeltaV(ShipConstruct v)
        {
            StartCoroutine(WaitForEditorShip(v));
        }
        
        private IEnumerator WaitForEditorShip(ShipConstruct ship)
        {
            if (ship == null)
                yield break;

            while (EditorLogic.fetch == null)
                yield return null;

            while (EditorLogic.fetch.ship == null)
                yield return null;
            
            if (EditorLogic.fetch.ship != ship)
                yield break;

            int timer = 0;

            while (EditorLogic.fetch.ship.vesselDeltaV == null)
            {
                timer++;

                if (timer > 120)
                    yield break;

                yield return null;
            }
            
            yield return new WaitForSeconds(0.75f);

            DeactivateEditorDeltaV();
        }

        private void DeactivateEditorDeltaV()
        {
            _vesselDeltaV = EditorLogic.fetch.ship.vesselDeltaV;

            if (_vesselDeltaV.enabled)
            {
                _vesselDeltaV.StopAllCoroutines();
                _vesselDeltaV.enabled = false;
            }

            if (_vesselDeltaVFlag != null)
                _vesselDeltaVFlag.SetValue(_vesselDeltaV, true);
            
            _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void OnStageModify()
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        private void OnStage(int stage)
        {
            if (_vesselDeltaV != null)
                _vesselDeltaVDirty.SetValue(_vesselDeltaV, true);

            _updateStages = true;
        }

        public CelestialBody CurrentCelestialBody
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight)
                    return FlightGlobals.currentMainBody;
                else
                    return DeltaVGlobals.DeltaVAppValues.body;
            }
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

        public float AtmosphereDepth
        {
            get { return (float)DeltaVGlobals.DeltaVAppValues.altitude; }
        }

        private Dictionary<CelestialBody, int> OrderedBodies
        {
            get
            {
                Dictionary<CelestialBody, int> orderedBodies = new Dictionary<CelestialBody, int>();

                var planets = FlightGlobals.Bodies.Where(b => b.referenceBody == Planetarium.fetch.Sun && b.referenceBody != b);

                var orderedPlanets = planets.OrderBy(p => p.orbit.semiMajorAxis).ToList();

                for (int i = 0; i < orderedPlanets.Count; i++)
                {
                    CelestialBody body = orderedPlanets[i];

                    orderedBodies.Add(body, 0);

                    for (int j = 0; j < body.orbitingBodies.Count; j++)
                    {
                        CelestialBody moon = body.orbitingBodies[j];

                        orderedBodies.Add(moon, 2);

                        for (int k = 0; k < moon.orbitingBodies.Count; k++)
                        {
                            CelestialBody subMoon = moon.orbitingBodies[k];

                            orderedBodies.Add(subMoon, 4);

                            for (int l = 0; l < subMoon.orbitingBodies.Count; l++)
                            {
                                CelestialBody subSubMoon = subMoon.orbitingBodies[l];

                                orderedBodies.Add(subSubMoon, 6);

                                for (int m = 0; m < subSubMoon.orbitingBodies.Count; m++)
                                {
                                    CelestialBody subSubSubMoon = subSubMoon.orbitingBodies[m];

                                    orderedBodies.Add(subSubSubMoon, 6);

                                    for (int n = 0; n < subSubSubMoon.orbitingBodies.Count; n++)
                                    {
                                        CelestialBody subSubSubSubMoon = subSubSubMoon.orbitingBodies[n];

                                        orderedBodies.Add(subSubSubSubMoon, 6);
                                    }
                                }
                            }
                        }
                    }
                }

                orderedBodies.Add(Planetarium.fetch.Sun, 0);

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
    }
}