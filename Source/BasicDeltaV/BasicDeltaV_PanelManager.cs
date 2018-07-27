#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_PanelHandler - Script for controlling the staging panels
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
using System.Reflection;
using System.Linq;
using BasicDeltaV.Unity.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using KSP.UI;
using KSP.UI.Screens;

namespace BasicDeltaV
{
	public class BasicDeltaV_PanelManager : MonoBehaviour
	{
		//private Dictionary<StageGroup, BasicDeltaV_StagePanel> panels = new Dictionary<StageGroup, BasicDeltaV_StagePanel>();
		private DictionaryValueList<StageGroup, BasicDeltaV_StagePanel> panels = new DictionaryValueList<StageGroup, BasicDeltaV_StagePanel>();
		private List<ApplicationLauncherButton> buttons = new List<ApplicationLauncherButton>();

		private bool inApp;
		private bool contractHover;
		private bool engineerHover;
		private bool contractsPinned;
		private bool engineerPinned;

		private void Awake()
		{
			BasicDeltaV_StageGroupHandler.OnStageGroupAwake.AddListener(new UnityAction<StageGroup>(StageAwake));
			BasicDeltaV_StageGroupHandler.OnStageGroupDestroy.AddListener(new UnityAction<StageGroup>(StageDestroy));

			if (HighLogic.LoadedSceneIsEditor)
				BasicDeltaV_UIAppHandler.OnUIAppAwake.AddListener(new UnityAction<ApplicationLauncherButton, UIApp>(UIAppAwake));
		}

		private void UIAppAwake(ApplicationLauncherButton button, UIApp app)
		{
			if (button == null || app == null)
				return;

			buttons.Add(button);

			if (app is ContractsApp)
			{
				button.onHover = (Callback)Delegate.Combine(button.onHover, new Callback(OnContractHover));
				button.onTrue = (Callback)Delegate.Combine(button.onTrue, new Callback(OnContractOpen));

				button.onHoverOut = (Callback)Delegate.Combine(button.onHoverOut, new Callback(OnContractHoverOut));
				button.onFalse = (Callback)Delegate.Combine(button.onFalse, new Callback(OnContractClosed));

				var fields = typeof(ContractsApp).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

				GenericAppFrame appframe = fields[7].GetValue((ContractsApp)app) as GenericAppFrame;

				if (appframe != null)
					appframe.AddGlobalInputDelegate(new UnityAction<PointerEventData>(OnAppEnter), new UnityAction<PointerEventData>(OnAppExit));
			}
			else if (app is EngineersReport)
			{
				button.onHover = (Callback)Delegate.Combine(button.onHover, new Callback(OnEngineerHover));
				button.onTrue = (Callback)Delegate.Combine(button.onTrue, new Callback(OnEngineerOpen));

				button.onHoverOut = (Callback)Delegate.Combine(button.onHoverOut, new Callback(OnEngineerHoverOut));
				button.onFalse = (Callback)Delegate.Combine(button.onFalse, new Callback(OnEngineerClosed));

				var fields = typeof(EngineersReport).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

				GenericAppFrame appframe = fields[1].GetValue((EngineersReport)app) as GenericAppFrame;

				if (appframe != null)
					appframe.AddGlobalInputDelegate(new UnityAction<PointerEventData>(OnAppEnter), new UnityAction<PointerEventData>(OnAppExit));
			}
		}

		private void StageAwake(StageGroup group)
		{
			bool display = BasicDeltaV.Instance.DisplayActive;

			//BasicDeltaV.BasicLogging("New Stage: Index: {0} - Last Stage: {1} - Current Stage: {2}", group.inverseStageIndex, StageManager.LastStage, StageManager.CurrentStage);

			if (display && HighLogic.LoadedSceneIsFlight && BasicDeltaV_Settings.Instance.ShowCurrentStageOnly)
				display = group.inverseStageIndex == StageManager.LastStage;

			AddStagePanel(group, display);
		}

		private void StageDestroy(StageGroup group)
		{
			if (panels.Contains(group))
			{
				panels[group].Destroy();
				panels[group] = null;
				panels.Remove(group);
			}
		}

		private void AddStagePanel(StageGroup group, bool display)
		{
			bool iconInfo = false;

			if (HighLogic.LoadedSceneIsFlight)
			{
				int checks = 0;

				for (int i = group.Icons.Count - 1; i >= 0; i--)
				{
					StageIcon icon = group.Icons[i];
					
					if (icon.grouped)
					{
						if (icon.expanded)
						{
							if (checks >= 1)
							{
								if (icon.groupedIcons[icon.groupedIcons.Count - 1].isDisplayingInfo)
								{
									iconInfo = true;
									break;
								}
								else
									break;
							}
							else
								checks += 2;
						}
						else
							checks++;

						if (!icon.isDisplayingInfoInGroup)
							continue;
					}
					else
					{
						checks++;

						if (!icon.isDisplayingInfo)
							continue;
					}

					iconInfo = true;
					break;
				}
			}

			BasicDeltaV_StagePanel panel = new BasicDeltaV_StagePanel(group.RectTransform, group.inverseStageIndex, iconInfo, display);

			panels.Add(group, panel);
		}

		public void RefreshPanels()
		{
			if (!BasicDeltaV.Instance.DisplayActive)
				return;

			for (int i = panels.Count - 1; i >= 0; i--)
				panels.At(i).RefreshModules();
		}

		public void UpdateAlpha(float alpha)
		{
			for (int i = panels.Count - 1; i >= 0; i--)
				panels.At(i).SetAlpha(alpha);
		}

		public void PanelHideDisplay()
		{
			for (int i = panels.Count - 1; i >= 0; i--)
			{
				BasicDeltaV_StagePanel panel = panels.At(i);

				panel.SetVisible(false);
			}
		}

		public void UpdatePanels()
		{
			var enumerator = panels.Keys.GetEnumerator();
			
			while (enumerator.MoveNext())
			{
				StageGroup group = enumerator.Current;

				if (group == null)
					continue;

				BasicDeltaV_StagePanel panel = null;

				if (!panels.TryGetValue(group, out panel))
					continue;

				panel.Index = group.inverseStageIndex;

				panel.Stage = BasicDeltaV.Instance.GetStage(panel.Index);
				
				if (panel.Stage == null)
				{
					panel.SetVisible(false);
					continue;
				}

				if (!BasicDeltaV.Instance.DisplayActive)
				{
					panel.SetVisible(false);
					continue;
				}

				if (group.Icons.Count <= 0)
				{
					panel.SetVisible(false);
					continue;
				}

				if (HighLogic.LoadedSceneIsFlight && BasicDeltaV_Settings.Instance.ShowCurrentStageOnly)
				{
					int i = StageManager.LastStage;

					if (group.inverseStageIndex != i)
					{
						panel.SetVisible(false);
						continue;
					}
					else if (panel.Stage.deltaV <= 0)
					{
						panel.ToggleNoDVModules(false);
					}
					else
					{
						panel.ToggleNoDVModules(true);
					}
				}
				else if (panel.Stage.deltaV <= 0)
				{
					panel.SetVisible(false);
					continue;
				}

				if (HighLogic.LoadedSceneIsFlight)
				{
					bool info = false;

					int checks = 0;

					for (int i = group.Icons.Count - 1; i >= 0; i--)
					{
						//BasicDeltaV.BasicLogging("Stage: {0} - Checks: {1}", panel.Index, checks);

						if (checks >= 2)
							break;

						StageIcon icon = group.Icons[i];

						if (icon.grouped)
						{
							if (icon.expanded)
							{
								if (checks >= 1)
								{
									if (icon.groupedIcons[icon.groupedIcons.Count - 1].isDisplayingInfo)
									{
										info = true;
										break;
									}
									else
										break;
								}
								else
									checks += 2;
							}
							else
								checks++;

							if (!icon.isDisplayingInfoInGroup)
								continue;
						}
						else
						{
							checks++;

							if (!icon.isDisplayingInfo)
								continue;
						}

						info = true;
						break;
					}

					if (info)
					{
						if (!panel.PanelRight)
							panel.MovePanel(true);
					}
					else if (panel.PanelRight)
						panel.MovePanel(false);
				}

				panel.SetVisible(true);
			}
		}

		private void OnAppEnter(PointerEventData eventData)
		{
			HidePanel(true);

			inApp = true;
		}

		private void OnAppExit(PointerEventData eventData)
		{
			inApp = false;

			if (!contractsPinned && !engineerPinned)
				HidePanel(false);
		}

		private void OnContractOpen()
		{
			contractsPinned = true;

			HidePanel(true);
		}

		private void OnEngineerOpen()
		{
			engineerPinned = true;

			HidePanel(true);
		}

		private void OnContractClosed()
		{
			contractsPinned = false;
			contractHover = false;

			if (!engineerPinned && !engineerHover)
				HidePanel(false);
		}

		private void OnEngineerClosed()
		{
			engineerPinned = false;
			engineerHover = false;

			if (!contractsPinned && !contractHover)
				HidePanel(false);
		}

		private void OnContractHover()
		{
			contractHover = true;

			HidePanel(true);
		}

		private void OnEngineerHover()
		{
			engineerHover = true;

			HidePanel(true);
		}

		private void OnContractHoverOut()
		{
			contractHover = false;

			StartCoroutine(WaitForHoverOut(true));
		}

		private void OnEngineerHoverOut()
		{
			engineerHover = false;

			StartCoroutine(WaitForHoverOut(false));
		}

		private IEnumerator WaitForHoverOut(bool contract)
		{
			int timer = 0;

			while (timer < 3)
			{
				timer++;
				yield return null;
			}

			bool anyButtonOn = false;

			for (int i = buttons.Count - 1; i >= 0; i--)
			{
				ApplicationLauncherButton button = buttons[i];

				if (!button.IsHovering && button.toggleButton.CurrentState != KSP.UI.UIRadioButton.State.True)
					continue;

				anyButtonOn = true;
				break;
			}

			if (!anyButtonOn && !inApp)
			{
				if (contract && !contractsPinned && !engineerPinned)
					HidePanel(false);
				else if (!contract && !engineerPinned && !contractsPinned)
					HidePanel(false);
			}
		}

		private void HidePanel(bool isOn)
		{
			for (int i = panels.Count - 1; i >= 0; i--)
				panels.At(i).HidePanel(isOn);
		}
	}
}
