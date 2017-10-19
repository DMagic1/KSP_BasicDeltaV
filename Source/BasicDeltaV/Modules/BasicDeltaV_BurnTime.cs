#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_BurnTime - Readout module for stage burn time
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

using System;
using System.Text;

namespace BasicDeltaV.Modules
{
    public class BasicDeltaV_BurnTime : BasicDeltaV_Module
    {
		private static int[] times = new int[5];
		private static string[] units = new string[5] { "s", "m", "h", "d", "y" };

        public BasicDeltaV_BurnTime(string t, BasicDeltaV_StagePanel p)
			: base (t, p)
        {
            _smallSize = false;
            _fixedOrder = 2;
			_simple = false;
			_dvModule = true;
        }

        protected override string fieldUpdate()
		{
			if (_panel.Stage == null)
				return "---";

			double time = _panel.Stage.time;

            return result(time, 3);
        }

		private string result(double time, int values)
		{
			if (time == 0)
				return "0s";

			if (double.IsNaN(time) || double.IsInfinity(time))
				return "---";

			if (time >= int.MaxValue)
				return "---";
			else if (time <= int.MinValue)
				return "---";

			SetTimes(time);

			StringBuilder sb = StringBuilderCache.Acquire();

			for (int i = times.Length -1; i >= 0; i--)
			{
				int t = times[i];

				if (t == 0)
				{
					if (i < times.Length - 1 && times[i + 1] == 0)
						continue;
					else if (i >= times.Length - 1)
						continue;
				}

				if (values <= 0)
					continue;

				sb.AppendFormat("{0}{1}", Math.Abs(t), units[i]);

				if (values > 1 && i > 0)
					sb.Append(",");

				values--;
			}

			return sb.ToStringAndRelease();
		}

		private void SetTimes(double d)
		{
			int year = KSPUtil.dateTimeFormatter.Year;
			int day = KSPUtil.dateTimeFormatter.Day;

			times[4] = (int)(d / year);
			d -= times[4] * year;

			times[3] = (int)(d / day);
			d -= times[3] * day;

			times[2] = (int)(d / 3600);
			d -= times[2] * 3600;

			times[1] = (int)(d / 60);
			d -= times[1] * 60;

			times[0] = (int)d;
		}

    }
}
