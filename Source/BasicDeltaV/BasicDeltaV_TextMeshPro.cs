#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_TextMeshPro - Extension method for setting the text field of a TextMeshProUGUI object
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

using BasicDeltaV.Unity;
using UnityEngine.Events;
using TMPro;

namespace BasicDeltaV
{
    public class BasicDeltaV_TextMeshPro : TextMeshProUGUI
    {
        private TextHandler _handler;

        new private void Awake()
        {
            m_isAlignmentEnumConverted = true;

            base.Awake();

            _handler = GetComponent<TextHandler>();

            if (_handler == null)
                return;
            
            _handler.OnTextUpdate.AddListener(new UnityAction<string>(UpdateText));
        }
        
        private void UpdateText(string t)
        {
            text = t;
        }
    }
}