// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BasicDeltaV
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class BasicLogger : MonoBehaviour
    {
        #region Fields

        private static readonly List<string[]> messages = new List<string[]>();

        #endregion

        #region Constants

        private static readonly string fileName;
        private static readonly AssemblyName assemblyName;

        #endregion

        #region Initialisation

        static BasicLogger()
        {
            assemblyName = Assembly.GetExecutingAssembly().GetName();
            fileName = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "log");
            File.Delete(fileName);

            lock (messages)
            {
                messages.Add(new[] {"Executing: " + assemblyName.Name + " - " + assemblyName.Version});
                messages.Add(new[] {"Assembly: " + Assembly.GetExecutingAssembly().Location});
            }
            Blank();
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        #endregion

        #region Printing

        public static void Blank()
        {
            lock (messages)
            {
                messages.Add(new string[] {});
            }
        }

        public static void Log(object obj)
        {
            lock (messages)
            {
                try
                {
                    messages.Add(new[] {"Log " + DateTime.Now.TimeOfDay, GetObjString(obj)});
                }
                catch (Exception ex)
                {
                    Exception(ex);
                }
            }
        }

        private static string GetObjString(object obj, int tabs = 0)
        {
            string objString;
            string tabString = string.Empty;
            for (int i = 0; i < tabs; i++)
            {
                tabString += " ";
            }

            if (obj != null)
            {
                objString = tabString + obj;

                IEnumerable items = obj as IEnumerable;
                if (items != null)
                {
                    foreach (object item in items)
                    {
                        objString += "\n" + GetObjString(item, tabs + 1);
                    }
                }
            }
            else
            {
                objString = "Null";
            }

            return objString;
        }

        public static void Exception(Exception ex)
        {
            lock (messages)
            {
                messages.Add(new[] {"Exception " + DateTime.Now.TimeOfDay, ex.Message});
                messages.Add(new[] {string.Empty, ex.StackTrace});
                Blank();
            }
        }

        public static void Exception(Exception ex, string location)
        {
            lock (messages)
            {
                messages.Add(new[] {"Exception " + DateTime.Now.TimeOfDay, location + " // " + ex.Message});
                messages.Add(new[] {string.Empty, ex.StackTrace});
                Blank();
            }
        }

        #endregion

        #region Flushing

        public static void Flush()
        {
            lock (messages)
            {
                if (messages.Count > 0)
                {
                    using (StreamWriter file = File.AppendText(fileName))
                    {
                        foreach (string[] message in messages)
                        {
                            file.WriteLine(message.Length > 0 ? message.Length > 1 ? "[" + message[0] + "]: " + message[1] : message[0] : string.Empty);
                            if (message.Length > 0)
                            {
                                print(message.Length > 1 ? assemblyName.Name + " -> " + message[1] : assemblyName.Name + " -> " + message[0]);
                            }
                        }
                    }
                    messages.Clear();
                }
            }
        }

        private void LateUpdate()
        {
            Flush();
        }

        #endregion

        #region Destruction

        private void OnDestroy()
        {
            Flush();
        }

        ~BasicLogger()
        {
            Flush();
        }

        #endregion
    }
}