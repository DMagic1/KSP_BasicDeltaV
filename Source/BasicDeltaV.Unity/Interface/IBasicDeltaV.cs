#region License
/*
 * Basic DeltaV
 * 
 * IBasicDeltaV - Interface for the primary UI object
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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BasicDeltaV.Unity.Interface
{
	public interface IBasicDeltaV
	{
		string Version { get; }

		string CurrentBody { get; set; }

		bool Flight { get; }

		bool DisplayActive { get; set; }

        bool MoreBasicMode { get; set; }

		bool ShowDeltaV { get; set; }

		bool ShowTWR { get; set; }

		bool ShowBurnTime { get; set; }

		bool ShowISP { get; set; }

		bool ShowMass { get; set; }

		bool ShowThrust { get; set; }

		bool ShowBodies { get; set; }

		bool ShowBody { get; }

		bool Atmosphere { get; set; }

		bool ShowAtmosphere { get; }

		bool InMenu { get; set; }

		bool StageScaleEditorOnly { get; set; }

		bool CurrentStageOnly { get; set; }

		bool ShowCurrentStageBar { get; }

		float AtmosphereDepth { get; set; }

		float MaxDepth { get; }

		float Mach { get; set; }

		float Alpha { get; set; }

		float StageScale { get; set; }

		float ToolbarScale { get; set; }

		float Height { get; set; }

		float FlightHeight { get; set; }

		Dictionary<string, int> CelestialBodies { get; }

		void ClampToScreen(RectTransform rect);
	}
}
