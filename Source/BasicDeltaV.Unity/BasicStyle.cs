#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_Style - Script for controlling the selection of UI style elements
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

namespace BasicDeltaV.Unity
{
    public class BasicStyle : MonoBehaviour
    {
        public enum ElementTypes
        {
            None,
            Title,
            Window,
            Box,
            Button,
            StandardButton,
            Toggle,
            ToggleCheck,
            Slider,
			Footer,
			ContentFooter,
			Content
        }

        [SerializeField]
        private ElementTypes m_ElementType = ElementTypes.None;

        public ElementTypes ElementType
        {
            get { return m_ElementType; }
        }
        
        private void setSelectable(Sprite normal, Sprite highlight, Sprite active, Sprite inactive)
        {
            Selectable select = GetComponent<Selectable>();

            if (select == null)
                return;

            select.image.sprite = normal;
            select.image.type = Image.Type.Sliced;
            select.transition = Selectable.Transition.SpriteSwap;

            SpriteState spriteState = select.spriteState;
            spriteState.highlightedSprite = highlight;
            spriteState.pressedSprite = active;
            spriteState.disabledSprite = inactive;
            select.spriteState = spriteState;
        }

        private void setSelectable(Sprite normal)
        {
            Selectable select = GetComponent<Selectable>();

            if (select == null)
                return;

            select.image.sprite = normal;
            select.image.type = Image.Type.Sliced;
            select.transition = Selectable.Transition.None;
        }

        public void setImage(Sprite sprite)
        {
            Image image = GetComponent<Image>();

            if (image == null)
                return;

            image.sprite = sprite;
        }

        public void setButton(Sprite normal)
        {
            setSelectable(normal);
        }

        public void setButton(Sprite normal, Sprite highlight, Sprite active, Sprite inactive)
        {
            setSelectable(normal, highlight, active, inactive);
        }
        
        public void setToggle(Sprite normal, Sprite onMark, Sprite offMark)
        {
            setSelectable(normal);

			if (onMark == null || offMark == null)
				return;

			StateToggle toggle = GetComponent<StateToggle>();

			if (toggle == null)
				return;

			toggle.OnSprite = onMark;
			toggle.OffSprite = offMark;
        }
        
        public void setSlider(Sprite background, Sprite thumb, Sprite thumbHighlight, Sprite thumbActive, Sprite thumbInactive)
        {
            setSelectable(thumb, thumbHighlight, thumbActive, thumbInactive);

            if (background == null)
                return;

            Slider slider = GetComponent<Slider>();

            if (slider == null)
                return;

            Image back = slider.GetComponentInChildren<Image>();

            if (back == null)
                return;

            back.sprite = background;
            back.type = Image.Type.Sliced;
        }

    }
}