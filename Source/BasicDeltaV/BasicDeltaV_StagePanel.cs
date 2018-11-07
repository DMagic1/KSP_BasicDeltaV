#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_StagePanel - Storage class for holding information on the readout module panels
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
using BasicDeltaV.Unity.Interface;
using BasicDeltaV.Unity.Unity;
using BasicDeltaV.Modules;
using BasicDeltaV.Simulation;
using UnityEngine;
using KSP.UI.Screens;

namespace BasicDeltaV
{
    public class BasicDeltaV_StagePanel
    {
        private List<IBasicModule> modules = new List<IBasicModule>();

		private RectTransform parent;
        private BasicDeltaV_SimplePanel panel;
        private BasicDeltaV_SimpleDeltaVGauge simpleGauge;
		private Stage stage;
		private int index;
		private bool rightPos;
	
		public bool PanelRight
		{
			get
			{
				if (panel == null)
					return rightPos;

				return panel.RightPos;
			}
			set { rightPos = value; }
		}

        public bool CurrentStage
        {
            get { return index == StageManager.LastStage; }
        }

		public Stage Stage
		{
			get { return stage; }
			set
            {
                stage = value;

                if (value != null)
                    index = stage.number;
            }
		}

        public BasicDeltaV_StagePanel(RectTransform rect, int i, bool right, bool display, StageIconInfoBox info)
        {
			parent = rect;
			index = i;
			rightPos = right;

			stage = BasicDeltaV.Instance.GetStage(i);

            StartModules();

            if (info != null)
            {
                simpleGauge = info.gameObject.AddComponent<BasicDeltaV_SimpleDeltaVGauge>();

                bool displayGauge = BasicDeltaV_Settings.Instance.MoreBasicMode;

                if (BasicDeltaV_Settings.Instance.BasicCurrentOnly && index != StageManager.LastStage)
                    displayGauge = false;

                if (BasicDeltaV.Instance.ComplexRestrictions)
                    displayGauge = false;

                simpleGauge.Initialize(info, displayGauge && stage != null && stage.stageStartDeltaV > 0, this);
            }

            CreatePanel(right, display && stage != null && stage.deltaV > 0);
        }

		public void RefreshModules()
		{
            //BasicDeltaV.BasicLogging("Panel Refresh - Index: {0}/{1} - Status: {2} - Position: {3}", index, StageManager.LastStage, panel == null ? "Null" : "Valid", rightPos);

			if (panel != null)
				panel.Close();

			panel = null;

			bool display = true;

			if (HighLogic.LoadedSceneIsFlight && BasicDeltaV_Settings.Instance.ShowCurrentStageOnly && index != StageManager.LastStage)
				display = false;

            bool displayGauge = BasicDeltaV_Settings.Instance.MoreBasicMode;

            if (BasicDeltaV_Settings.Instance.BasicCurrentOnly && index != StageManager.LastStage)
                displayGauge = false;

            if (BasicDeltaV.Instance.ComplexRestrictions)
                displayGauge = false;

            if (simpleGauge != null)
                simpleGauge.Expand(displayGauge && stage != null && stage.stageStartDeltaV > 0);

            if (!BasicDeltaV.Instance.DisplayActive)
                return;

			StartModules();

            if (modules.Count <= 0)
                return;

			if (panel != null)
				rightPos = panel.RightPos;

			CreatePanel(rightPos, stage != null && display && stage.deltaV > 0);
		}

		public void Destroy()
		{
			if (panel != null)
				panel.Close();
		}

		public int Index
		{
			get { return index; }
			set { index = value; }
		}

		public void ToggleNoDVModules(bool isOn)
		{
			if (panel != null)
				panel.SetNoDVMode(isOn);
		}

        private void StartModules()
		{
			modules.Clear();

            if (HighLogic.LoadedSceneIsFlight && BasicDeltaV_Settings.Instance.MoreBasicMode && !BasicDeltaV_Settings.Instance.BasicShowStandard)
                return;

            bool active = HighLogic.LoadedSceneIsFlight && index == StageManager.LastStage;
            
            if (HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsFlight && !BasicDeltaV_Settings.Instance.MoreBasicMode))
            {
                if (BasicDeltaV_Settings.Instance.ShowDeltaV && !BasicDeltaV.Instance.ComplexRestrictions)
                    modules.Add(new BasicDeltaV_DeltaV("ΔV: ", this));

                if (BasicDeltaV_Settings.Instance.ShowTWR && !BasicDeltaV.Instance.SimpleRestrictions)
                    modules.Add(new BasicDeltaV_TWR("TWR: ", active, this));
            }

			if (BasicDeltaV_Settings.Instance.ShowMass && !BasicDeltaV.Instance.SimpleRestrictions)
				modules.Add(new BasicDeltaV_Mass("Mass: ", this));

			if (BasicDeltaV_Settings.Instance.ShowBurnTime && !BasicDeltaV.Instance.ComplexRestrictions)
				modules.Add(new BasicDeltaV_BurnTime("Burn Time: ", this));

			if (BasicDeltaV_Settings.Instance.ShowISP  && !BasicDeltaV.Instance.ComplexRestrictions)
				modules.Add(new BasicDeltaV_ISP("ISP: ", this));

			if (BasicDeltaV_Settings.Instance.ShowThrust && !BasicDeltaV.Instance.SimpleRestrictions)
				modules.Add(new BasicDeltaV_Thrust("Thrust: ", active, this));
        }

        private void CreatePanel(bool right, bool display)
        {
            if (BasicDeltaV_Loader.SimplePanelPrefab == null || modules == null || modules.Count == 0)
                return;

            panel = GameObject.Instantiate(BasicDeltaV_Loader.SimplePanelPrefab, parent, false).GetComponent<BasicDeltaV_SimplePanel>();
            
			panel.transform.SetAsFirstSibling();

            panel.setPanel(modules, BasicDeltaV_Settings.Instance.PanelAlpha, HighLogic.LoadedSceneIsFlight);
			
			if (right)
				panel.MovePanel(true);

            if (!display)
            {
                panel.Unregister();
                panel.gameObject.SetActive(false);
            }
        }

		public void MovePanel(bool right)
		{
			if (panel != null)
				panel.MovePanel(right);

			rightPos = right;
		}

        public void SetAlpha(float alpha)
        {
            if (panel != null)
                panel.SetAlpha(alpha);
        }

        public void ToggleDVText(bool isOn)
        {
            if (simpleGauge != null)
                simpleGauge.ToggleText(isOn);
        }

        public void SetVisible(bool isOn)
        {
            if (panel != null)
            {
                if (BasicDeltaV_Settings.Instance.ShowCurrentStageOnly && index != StageManager.LastStage)
                    isOn = false;

                if (panel.gameObject.activeSelf && !isOn)
                {
                    panel.Unregister();
                    panel.gameObject.SetActive(false);
                }
                else if (!panel.gameObject.activeSelf && isOn)
                {
                    panel.gameObject.SetActive(true);
                    panel.Register();
                }
            }

            if (simpleGauge != null)
            {
                bool displayGauge = BasicDeltaV_Settings.Instance.MoreBasicMode;

                if (BasicDeltaV_Settings.Instance.BasicCurrentOnly && index != StageManager.LastStage)
                    displayGauge = false;

                if (BasicDeltaV.Instance.ComplexRestrictions)
                    displayGauge = false;

                if (!isOn && stage != null && stage.deltaV <= 0)
                    isOn = true;

                simpleGauge.Expand(displayGauge && isOn && stage != null && stage.stageStartDeltaV > 0);
            }
        }

		public void HidePanel(bool isOn)
		{
			if (panel == null)
				return;

			if (isOn)
				panel.OnHide();
			else
				panel.OnUnHide();
		}
    }
}