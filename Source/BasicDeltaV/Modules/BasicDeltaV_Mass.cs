#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_Mass - Readout module for vessel mass
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

namespace BasicDeltaV.Modules
{
    public class BasicDeltaV_Mass : BasicDeltaV_Module
    {
		public BasicDeltaV_Mass(string t, BasicDeltaV_StagePanel p)
			: base(t, p)
        {
            _smallSize = false;
			_fixedOrder = 3;
        }

        protected override string fieldUpdate()
		{
			if (_panel.Stage == null)
				return "---";

			double mass = _panel.Stage.mass;
			double total = _panel.Stage.totalMass;

			return result(mass, total);
        }

        private string result(double mass, double tot)
        {
			if (mass >= 100f || tot >= 100f)
				return string.Format("{0:N2}/{1:N2}t", mass, tot);

			mass *= 1000.0;
			tot *= 1000.0;

			return string.Format("{0:N0}/{1:N0}kg", mass, tot);
        }
    }
}
