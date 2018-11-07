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

using System.Text;
using BasicDeltaV.Simulation;

namespace BasicDeltaV.Modules
{
    public class BasicDeltaV_DeltaV : BasicDeltaV_Module
    {
		public BasicDeltaV_DeltaV(string t, BasicDeltaV_StagePanel p)
			: base(t, p)
        {
            _smallSize = false;
			_fixedOrder = 1;
			_simple = false;
			_dvModule = false;
        }
        
        protected override void fieldUpdate(StringBuilder sb)
        {
            if (_panel.Stage == null)
                return;

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.LabelColorHex);
            sb.Append(_title);
            sb.Append(COLOR_CLOSE_TAG);

            double dv = _panel.Stage.deltaV;

            double total = _panel.Stage.inverseTotalDeltaV;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (_panel.CurrentStage)
                {
                    if (BasicDeltaV_Settings.Instance.ShowCurrentStageOnly)
                        total = BasicDeltaV.Instance.LastStage.totalDeltaV;
                    else
                        total = _panel.Stage.stageStartDeltaV;
                }
                else
                    total = BasicDeltaV.Instance.LastStage.totalDeltaV;
            }

            sb.AppendFormat(COLOR_OPEN_TAG, BasicDeltaV_Settings.ReadoutColorHex);
            result(sb, dv, total);
            sb.Append(COLOR_CLOSE_TAG);
        }
        
        private void result(StringBuilder sb, double dv, double tot)
        {
            if (dv >= 10000f || tot >= 10000f)
                sb.AppendFormat("{0}/{1}km/s", (dv / 1000).ToString("N2"), (tot / 1000).ToString("N2"));
            else
                sb.AppendFormat("{0}/{1}m/s", dv.ToString("N0"), tot.ToString("N0"));
        }
    }
}
