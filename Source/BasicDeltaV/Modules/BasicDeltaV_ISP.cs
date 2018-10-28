#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_ISP - Readout module for engine ISP
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
    public class BasicDeltaV_ISP : BasicDeltaV_Module
    {
		public BasicDeltaV_ISP(string t, BasicDeltaV_StagePanel p)
			: base(t, p)
        {
            _smallSize = true;
			_fixedOrder = 2;
			_simple = false;
			_dvModule = true;
            _showInBasic = true;
        }

        protected override string fieldUpdate()
		{
			if (_panel.Stage == null)
				return "---";

			double isp = _panel.Stage.isp;

			return result(isp);
        }

        protected override void fieldUpdate(StringBuilder sb)
        {
            if (_panel.Stage == null)
                return;

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.LabelColorHex);
            sb.Append(ModuleTitle);
            sb.Append(COLOR_CLOSE_TAG);
            
            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.ReadoutColorHex);
            result(sb, _panel.Stage.isp);
            sb.Append(COLOR_CLOSE_TAG);
        }

        private string result(double isp)
        {
            return string.Format("{0:N1}s", isp);
        }

        private void result(StringBuilder sb, double isp)
        {
            sb.AppendFormat("{0:N1}s", isp);
        }
    }
}
