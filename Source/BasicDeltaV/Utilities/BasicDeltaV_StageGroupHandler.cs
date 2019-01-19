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

using System.Collections;
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

        private const float REFRESH_RATE = 0.333f;
        private bool _resetStagePanelInfo;

        private bool _stageInfoSetup;

        private Coroutine _panelRefresh;

		private void Start()
		{
			if (!HighLogic.LoadedSceneIsFlight)
			{
				Destroy(this);
				return;
			}

			group = GetComponent<StageGroup>();

			if (group != null)
				OnStageGroupAwake.Invoke(group);

            _panelRefresh = StartCoroutine(WaitForPanelRefresh());

            GameEvents.onDeltaVCalcsCompleted.Add(DeltaVCalcsCompleted);
            GameEvents.onDeltaVAppInfoItemsChanged.Add(DeltaVCalcsCompleted);
		}

        private IEnumerator WaitForPanelRefresh()
        {
            WaitForSeconds wait = new WaitForSeconds(REFRESH_RATE);
            WaitForEndOfFrame frame = new WaitForEndOfFrame();

            yield return null;

            while (group != null)
            {
                if (!group.InfoPanelStockDisplayEnabled)
                    group.EnableStockInfoPanelDisplays();
                
                if (group.InfoPanelEnabled && !_stageInfoSetup)
                {
                    _stageInfoSetup = true;

                    GameEvents.onDeltaVAppInfoItemsChanged.Fire();
                }

                yield return frame;
                
                _resetStagePanelInfo = true;

                yield return wait;
            }

            _panelRefresh = null;
        }
        
        private void DeltaVCalcsCompleted()
        {
            if (!group.InfoPanelStockDisplayEnabled)
                group.EnableStockInfoPanelDisplays();

            _resetStagePanelInfo = true;
        }

        private void LateUpdate()
        {
            if (group == null)
                return;

            if (_panelRefresh == null)
                _panelRefresh = StartCoroutine(WaitForPanelRefresh());

            if (!_resetStagePanelInfo)
                return;

            _resetStagePanelInfo = false;

            if (group.InfoPanelStockDisplayEnabled)
                group.DisableStockInfoPanelDisplays();
        }

        private void OnDestroy()
		{
			if (group != null)
				OnStageGroupDestroy.Invoke(group);

            group = null;

            if (_panelRefresh != null)
                StopCoroutine(_panelRefresh);

            GameEvents.onDeltaVCalcsCompleted.Remove(DeltaVCalcsCompleted);
            GameEvents.onDeltaVAppInfoItemsChanged.Remove(DeltaVCalcsCompleted);
        }
	}
}
