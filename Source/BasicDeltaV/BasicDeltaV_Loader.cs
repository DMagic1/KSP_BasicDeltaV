#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_Loader - MonoBehaviour for final processing of the Unity prefabs
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
using System.Reflection;
using System.Linq;
using UnityEngine;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;

namespace BasicDeltaV
{
	[KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
	public class BasicDeltaV_Loader : MonoBehaviour
	{
		private static bool loaded;
		private static bool spritesLoaded;
		private static bool stageFlightGroupProcessed;

        private static bool dvToolbarEditorProcessed;
        private static bool dvToolbarFlightProcessed;
        
        private static TooltipController_Text tooltipPrefab;
        
        private static Sprite twrGaugeSprite;
        
        public static TooltipController_Text TooltipPrefab
        {
            get { return tooltipPrefab; }
        }

        public static Sprite TWRGaugeSprite
        {
            get { return twrGaugeSprite; }
        }
        
        private void Awake()
		{
			if (loaded)
			{
				Destroy(gameObject);
				return;
			}
			
			if (!spritesLoaded)
				loadSprites();
            
            if (!dvToolbarEditorProcessed && HighLogic.LoadedSceneIsEditor)
                processEditorDVToolbar();

            if (!stageFlightGroupProcessed && HighLogic.LoadedSceneIsFlight)
				processFlightStageGroup();

            if (!dvToolbarFlightProcessed && HighLogic.LoadedSceneIsFlight)
                processFlightDVToolbar();
            
            if (spritesLoaded && stageFlightGroupProcessed && dvToolbarEditorProcessed && dvToolbarFlightProcessed)
                loaded = true;

            Destroy(gameObject);
		}

		private void loadSprites()
		{
            Texture2D twr = GameDatabase.Instance.GetTexture("BasicDeltaV/Resources/TWRGauge", false);

            twrGaugeSprite = Sprite.Create(twr, new Rect(0, 0, twr.width, twr.height), new Vector2(0.5f, 0.5f));
            
			spritesLoaded = true;
		}

        private void processEditorDVToolbar()
        {
            DeltaVApp dvApp = null;

            var apps = Resources.FindObjectsOfTypeAll<DeltaVApp>();

            if (apps != null && apps.Length > 0)
            {
                dvApp = apps[0];
                
                var fields = typeof(DeltaVApp).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).ToArray();

                DeltaVAppSituation situationPrefab = null;

                for (int i = fields.Length - 1; i >= 0; i--)
                {
                    if (fields[i].Name == "situationPrefab")
                        situationPrefab = fields[i].GetValue(dvApp) as DeltaVAppSituation;
                    //else if (fields[i].Name == "appHeightSituation")
                    //    fields[i].SetValue(dvApp, 201);
                    else if (fields[i].Name == "appHeightStageInfo")
                        fields[i].SetValue(dvApp, 130);
                }

                if (situationPrefab != null)
                {
                    situationPrefab.gameObject.AddComponent<BasicDeltaV_DeltaVSituationHandler>();
                }
            }

            dvToolbarEditorProcessed = true;
        }

        private void processFlightDVToolbar()
        {
            DeltaVApp dvApp = null;

            var apps = Resources.FindObjectsOfTypeAll<DeltaVApp>();

            if (apps != null && apps.Length > 0)
            {
                dvApp = apps[0];

                var fields = typeof(DeltaVApp).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).ToArray();

                DeltaVAppStageInfo stageInfoPrefab = null;

                for (int i = fields.Length - 1; i >= 0; i--)
                {
                    if (fields[i].Name == "stageInfoPrefab")
                        stageInfoPrefab = fields[i].GetValue(dvApp) as DeltaVAppStageInfo;
                    else if (fields[i].Name == "appHeightStageInfo")
                        fields[i].SetValue(dvApp, 130);
                }

                if (stageInfoPrefab != null)
                {
                    stageInfoPrefab.gameObject.AddComponent<BasicDeltaV_DeltaVAppStageHandler>();
                }
            }

            dvToolbarFlightProcessed = true;
        }
        
        private void processFlightStageGroup()
        {
            StageManager prefabFlight = null;

            var prefabs = Resources.FindObjectsOfTypeAll<StageManager>();

            for (int i = prefabs.Length - 1; i >= 0; i--)
            {
                var pre = prefabs[i];

                if (pre.name == "StageManager")
                    prefabFlight = pre;
            }

            if (prefabFlight == null)
                return;

            try
            {
                var fields = typeof(StageIcon).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).ToArray();

                StageIconInfoBox infoBar = fields[12].GetValue(prefabFlight.stageIconPrefab) as StageIconInfoBox;
                
            tooltipPrefab = infoBar.GetComponent<TooltipController_Text>();
                
            tooltipPrefab.enabled = true;
            tooltipPrefab.continuousUpdate = true;
                
            }
            catch (Exception e)
            {
                BasicDeltaV.BasicLogging("Error in stage panel info box UI: {0}", e);
            }

            StageGroup group = prefabFlight.stageGroupPrefab;
            
            group.gameObject.AddComponent<BasicDeltaV_StageGroupHandler>();

            stageFlightGroupProcessed = true;
        }
        
	}
}