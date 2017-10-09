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
        private BasicDeltaV_Panel panel;
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

		public Stage Stage
		{
			get { return stage; }
			set { stage = value; }
		}

        public BasicDeltaV_StagePanel(RectTransform rect, int i, bool right, bool display)
        {
			parent = rect;
			index = i;
			rightPos = right;

			stage = BasicDeltaV.Instance.GetStage(i);

            StartModules();

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

			StartModules();

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

        private void StartModules()
		{
			modules.Clear();

			if (BasicDeltaV_Settings.Instance.ShowDeltaV && !BasicDeltaV.Instance.ComplexRestrictions)
				modules.Add(new BasicDeltaV_DeltaV("ΔV", this));

			if (BasicDeltaV_Settings.Instance.ShowTWR && stage != null && stage.deltaV > 0 && !BasicDeltaV.Instance.SimpleRestrictions)
				modules.Add(new BasicDeltaV_TWR("TWR", this));

			if (BasicDeltaV_Settings.Instance.ShowMass && !BasicDeltaV.Instance.SimpleRestrictions)
				modules.Add(new BasicDeltaV_Mass("Mass", this));

			if (BasicDeltaV_Settings.Instance.ShowBurnTime && stage != null && stage.deltaV > 0 && !BasicDeltaV.Instance.ComplexRestrictions)
				modules.Add(new BasicDeltaV_BurnTime("Burn Time", this));

			if (BasicDeltaV_Settings.Instance.ShowISP && stage != null && stage.deltaV > 0 && !BasicDeltaV.Instance.ComplexRestrictions)
				modules.Add(new BasicDeltaV_ISP("ISP", this));

			if (BasicDeltaV_Settings.Instance.ShowThrust && stage != null && stage.deltaV > 0 && !BasicDeltaV.Instance.SimpleRestrictions)
				modules.Add(new BasicDeltaV_Thrust("Thrust", this));
        }

        private void CreatePanel(bool right, bool display)
        {
            if (BasicDeltaV_Loader.PanelPrefab == null || modules == null || modules.Count == 0)
                return;

            panel = GameObject.Instantiate(BasicDeltaV_Loader.PanelPrefab).GetComponent<BasicDeltaV_Panel>();

            if (panel == null)
                return;

            panel.transform.SetParent(parent, false);
			panel.transform.SetAsFirstSibling();

            panel.setPanel(modules, BasicDeltaV_Settings.Instance.PanelAlpha, HighLogic.LoadedSceneIsFlight);
			
			if (right)
				panel.MovePanel(true);

			if (!display)
				panel.gameObject.SetActive(false);
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

		public void SetVisible(bool isOn)
		{
			if (panel == null)
				return;

			if (panel.gameObject.activeSelf && !isOn)
				panel.gameObject.SetActive(false);
			else if (!panel.gameObject.activeSelf && isOn)
				panel.gameObject.SetActive(true);
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