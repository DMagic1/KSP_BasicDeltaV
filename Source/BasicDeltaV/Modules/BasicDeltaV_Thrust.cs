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

using System.Text;

namespace BasicDeltaV.Modules
{
    public class BasicDeltaV_Thrust : BasicDeltaV_Module
    {
        private bool _activeStage;

        public BasicDeltaV_Thrust(string t, bool active, BasicDeltaV_StagePanel p)
			: base(t, p)
        {
            _activeStage = active;
            _smallSize = true;
			_fixedOrder = 3;
			_simple = true;
			_dvModule = true;
            _showInBasic = true;
        }

        protected override string fieldUpdate()
		{
			if (_panel.Stage == null)
				return "---";
            
            if (_activeStage)
                return activeResult(_panel.Stage.actualThrust, _panel.Stage.thrust);
            else
                return result(_panel.Stage.thrust);
        }

        protected override void fieldUpdate(StringBuilder sb)
        {
            if (_panel.Stage == null)
                return;

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.LabelColorHex);
            sb.Append(ModuleTitle);
            sb.Append(COLOR_CLOSE_TAG);

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.ReadoutColorHex);

            if (_activeStage)
                activeResult(sb, _panel.Stage.actualThrust, _panel.Stage.thrust);
            else
                result(sb, _panel.Stage.thrust);

            sb.Append(COLOR_CLOSE_TAG);
        }

        private string result(double thrust)
        {
			if (thrust < 10)
				return string.Format("{0:N3}kN", thrust);
			else if (thrust < 100)
				return string.Format("{0:N2}kN", thrust);
			else if (thrust < 1000)
				return string.Format("{0:N1}kN", thrust);
			else if (thrust < 10000)
				return string.Format("{0:N0}kN", thrust);
			else if (thrust < 100000)
				return string.Format("{0:N2}MN", thrust / 1000);
			else
				return string.Format("{0:N1}MN", thrust / 1000);
        }

        private string activeResult(double thrust, double max)
        {
            if (thrust == 0)
                return string.Format("0kN({0:N0})", max);
            else if (thrust < 10)
                return string.Format("{0:N2}kN({1:N0})", thrust, max);
            else if (thrust < 100)
                return string.Format("{0:N1}kN({1:N0})", thrust, max);
            else if (thrust < 10000)
                return string.Format("{0:N0}kN({1:N0})", thrust, max);
            else if (thrust < 100000)
                return string.Format("{0:N1}MN({1:N0})", thrust / 1000, max / 1000);
            else
                return string.Format("{0:N0}MN({1:N0})", thrust / 1000, max / 1000);
        }

        private void result(StringBuilder sb, double thrust)
        {
            if (thrust < 10)
                sb.AppendFormat("{0:N3}kN", thrust);
            else if (thrust < 100)
                sb.AppendFormat("{0:N2}kN", thrust);
            else if (thrust < 1000)
                sb.AppendFormat("{0:N1}kN", thrust);
            else if (thrust < 10000)
                sb.AppendFormat("{0:N0}kN", thrust);
            else if (thrust < 100000)
                sb.AppendFormat("{0:N2}MN", thrust / 1000);
            else
                sb.AppendFormat("{0:N1}MN", thrust / 1000);
        }

        private void activeResult(StringBuilder sb, double thrust, double max)
        {
            if (thrust == 0)
                sb.AppendFormat("0kN({0:N0})", max);
            else if (thrust < 10)
                sb.AppendFormat("{0:N2}kN({1:N0})", thrust, max);
            else if (thrust < 100)
                sb.AppendFormat("{0:N1}kN({1:N0})", thrust, max);
            else if (thrust < 10000)
                sb.AppendFormat("{0:N0}kN({1:N0})", thrust, max);
            else if (thrust < 100000)
                sb.AppendFormat("{0:N1}MN({1:N0})", thrust / 1000, max / 1000);
            else
                sb.AppendFormat("{0:N0}MN({1:N0})", thrust / 1000, max / 1000);
        }
    }
}
