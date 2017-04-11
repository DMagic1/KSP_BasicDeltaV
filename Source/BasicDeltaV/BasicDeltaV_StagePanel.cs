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

namespace BasicDeltaV
{
    public class BasicDeltaV_StagePanel
    {
        private List<IBasicModule> modules = new List<IBasicModule>();

		private RectTransform parent;
        private BasicDeltaV_Panel panel;
		private Stage stage;
		private int index;

		public Stage Stage
		{
			get { return stage; }
			set { stage = value; }
		}

        public BasicDeltaV_StagePanel(RectTransform rect, int i)
        {
			parent = rect;
			index = i;

			stage = BasicDeltaV.Instance.GetStage(i);

            StartModules();

            CreatePanel();
        }

		public void RefreshModules()
		{
			if (panel != null)
				panel.Close();

			panel = null;

			StartModules();

			CreatePanel();
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

			if (BasicDeltaV_Settings.Instance.ShowDeltaV)
				modules.Add(new BasicDeltaV_DeltaV("dV", this));

			if (BasicDeltaV_Settings.Instance.ShowTWR)
				modules.Add(new BasicDeltaV_TWR("TWR", this));

			if (BasicDeltaV_Settings.Instance.ShowMass)
				modules.Add(new BasicDeltaV_Mass("Mass", this));

			if (BasicDeltaV_Settings.Instance.ShowBurnTime)
				modules.Add(new BasicDeltaV_BurnTime("Burn Time", this));

			if (BasicDeltaV_Settings.Instance.ShowISP)
				modules.Add(new BasicDeltaV_ISP("ISP", this));

			if (BasicDeltaV_Settings.Instance.ShowThrust)
				modules.Add(new BasicDeltaV_Thrust("Thrust", this));
        }

        private void CreatePanel()
        {
            if (BasicDeltaV_Loader.PanelPrefab == null || modules == null || modules.Count == 0)
                return;

            panel = GameObject.Instantiate(BasicDeltaV_Loader.PanelPrefab).GetComponent<BasicDeltaV_Panel>();

            if (panel == null)
                return;

            panel.transform.SetParent(parent, false);
			panel.transform.SetAsFirstSibling();

            panel.setPanel(modules, BasicDeltaV_Settings.Instance.PanelAlpha);

			panel.gameObject.SetActive(false);
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