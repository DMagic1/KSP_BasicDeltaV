#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_StageGroupHandler - Listener attached to the Staging group prefab
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
using KSP.UI.Screens;

namespace BasicDeltaV
{
	public class BasicDeltaV_StageGroupHandler : MonoBehaviour
	{
		public class StageGroupAwake : UnityEvent<StageGroup> { }
		public class StageGroupDestroy : UnityEvent<StageGroup> { }

		public static StageGroupAwake OnStageGroupAwake = new StageGroupAwake();
		public static StageGroupDestroy OnStageGroupDestroy = new StageGroupDestroy();

		private StageGroup group;

		private void Start()
		{
			if (HighLogic.LoadedSceneIsEditor && !BasicDeltaV.ReadoutsAvailable)
			{
				Destroy(this);
				return;
			}
			else if (HighLogic.LoadedSceneIsFlight && !BasicDeltaV.AvailableInFlight)
			{
				Destroy(this);
				return;
			}

			group = GetComponent<StageGroup>();

			if (group != null)
				OnStageGroupAwake.Invoke(group);
		}

		private void OnDestroy()
		{
			if (group != null)
				OnStageGroupDestroy.Invoke(group);
		}
	}
}
