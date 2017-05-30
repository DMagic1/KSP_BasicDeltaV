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
using System.Collections;
using System.Reflection;
using System.Linq;
using BasicDeltaV.Unity;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KSP.UI;
using KSP.UI.Screens;

namespace BasicDeltaV
{
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class BasicDeltaV_Loader : MonoBehaviour
	{
		private const string bundleName = "/basic_deltav_prefabs";
		private const string bundlePath = "GameData/BasicDeltaV/Resources";

		private static bool loaded;
		private static bool TMPLoaded;
		private static bool UILoaded;
		private static bool spritesLoaded;
		private static bool stageGroupProcessed;
		private static bool UIappsProcessed;

		private static GameObject[] loadedPrefabs;

		private static GameObject toolbarPrefab;
		private static GameObject panelPrefab;

		private static Sprite titleSprite;
		private static Sprite footerSprite;
		private static Sprite contentFooterSprite;
		private static Sprite buttonSprite;
		private static Sprite componentSprite;
		private static Sprite selectedSprite;
		private static Sprite unselectedSprite;
		private static Sprite windowSprite;
		private static Sprite stageGroupSprite;

		public static GameObject ToolbarPrefab
		{
			get { return toolbarPrefab; }
		}

		public static GameObject PanelPrefab
		{
			get { return panelPrefab; }
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

			if (!stageGroupProcessed)
				processStageGroup();

			if (!UIappsProcessed)
				processUIApps();

			if (loadedPrefabs == null)
			{
				string path = KSPUtil.ApplicationRootPath + bundlePath;

				AssetBundle prefabs = AssetBundle.LoadFromFile(path + bundleName);

				if (prefabs != null)
					loadedPrefabs = prefabs.LoadAllAssets<GameObject>();
			}

			if (loadedPrefabs != null)
			{
				if (!TMPLoaded)
					processTMPPrefabs();

				if (UISkinManager.defaultSkin != null && !UILoaded)
					processUIPrefabs();
			}

			if (TMPLoaded && UILoaded)
			{
				loaded = true;
				BasicDeltaV.BasicLogging("UI Loaded and processed");
			}

			Destroy(gameObject);
		}

		private void loadSprites()
		{
			ContractsApp prefab = null;

			var prefabs = Resources.FindObjectsOfTypeAll<ContractsApp>();

			for (int i = prefabs.Length - 1; i >= 0; i--)
			{
				var pre = prefabs[i];

				if (pre.name != "ContractsApp")
					continue;

				prefab = pre;
				break;
			}

			if (prefab == null)
				return;

			GenericAppFrame appFrame = null;
			GenericCascadingList cascadingList = null;
			UIListItem_spacer spacer = null;

			try
			{
				var fields = typeof(ContractsApp).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).ToArray();

				appFrame = fields[6].GetValue(prefab) as GenericAppFrame;

				cascadingList = fields[8].GetValue(prefab) as GenericCascadingList;

				spacer = fields[10].GetValue(prefab) as UIListItem_spacer;
			}
			catch (Exception e)
			{
				BasicDeltaV.BasicLogging("Error in processing toolbar panel UI: {0}", e);
			}

			if (appFrame != null)
			{
				windowSprite = appFrame.gfxBg.sprite;
				titleSprite = appFrame.gfxHeader.sprite;
				footerSprite = appFrame.gfxFooter.sprite;
			}

			if (cascadingList != null)
			{
				buttonSprite = cascadingList.cascadeHeader.GetComponent<Image>().sprite;
				contentFooterSprite = cascadingList.cascadeFooter.GetComponent<Image>().sprite;
			}

			if (spacer != null)
			{
				componentSprite = spacer.GetComponent<Image>().sprite;

				UIStateImage stateImage = spacer.GetComponentInChildren<UIStateImage>();

				selectedSprite = stateImage.states[1].sprite;
				unselectedSprite = stateImage.states[0].sprite;
			}

			spritesLoaded = true;
		}

		private void processStageGroup()
		{
			StageManager prefab = null;

			var prefabs = Resources.FindObjectsOfTypeAll<StageManager>();

			for (int i = prefabs.Length - 1; i >= 0; i--)
			{
				var pre = prefabs[i];

				if (pre.name != "StageManagerEditor")
					continue;

				prefab = pre;
				break;
			}

			if (prefab == null)
				return;

			StageGroup group = prefab.stageGroupPrefab;

			Transform layout = group.transform.FindChild("IconLayout");

			if (layout != null)
				stageGroupSprite = layout.GetComponent<Image>().sprite;

			group.gameObject.AddComponent<BasicDeltaV_StageGroupHandler>();

			stageGroupProcessed = true;
		}

		private void processUIApps()
		{
			var uiapps = Resources.FindObjectsOfTypeAll<UIApp>();

			for (int i = uiapps.Length - 1; i >= 0; i--)
			{
				UIApp app = uiapps[i];

				if (app.name == "ContractsApp" || app.name == "EngineersReport")// || app.name == "SMS")
					app.gameObject.AddComponent<BasicDeltaV_UIAppHandler>();
			}

			UIappsProcessed = true;
		}

		private void processTMPPrefabs()
		{
			for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
			{
				GameObject o = loadedPrefabs[i];

				if (o.name == "BasicDeltaV_Panel")
					panelPrefab = o;
				else if (o.name == "BasicDeltaV_AppWindow")
					toolbarPrefab = o;

				if (o != null)
					processTMP(o);
			}

			TMPLoaded = true;
		}

		private void processTMP(GameObject obj)
		{
			TextHandler[] handlers = obj.GetComponentsInChildren<TextHandler>(true);

			if (handlers == null)
				return;

			for (int i = 0; i < handlers.Length; i++)
				TMProFromText(handlers[i]);
		}

		private void TMProFromText(TextHandler handler)
		{
			if (handler == null)
				return;

			Text text = handler.GetComponent<Text>();

			if (text == null)
				return;

			string t = text.text;
			Color c = text.color;
			int i = text.fontSize;
			bool r = text.raycastTarget;
			FontStyles sty = TMPProUtil.FontStyle(text.fontStyle);
			TextAlignmentOptions align = TMPProUtil.TextAlignment(text.alignment);
			float spacing = text.lineSpacing;
			GameObject obj = text.gameObject;

			DestroyImmediate(text);

			BasicDeltaV_TextMeshPro tmp = obj.AddComponent<BasicDeltaV_TextMeshPro>();

			tmp.text = t;
			tmp.color = c;
			tmp.fontSize = i;
			tmp.raycastTarget = r;
			tmp.alignment = align;
			tmp.fontStyle = sty;
			tmp.lineSpacing = spacing;

			tmp.font = UISkinManager.TMPFont;
			tmp.fontSharedMaterial = Resources.Load("Fonts/Materials/Calibri Dropshadow", typeof(Material)) as Material;

			tmp.enableWordWrapping = true;
			tmp.isOverlay = false;
			tmp.richText = true;
		}

		private void processUIPrefabs()
		{
			for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
			{
				GameObject o = loadedPrefabs[i];

				if (o != null)
					processUIComponents(o);
			}

			UILoaded = true;
		}

		private void processUIComponents(GameObject obj)
		{
			BasicStyle[] styles = obj.GetComponentsInChildren<BasicStyle>(true);

			if (styles == null)
				return;

			for (int i = 0; i < styles.Length; i++)
				processComponents(styles[i]);
		}

		private void processComponents(BasicStyle style)
		{
			if (style == null)
				return;

			UISkinDef skin = UISkinManager.defaultSkin;

			if (skin == null)
				return;

			switch (style.ElementType)
			{
				case BasicStyle.ElementTypes.Title:
					style.setImage(titleSprite);
					break;
				case BasicStyle.ElementTypes.Window:
					style.setImage(windowSprite);
					break;
				case BasicStyle.ElementTypes.Box:
					style.setImage(stageGroupSprite);
					break;
				case BasicStyle.ElementTypes.Button:
					style.setButton(componentSprite);
					break;
				case BasicStyle.ElementTypes.StandardButton:
					style.setButton(skin.button.normal.background, skin.button.highlight.background, skin.button.active.background, skin.button.disabled.background);
					break;
				case BasicStyle.ElementTypes.Toggle:
					style.setToggle(buttonSprite, null, null);
					break;
				case BasicStyle.ElementTypes.ToggleCheck:
					style.setToggle(componentSprite, selectedSprite, unselectedSprite);
					break;
				case BasicStyle.ElementTypes.Slider:
					style.setSlider(skin.horizontalSlider.normal.background, skin.horizontalSliderThumb.normal.background, skin.horizontalSliderThumb.highlight.background, skin.horizontalSliderThumb.active.background, skin.horizontalSliderThumb.disabled.background);
					break;
				case BasicStyle.ElementTypes.Footer:
					style.setImage(footerSprite);
					break;
				case BasicStyle.ElementTypes.ContentFooter:
					style.setImage(contentFooterSprite);
					break;
				case BasicStyle.ElementTypes.Content:
					style.setImage(componentSprite);
					break;
				default:
					break;
			}
		}

	}
}