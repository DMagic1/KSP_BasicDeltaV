// 
//     Code From Kerbal Engineer Redux
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

namespace BasicDeltaV
{
    using System;
    using System.Collections.Generic;
    using CompoundParts;

    public static class PartExtensions
    {
        /// <summary>
        ///     Gets the cost of the part excluding resources.
        /// </summary>
        public static double GetCostDry(this Part part)
        {
            return part.partInfo.cost - GetResourceCostMax(part) + part.GetModuleCosts(0.0f);
        }

        public static double getCrewAdjustment(this Part part)
        {
            if (HighLogic.LoadedSceneIsEditor && PhysicsGlobals.KerbalCrewMass != 0 && ShipConstruction.ShipManifest != null)
            { //fix weird stock behavior with this physics setting.

                var crewlist = ShipConstruction.ShipManifest.GetAllCrew(false);

                int crew = 0;

                foreach (var crewmem in crewlist)
                {
                    if (crewmem != null) crew++;
                }

                if (crew > 0)
                {
                    var pcm = ShipConstruction.ShipManifest.GetPartCrewManifest(part.craftID);

                    int actualCrew = 0;

                    foreach (var crewmem in pcm.GetPartCrew())
                    {
                        if (crewmem != null)
                            actualCrew++;
                    }

                    if (actualCrew < crew)
                    {
                        return -PhysicsGlobals.KerbalCrewMass * (crew - actualCrew);
                    }

                }
            }

            return 0;
        }

        /// <summary>
        ///     Gets the first typed PartModule in the part's module list.
        /// </summary>
        public static T GetModule<T>(this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                PartModule pm = part.Modules[i];
                if (pm is T)
                    return (T)pm;
            }
            return null;
        }

        /// <summary>
        ///     Gets the cost of the part's maximum contained resources.
        /// </summary>
        public static double GetResourceCostMax(this Part part)
        {
            double cost = 0.0;
            for (int i = 0; i < part.Resources.dict.Count; ++i)
            {
                PartResource cachePartResource = part.Resources.dict.At(i);
                cost = cost + (cachePartResource.maxAmount * cachePartResource.info.unitCost);
            }
            return cost;
        }

        /// <summary>
        ///     Gets whether the part contains a PartModule.
        /// </summary>
        public static bool HasModule<T>(this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++)
            {
                if (part.Modules[i] is T)
                    return true;
            }
            return false;
        }

    }
}