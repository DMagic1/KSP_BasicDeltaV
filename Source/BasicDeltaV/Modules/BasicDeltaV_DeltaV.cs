#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_DeltaV - Readout module for deltaV
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
    public class BasicDeltaV_DeltaV : BasicDeltaV_Module
    {
		public BasicDeltaV_DeltaV(string t, BasicDeltaV_StagePanel p)
			: base(t, p)
        {
            _smallSize = false;
			_fixedOrder = 1;
        }

        protected override string fieldUpdate()
		{
			if (_panel.Stage == null)
				return "---";

			double dv = _panel.Stage.deltaV;
			double total = _panel.Stage.totalDeltaV;

			return result(dv, total);
        }

        private string result(double dv, double tot)
        {
			if (dv >= 10000f || tot >= 10000f)
				return string.Format("{0:N2}/{1:N2}km/s", dv / 1000, tot / 1000);

            return string.Format("{0:N0}/{1:N0}m/s", dv,tot);
        }
    }
}
