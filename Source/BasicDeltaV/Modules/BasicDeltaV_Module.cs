#region License
/*
 * Basic DeltaV
 * 
 * BasicDeltaV_Module - Base class for all readout modules
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

using BasicDeltaV.Unity.Interface;
using BasicDeltaV.Simulation;

namespace BasicDeltaV.Modules
{
    public abstract class BasicDeltaV_Module : IBasicModule
    {
        private string _title;
        private string _moduleValue;
        protected bool _smallSize;
        protected int _order;
		protected int _fixedOrder;
		protected BasicDeltaV_StagePanel _panel;

        public BasicDeltaV_Module(string t, BasicDeltaV_StagePanel p)
        {
            _title = t;
			_panel = p;
        }
        
        public string ModuleTitle
        {
            get { return _title; }
        }

        public string ModuleText
        {
            get { return _moduleValue; }
        }

        public bool SmallSize
        {
            get { return _smallSize; }
        }

        public int Order
        {
            get { return _order; }
            set { _order = value; }
        }

		public int FixedOrder
		{
			get { return _fixedOrder; }
		}
        
        public void Update()
        {
            _moduleValue = fieldUpdate();
        }

        protected abstract string fieldUpdate();
    }
}