#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_Panel - Script for controlling the readout module UI panel
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
using UnityEngine;
using UnityEngine.UI;

namespace BasicDeltaV.Unity.Unity
{
    public class BasicDeltaV_Panel : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_ModulePrefab = null;
		[SerializeField]
		private LayoutGroup m_ContentLayout = null;
        [SerializeField]
        private Transform m_RowOneTransform = null;
        [SerializeField]
        private Transform m_RowTwoTransform = null;
        [SerializeField]
		private Transform m_RowThreeTransform = null;
        [SerializeField]
		private Image m_Background = null;
		[SerializeField]
		private Image m_Header = null;

		private bool rightPos = false;
		private bool shifted = false;

        private RectTransform rect;
		private CanvasGroup cg;
        private List<BasicDeltaV_Module> Modules = new List<BasicDeltaV_Module>();
        
		public bool RightPos
		{
			get { return rightPos; }
		}

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
			cg = GetComponent<CanvasGroup>();
        }

		public void OnHide()
		{
			if (cg != null)
				cg.alpha = 0.2f;
		}

		public void OnUnHide()
		{
			if (cg != null)
				cg.alpha = 1;
		}
        
        public void Close()
        {
            BasicDVPanelManager.Instance.UnregisterPanel(this);

            gameObject.SetActive(false);

            Destroy(gameObject);
        }

        public void setPanel(List<IBasicModule> modules, float alpha, bool right)
        {
            CreateModules(modules);

            SetAlpha(alpha);

            if (right)
            {
                if (m_ContentLayout != null)
                    m_ContentLayout.childAlignment = TextAnchor.UpperLeft;

                if (rect != null)
                {
                    rect.pivot = new Vector2(0, 0);

                    rect.anchoredPosition = new Vector2(24, rect.anchoredPosition.y);
                }
            }

            BasicDVPanelManager.Instance.RegisterPanel(this);
        }

		public void MovePanel(bool right)
		{
			int value = 0;

			if (right && !rightPos)
				value = 75;
			else if (!right && rightPos)
				value = -75;

			if (rect != null)
				rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + value, rect.anchoredPosition.y);

			rightPos = right;
		}

		public void MovePanelUp()
		{
			if (rect != null && !shifted)
				rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + 12);

			shifted = true;
		}
        
        public void SetAlpha(float a)
        {
			if (m_Background != null)
			{
				Color c = m_Background.color;

				c.a = a;

				m_Background.color = c;
			}

			if (m_Header != null)
			{
				Color c = m_Header.color;

				c.a = a;

				m_Header.color = c;
			}
        }

		public void ToggleNoDVModules(bool isOn)
		{
			for (int i = Modules.Count - 1; i >= 0; i--)
			{
				BasicDeltaV_Module mod = Modules[i];

				if (mod == null)
					continue;

				mod.ToggleModule(isOn);
			}
		}
        
        private void CreateModules(List<IBasicModule> modules)
        {
            if (modules == null || modules.Count == 0)
                return;
            
            if (m_ModulePrefab == null || m_RowOneTransform == null || m_RowTwoTransform == null || m_RowThreeTransform == null)
                return;

            modules = CalculateOrder(modules);

            for (int i = 0; i < modules.Count; i++)
            {
                IBasicModule module = modules[i];

                if (module == null)
                    continue;

                CreateModule(module);
            }
        }

        private List<IBasicModule> CalculateOrder(List<IBasicModule> modules)
        {
            List<IBasicModule> smallMods = new List<IBasicModule>();
            List<IBasicModule> largeMods = new List<IBasicModule>();

            for (int i = modules.Count - 1; i >= 0; i--)
            {
                IBasicModule mod = modules[i];

                if (mod.SmallSize)
                    smallMods.Add(mod);
                else
                    largeMods.Add(mod);
            }

            smallMods.Sort((a, b) => a.FixedOrder.CompareTo(b.FixedOrder));
			largeMods.Sort((a, b) => a.FixedOrder.CompareTo(b.FixedOrder));

            for (int i = 0; i < smallMods.Count; i++)
                smallMods[i].Order = i + 1;

            for (int i = 0; i < largeMods.Count; i++)
                largeMods[i].Order = i + 1;

            if (largeMods.Count == 0)
            {
                for (int i = 0; i < smallMods.Count; i++)
                    smallMods[i].Order = i + 1;
            }
            else if (largeMods.Count == 1)
            {
                largeMods[0].Order = 2;

				if (smallMods.Count == 1)
					smallMods[0].Order = 3;
				else if (smallMods.Count == 2)
				{
					smallMods[0].Order = 3;
					smallMods[1].Order = 4;
				}
				else
				{
					for (int i = 0; i < smallMods.Count; i++)
						smallMods[i].Order = i <= 0 ? 1 : i + 2;
				}
            }
			else if (largeMods.Count == 2)
			{
				largeMods[0].Order = 2;
				largeMods[1].Order = 4;

				if (smallMods.Count == 1)
					smallMods[0].Order = 5;
				else if (smallMods.Count == 2)
				{
					smallMods[0].Order = 5;
					smallMods[1].Order = 6;
				}
				else
				{
					for (int i = 0; i < smallMods.Count; i++)
						smallMods[i].Order = (i * 2) + 1;
				}
			}
            else
            {
                for (int i = 0; i < largeMods.Count; i++)
                    largeMods[i].Order = (i + 1) * 2;

                for (int i = 0; i < smallMods.Count; i++)
                    smallMods[i].Order = (i * 2) + 1;
            }

            List<IBasicModule> orderedMods = new List<IBasicModule>();

            orderedMods.AddRange(smallMods);
            orderedMods.AddRange(largeMods);

            orderedMods.Sort((a, b) => a.Order.CompareTo(b.Order));

            return orderedMods;
        }
        
        private void CreateModule(IBasicModule module)
        {
            BasicDeltaV_Module mod = Instantiate(m_ModulePrefab).GetComponent<BasicDeltaV_Module>();

            if (mod == null)
                return;

            mod.transform.SetParent(module.Order <= 2 ? m_RowOneTransform : module.Order <= 4 ? m_RowTwoTransform : m_RowThreeTransform, false);

            mod.setModule(module);

            Modules.Add(mod);
        }
        
        public void OnUpdate()
        {
            for (int i = Modules.Count - 1; i >= 0; i--)
            {
                BasicDeltaV_Module mod = Modules[i];

                if (mod == null)
                    continue;

                mod.UpdateModule();
            }
        }

    }
}