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
            _showInBasic = false;
        }

        protected override string fieldUpdate()
        {
			if (_panel.Stage == null)
				return "---";

			double twr = _panel.Stage.thrustToWeight;
			double maxTWR = _panel.Stage.maxThrustToWeight;

            if (_activeStage)
            {
                twr = _panel.Stage.actualThrustToWeight;
                maxTWR = _panel.Stage.thrustToWeight;
            }

			return result(twr, maxTWR);
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
            sb.Append(ModuleTitle);
            sb.Append(COLOR_CLOSE_TAG);

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.ReadoutColorHex);
            result(sb, twr, maxTWR);
            sb.Append(COLOR_CLOSE_TAG);
        }

        private string result(double twr, double max)
        {
            if (twr == 0)
            {
                if (max < 10)
                    return string.Format("0({0:F2})", max);
                else if (max < 100)
                    return string.Format("0({0:F1})", max);
                else
                    return string.Format("0({0:F0})", max);
            }

            if (twr < 10 || max < 10)
                return string.Format("{0:F2}({1:F2})", twr, max);
            else if (twr < 100 || max < 100)
                return string.Format("{0:F1}({1:F1})", twr, max);

            return string.Format("{0:F0}({1:F0})", twr, max);
        }

        private void result(StringBuilder sb, double twr, double max)
        {
            if (twr == 0)
            {
                if (max < 10)
                    sb.AppendFormat("0({0:F2})", max);
                else if (max < 100)
                    sb.AppendFormat("0({0:F1})", max);
                else
                    sb.AppendFormat("0({0:F0})", max);
            }
            else if (twr < 10 || max < 10)
                sb.AppendFormat("{0:F2}({1:F2})", twr, max);
            else if (twr < 100 || max < 100)
                sb.AppendFormat("{0:F1}({1:F1})", twr, max);
            else
                sb.AppendFormat("{0:F0}({1:F0})", twr, max);
        }
    }
}
