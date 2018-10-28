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

using System.Text;

namespace BasicDeltaV.Modules
{
    public class BasicDeltaV_Mass : BasicDeltaV_Module
    {
		public BasicDeltaV_Mass(string t, BasicDeltaV_StagePanel p)
			: base(t, p)
        {
            _smallSize = false;
			_fixedOrder = 3;
			_simple = true;
			_dvModule = false;
            _showInBasic = true;
        }

        protected override string fieldUpdate()
		{
			if (_panel.Stage == null)
				return "---";

			double mass = _panel.Stage.mass;
			double total = _panel.Stage.totalMass;

			return result(mass, total);
        }

        protected override void fieldUpdate(StringBuilder sb)
        {
            if (_panel.Stage == null)
                return;

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.LabelColorHex);
            sb.Append(ModuleTitle);
            sb.Append(COLOR_CLOSE_TAG);

            double mass = _panel.Stage.mass;
            double total = _panel.Stage.totalMass;

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.ReadoutColorHex);
            result(sb, mass, total);
            sb.Append(COLOR_CLOSE_TAG);
        }

        private string result(double mass, double tot)
        {
			if (mass >= 100f || tot >= 100f)
				return string.Format("{0:N2}/{1:N2}t", mass, tot);

			return string.Format("{0:N0}/{1:N0}kg", mass * 1000, tot * 1000);
        }

        private void result(StringBuilder sb, double mass, double tot)
        {
            if (mass >= 100f || tot >= 100f)
                sb.AppendFormat("{0:N2}/{1:N2}t", mass, tot);
            else
                sb.AppendFormat("{0:N0}/{1:N0}kg", mass * 1000, tot * 1000);
        }
    }
}
