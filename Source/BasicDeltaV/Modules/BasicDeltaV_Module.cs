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

using System.Text;
using BasicDeltaV.Unity.Interface;
using BasicDeltaV.Simulation;

namespace BasicDeltaV.Modules
{
    public abstract class BasicDeltaV_Module : IBasicModule
    {
        protected const string COLOR_OPEN_TAG = "<color=#{0}>";
        protected const string COLOR_CLOSE_TAG = "</color>";

        private string _title;
        private string _moduleValue;
        protected bool _smallSize;
		protected bool _dvModule;
        protected bool _lineBreak;
        protected bool _showInBasic;
        protected int _order;
		protected int _fixedOrder;
		protected bool _simple;
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

		public bool DVModule
		{
			get { return _dvModule; }
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

        public bool ShowInBasic
        {
            get { return _showInBasic; }
        }

        public bool LineBreak
        {
            get { return _lineBreak; }
            set { _lineBreak = value; }
        }

        public void Update()
        {
            _moduleValue = fieldUpdate();
        }

        public void Update(StringBuilder sb)
        {
            fieldUpdate(sb);
        }

        protected abstract string fieldUpdate();

        protected abstract void fieldUpdate(StringBuilder sb);
    }
}