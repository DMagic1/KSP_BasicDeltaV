#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_TWR - Readout module for thrust to weight ratio
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
    public class BasicDeltaV_TWR : BasicDeltaV_Module
    {
        private bool _activeStage;

		public BasicDeltaV_TWR(string t, bool active, BasicDeltaV_StagePanel p)
			: base(t, p)
        {
            _activeStage = active;
            _smallSize = true;
			_fixedOrder = 1;
			_simple = true;
			_dvModule = true;
        }
        
        protected override void fieldUpdate(StringBuilder sb)
        {
            if (_panel.Stage == null)
                return;

            double twr = _panel.Stage.thrustToWeight;
            double maxTWR = _panel.Stage.maxThrustToWeight;

            if (_activeStage)
            {
                twr = _panel.Stage.actualThrustToWeight;
                maxTWR = _panel.Stage.thrustToWeight;
            }

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.LabelColorHex);
            sb.Append(_title);
            sb.Append(COLOR_CLOSE_TAG);

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.ReadoutColorHex);
            result(sb, twr, maxTWR);
            sb.Append(COLOR_CLOSE_TAG);
        }
        
        private void result(StringBuilder sb, double twr, double max)
        {
            if (twr == 0)
            {
                if (max < 10)
                    sb.AppendFormat("0({0})", max.ToString("F2"));
                else if (max < 100)
                    sb.AppendFormat("0({0})", max.ToString("F1"));
                else
                    sb.AppendFormat("0({0})", max.ToString("F0"));
            }
            else if (twr < 10 || max < 10)
                sb.AppendFormat("{0}({1})", twr.ToString("F2"), max.ToString("F2"));
            else if (twr < 100 || max < 100)
                sb.AppendFormat("{0}({1})", twr.ToString("F1"), max.ToString("F1"));
            else
                sb.AppendFormat("{0}({1})", twr.ToString("F0"), max.ToString("F0"));
        }
    }
}
