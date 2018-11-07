#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_UIAppHandler - Listener attached to the stock UIApp
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
using KSP.UI;
using KSP.UI.Screens;

namespace BasicDeltaV
{
	public class BasicDeltaV_UIAppHandler : MonoBehaviour
	{
		public class UIAppAwake : UnityEvent<ApplicationLauncherButton, UIApp> { }

		public static UIAppAwake OnUIAppAwake = new UIAppAwake();

		private UIApp app;

		private void Awake()
		{
			if (!HighLogic.LoadedSceneIsEditor)
			{
				Destroy(this);
				return;
			}

			app = GetComponent<UIApp>();

			if (app != null)
				StartCoroutine(WaitForAppLauncher());
		}

		private IEnumerator WaitForAppLauncher()
		{
			while (!ApplicationLauncher.Ready)
				yield return null;

			while (app.appLauncherButton == null)
				yield return null;

			int timer = 0;

			while (timer < 20)
			{
				timer++;
				yield return null;
			}

			OnUIAppAwake.Invoke(app.appLauncherButton, app);
		}
	}
}
