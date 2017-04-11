#region License
/*
 * Basic DeltaV
 * 
 * StateToggle - Toggle extension for switching between two status indicator sprites
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
using UnityEngine.UI;
using UnityEngine.Events;

namespace BasicDeltaV.Unity
{
	public class StateToggle : Toggle
	{
		[SerializeField]
		private Sprite m_OffSprite = null;
		[SerializeField]
		private Sprite m_OnSprite = null;

		private Image CheckMark;

		public Sprite OffSprite
		{
			set { m_OffSprite = value; }
		}

		public Sprite OnSprite
		{
			set { m_OnSprite = value; }
		}

		protected override void Awake()
		{
			base.Awake();

			CheckMark = GetComponentsInChildren<Image>()[1];

			onValueChanged.AddListener(new UnityAction<bool>(StateChange));

			if (CheckMark != null)
				CheckMark.sprite = isOn ? m_OnSprite : m_OffSprite;
		}

		private void StateChange(bool _isOn)
		{
			if (CheckMark != null)
				CheckMark.sprite = _isOn ? m_OnSprite : m_OffSprite;
		}
	}
}
