using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public class Smooth<T>
    {

        #region State
        private T _current_value;
        private T _target_value;
        #endregion

        #region Tick
        public delegate T TickHandler(T current, T target, float dt);
        TickHandler _tick_function;
        public void tick(float dt)
        {
            _current_value = _tick_function(_current_value, _target_value, dt);
        }
        public void tick(float dt, TickHandler tick_function)
        {
            _current_value = tick_function(_current_value, _target_value, dt);
        }
        #endregion

        #region Initialisation
        /// <summary>
        /// Constructor of a smooth object.
        /// </summary>
        /// <param name="startValue">The start value of this smooth object.</param>
        /// <param name="tickFunction">The tick function that refresh the value depending of dt (deltatime).</param>
        public Smooth(T startValue, TickHandler tickFunction)
        {
            _target_value = _current_value = startValue;
            this._tick_function = tickFunction;
        }
        #endregion

        #region Use
        public T get_value()
        {
            return _current_value;
        }
        public T get_target()
        {
            return _target_value;
        }

        public void set_value_now(T value)
        {
            this._current_value = value;
        }

        public void set_target(T target)
        {
            this._target_value = target;
        }

        #endregion

    }
}