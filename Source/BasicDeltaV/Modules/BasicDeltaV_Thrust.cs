#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_Thrust - Readout module for engine thrust
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
    public class BasicDeltaV_Thrust : BasicDeltaV_Module
    {
		public BasicDeltaV_Thrust(string t, BasicDeltaV_StagePanel p)
			: base(t, p)
        {
            _smallSize = true;
			_fixedOrder = 3;
        }

        protected override string fieldUpdate()
		{
			if (_panel.Stage == null)
				return "---";

			double thrust = _panel.Stage.thrust;

            return result(thrust);
        }

		private string result(double thrust)
        {
			if (thrust < 50)
				return string.Format("{0:N2}kN", thrust);
			else if (thrust < 1000)
				return string.Format("{0:N1}kN", thrust);
			else
				return string.Format("{0:N0}kN", thrust);
        }
    }
}
