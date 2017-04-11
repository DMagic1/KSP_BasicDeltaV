#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_Module - Script for controlling the readout module
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

using BasicDeltaV.Unity.Interface;
using UnityEngine;

namespace BasicDeltaV.Unity.Unity
{
    public class BasicDeltaV_Module : MonoBehaviour
    {
        [SerializeField]
        private TextHandler m_ModuleTitle = null;
        [SerializeField]
        private TextHandler m_TextModule = null;

        private IBasicModule moduleInterface;

        public void setModule(IBasicModule module)
        {
            if (module == null || m_TextModule == null)
                return;

            if (m_ModuleTitle != null)
                m_ModuleTitle.OnTextUpdate.Invoke(module.ModuleTitle + ": ");

            moduleInterface = module;            
        }
        
        public void UpdateModule()
        {
            if (moduleInterface == null || m_TextModule == null)
                return;

            moduleInterface.Update();

            m_TextModule.OnTextUpdate.Invoke(moduleInterface.ModuleText);
        }
    }
}
