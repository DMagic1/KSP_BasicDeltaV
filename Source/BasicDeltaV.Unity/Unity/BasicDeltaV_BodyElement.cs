#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_BodyElement - Script for handling the celestial body selection objects
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

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BasicDeltaV.Unity.Unity
{
    public class BasicDeltaV_BodyElement : MonoBehaviour
    {
        public class OnSelect : UnityEvent<string> { }

        [SerializeField]
        private TextHandler m_ElementTitle = null;
		[SerializeField]
		private StateToggle m_BodyToggle = null;
		[SerializeField]
		private LayoutElement m_SpacerLayout = null;

        private string _title;
        private OnSelect _bodySelect = new OnSelect();

        public OnSelect BodySelect
        {
            get { return _bodySelect; }
		}
		
        public void Setup(string element, bool current, int offset, ToggleGroup group)
        {
            _title = element;

            if (m_ElementTitle != null)
                m_ElementTitle.OnTextUpdate.Invoke(element);

			if (m_SpacerLayout != null)
				m_SpacerLayout.minWidth = offset;

			if (m_BodyToggle != null)
			{
				m_BodyToggle.isOn = current;
				m_BodyToggle.group = group;
			}
        }

        public void Select(bool isOn)
        {
            _bodySelect.Invoke(_title);
        }
    }
}
