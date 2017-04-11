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

		/// <summary>
		///     Gets the cost of the part modules
		///     Same as stock but without mem allocation
		/// </summary>
		public static double GetModuleCostsNoAlloc(this Part part, float defaultCost)
		{
			float cost = 0f;
			for (int i = 0; i < part.Modules.Count; i++)
			{
				PartModule pm = part.Modules[i];
				if (pm is IPartCostModifier)
					cost += (pm as IPartCostModifier).GetModuleCost(defaultCost, ModifierStagingSituation.CURRENT);
			}
			return cost;
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

		public static ProtoModuleDecoupler GetProtoModuleDecoupler(this Part part)
		{
			PartModule cachePartModule = GetModule<ModuleDecouple>(part);
			if (cachePartModule == null)
			{
				cachePartModule = GetModule<ModuleAnchoredDecoupler>(part);
			}
			if (cachePartModule != null)
			{
				return new ProtoModuleDecoupler(cachePartModule);
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

		public class ProtoModuleDecoupler
		{
			private readonly PartModule module;

			public ProtoModuleDecoupler(PartModule module)
			{
				this.module = module;

				if (this.module is ModuleDecouple)
				{
					SetModuleDecouple();
				}
				else if (this.module is ModuleAnchoredDecoupler)
				{
					SetModuleAnchoredDecoupler();
				}
			}

			public double EjectionForce { get; private set; }
			public bool IsOmniDecoupler { get; private set; }
			public bool IsStageEnabled { get; private set; }

			private void SetModuleAnchoredDecoupler()
			{
				ModuleAnchoredDecoupler decoupler = module as ModuleAnchoredDecoupler;
				if (decoupler == null)
				{
					return;
				}

				EjectionForce = decoupler.ejectionForce;
				IsStageEnabled = decoupler.stagingEnabled;
			}

			private void SetModuleDecouple()
			{
				ModuleDecouple decoupler = module as ModuleDecouple;
				if (decoupler == null)
				{
					return;
				}

				EjectionForce = decoupler.ejectionForce;
				IsOmniDecoupler = decoupler.isOmniDecoupler;
				IsStageEnabled = decoupler.stagingEnabled;
			}
		}
	}
}