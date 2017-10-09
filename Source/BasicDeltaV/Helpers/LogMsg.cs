// 
//     Code From Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System.Text;

namespace BasicDeltaV
{
    public class LogMsg
    {
        public StringBuilder buf;

        public LogMsg()
        {
            this.buf = new StringBuilder();
        }

        public void Flush()
        {
            if (this.buf.Length > 0)
            {
                BasicLogger.Log(this.buf);
            }
            this.buf.Length = 0;
        }
		
		public LogMsg AppendLine(string val)
		{
			buf.AppendLine(val);
			return this;
		}

		public LogMsg Append<T>(T val)
		{
			buf.Append(val);
			return this;
		}

		public LogMsg Append<T, U>(T val, U val2)
		{
			buf.Append(val);
			buf.Append(val2);
			return this;
		}

		public LogMsg Append<T, U, V>(T val, U val2, V val3)
		{
			buf.Append(val);
			buf.Append(val2);
			buf.Append(val3);
			return this;
		}

		public LogMsg Append<T, U, V, W>(T val, U val2, V val3, W val4)
		{
			buf.Append(val);
			buf.Append(val2);
			buf.Append(val3);
			buf.Append(val4);
			return this;
		}

		public LogMsg Append<T, U, V, W, X>(T val, U val2, V val3, W val4, X val5)
		{
			buf.Append(val);
			buf.Append(val2);
			buf.Append(val3);
			buf.Append(val4);
			buf.Append(val5);
			return this;
		}

		public LogMsg AppendLine<T>(T val)
		{
			buf.Append(val);
			buf.AppendLine();
			return this;
		}

		public LogMsg AppendLine<T, U>(T val, U val2)
		{
			buf.Append(val);
			buf.Append(val2);
			buf.AppendLine();
			return this;
		}

		public LogMsg AppendLine<T, U, V>(T val, U val2, V val3)
		{
			buf.Append(val);
			buf.Append(val2);
			buf.Append(val3);
			buf.AppendLine();
			return this;
		}

		public LogMsg AppendLine<T, U, V, W>(T val, U val2, V val3, W val4)
		{
			buf.Append(val);
			buf.Append(val2);
			buf.Append(val3);
			buf.Append(val4);
			buf.AppendLine();
			return this;
		}

		public LogMsg AppendLine<T, U, V, W, X>(T val, U val2, V val3, W val4, X val5)
		{
			buf.Append(val);
			buf.Append(val2);
			buf.Append(val3);
			buf.Append(val4);
			buf.Append(val5);
			buf.AppendLine();
			return this;
		}

		public LogMsg EOL()
		{
			buf.AppendLine();
			return this;
		}
    }
}
