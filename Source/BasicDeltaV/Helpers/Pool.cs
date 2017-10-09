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

using System.Collections.Generic;

namespace BasicDeltaV
{
    /// <summary>
    ///     Pool of object
    /// </summary>
    public class Pool<T> {
        
        private readonly Stack<T> values = new Stack<T>();

        private readonly CreateDelegate<T> create;
        private readonly ResetDelegate<T> reset;

        public delegate R CreateDelegate<out R>();
        public delegate void ResetDelegate<in T1>(T1 a);
        
        /// <summary>
        ///     Creates an empty pool with the specified object creation and reset delegates.
        /// </summary>
        public Pool(CreateDelegate<T> create, ResetDelegate<T> reset) {
            this.create = create;
            this.reset = reset;
        }

        /// <summary>
        ///     Borrows an object from the pool.
        /// </summary>
        public T Borrow() {
            lock (values) {
                return values.Count > 0 ? values.Pop() : create();
            }
        }
        
        /// <summary>
        ///     Release an object, reset it and returns it to the pool.
        /// </summary>
        public void Release(T value) {
            reset(value);
            lock (values) {
                values.Push(value);
            }
        }
        
        /// <summary>
        ///     Current size of the pool.
        /// </summary>
        public int Count()
        {
            return values.Count;
        }
    }
}