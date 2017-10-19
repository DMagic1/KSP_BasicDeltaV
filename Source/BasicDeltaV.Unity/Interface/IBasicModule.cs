#region License
/*
 * Basic DeltaV
 * 
 * IBasicModule - Interface for transferring information to and from the readout UI element
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

namespace BasicDeltaV.Unity.Interface
{
    public interface IBasicModule
    {
        string ModuleTitle { get; }

        string ModuleText { get; }

        bool SmallSize { get; }

		bool DVModule { get; }

        int Order { get; set; }

		int FixedOrder { get; }
        
        void Update();
    }
}