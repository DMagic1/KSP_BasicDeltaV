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
        }
        
        protected override void fieldUpdate(StringBuilder sb)
        {
            if (_panel.Stage == null)
                return;

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.LabelColorHex);
            sb.Append(_title);
            sb.Append(COLOR_CLOSE_TAG);

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.ReadoutColorHex);

            if (_activeStage)
                activeResult(sb, _panel.Stage.actualThrust, _panel.Stage.thrust);
            else
                result(sb, _panel.Stage.thrust);

            sb.Append(COLOR_CLOSE_TAG);
        }
        
        private void result(StringBuilder sb, double thrust)
        {
            if (thrust < 10)
                sb.AppendFormat("{0}kN", thrust.ToString("N3"));
            else if (thrust < 100)
                sb.AppendFormat("{0}kN", thrust.ToString("N2"));
            else if (thrust < 1000)
                sb.AppendFormat("{0}kN", thrust.ToString("N1"));
            else if (thrust < 10000)
                sb.AppendFormat("{0}kN", thrust.ToString("N0"));
            else if (thrust < 100000)
                sb.AppendFormat("{0}MN", (thrust / 1000).ToString("N2"));
            else
                sb.AppendFormat("{0}MN", (thrust / 1000).ToString("N1"));
        }

        private void activeResult(StringBuilder sb, double thrust, double max)
        {
            if (thrust == 0)
                sb.AppendFormat("0kN({0})", max.ToString("N0"));
            else if (thrust < 10)
                sb.AppendFormat("{0}kN({1})", thrust.ToString("N2"), max.ToString("N0"));
            else if (thrust < 100)
                sb.AppendFormat("{0}kN({1})", thrust.ToString("N1"), max.ToString("N0"));
            else if (thrust < 10000)
                sb.AppendFormat("{0}kN({1})", thrust.ToString("N0"), max.ToString("N0"));
            else if (thrust < 100000)
                sb.AppendFormat("{0}MN({1})", (thrust / 1000).ToString("N1"), (max / 1000).ToString("N0"));
            else
                sb.AppendFormat("{0}MN({1})", (thrust / 1000).ToString("N0"), (max / 1000).ToString("N0"));
        }
    }
}
