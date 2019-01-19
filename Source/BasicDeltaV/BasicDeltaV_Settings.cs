#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_Settings - Persistent settings controller
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
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BasicDeltaV
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class BasicDeltaV_Settings : MonoBehaviour
    {
        [Persistent]
        public bool ShowTWRGauge = true;
        [Persistent]
        public bool ShowDVSliders = true;
        [Persistent]
        public bool ShowRCS = true;
        [Persistent]
        public bool ShowTorque = true;
        [Persistent]
        public int VABSituation = 0;
        [Persistent]
        public int SPHSituation = 0;
        
        [Persistent]
        public bool VectoredThrust = true;
        //[Persistent]
        //public Color LabelColor = Color.white;
        //[Persistent]
        //public Color ReadoutColor = new Color(0.72157f, 0.79216f, 0.44706f);
        
        private const string fileName = "PluginData/Settings.cfg";
        private string fullPath;

        private static bool loaded;
        private static BasicDeltaV_Settings instance;

        //private static string _labelColorHex;
        //private static string _readoutColorHex;

        public static BasicDeltaV_Settings Instance
        {
            get { return instance; }
        }

        //public static string LabelColorHex
        //{
        //    get { return _labelColorHex; }
        //}

        //public static string ReadoutColorHex
        //{
        //    get { return _readoutColorHex; }
        //}

        private void Awake()
        {
            if (loaded)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            loaded = true;

            instance = this;

            fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName).Replace("\\", "/");

            if (Load())
                BasicDeltaV.BasicLogging("Settings file loaded");
			else
			{
				if (Save())
					BasicDeltaV.BasicLogging("New Settings files generated at:\n{0}", fullPath);
			}
        }
        
        public bool Load()
        {
            bool b = false;

            try
            {
                if (File.Exists(fullPath))
                {
                    ConfigNode node = ConfigNode.Load(fullPath);
                    ConfigNode unwrapped = node.GetNode(GetType().Name);
                    ConfigNode.LoadObjectFromConfig(this, unwrapped);
                    b = true;
                }
                else
                {
                    BasicDeltaV.BasicLogging("Settings file could not be found [{0}]", fullPath);
                    b = false;
                }
            }
            catch (Exception e)
            {
                BasicDeltaV.BasicLogging("Error while loading settings file from [{0}]\n{1}", fullPath, e);
                b = false;
            }

            //_labelColorHex = ColorUtility.ToHtmlStringRGB(LabelColor);
            //_readoutColorHex = ColorUtility.ToHtmlStringRGB(ReadoutColor);

            return b;
        }

        public bool Save()
        {
            bool b = false;

            try
            {
                ConfigNode node = AsConfigNode();
                ConfigNode wrapper = new ConfigNode(GetType().Name);
                wrapper.AddNode(node);
                wrapper.Save(fullPath);
                b = true;
            }
            catch (Exception e)
            {
                BasicDeltaV.BasicLogging("Error while saving settings file from [{0}]\n{1}", fullPath, e);
                b = false;
            }

            return b;
        }

        private ConfigNode AsConfigNode()
        {
            try
            {
                ConfigNode node = new ConfigNode(GetType().Name);

                node = ConfigNode.CreateConfigFromObject(this, node);
                return node;
            }
            catch (Exception e)
            {
                BasicDeltaV.BasicLogging("Failed to generate settings file node...\n{0}", e);
                return new ConfigNode(GetType().Name);
            }
        }
    }
}