using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.Controller
{
    public class GamepadXbox : MonoBehaviour {

        #region Configuration
        /*****************
         * CONFIGURATION *
         *****************/
        [Serializable]
        internal class Configuration
        {
            [Header("Sticks")]
            // Left Stick
            // x
            public string left_stick_x_axis_name;
            public bool inverse_left_stick_x;
            // y
            public string left_stick_y_axis_name;
            public bool inverse_left_stick_y;
            // Right Stick
            // x
            public string right_stick_x_axis_name;
            public bool inverse_right_stick_x;
            // y
            public string right_stick_y_axis_name;
            public bool inverse_right_stick_y;
        }

        // Configuration Instance
        [SerializeField]
        internal Configuration config = new Configuration();
        #endregion


        #region State
        // Face buttons
        InputButton a;
        InputButton b;
        InputButton x;
        InputButton y;

        // Bumpers
        InputButton left_bumper;
        InputButton right_bumper;

        // Sticks
        public InputTwoAxis left_stick;
        public InputTwoAxis right_stick;
        #endregion


        #region Unity
        /*********
         * UNITY *
         *********/

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            Tick();
        }
        #endregion

        #region Logic
        public void Init()
        {
            // TODO
            // Face buttons

            // TODO
            // Bumpers

            // Sticks
            left_stick = new InputTwoAxis(
                new InputAxis(
                    config.left_stick_x_axis_name,
                    config.inverse_left_stick_x
                    ),
                new InputAxis(
                    config.left_stick_y_axis_name,
                    config.inverse_left_stick_y
                    )
                );

            right_stick = new InputTwoAxis(
                new InputAxis(
                    config.right_stick_x_axis_name,
                    config.inverse_right_stick_x
                    ),
                new InputAxis(
                    config.right_stick_y_axis_name,
                    config.inverse_right_stick_y
                    )
                );

        }
        public void Tick()
        {
            // TODO
            // Face buttons

            // TODO
            // Bumpers

            // Sticks
            left_stick.Tick();

            right_stick.Tick();

        }
        #endregion

    }

    public class InputButton : IInputButton
    {
        private string _button_name;

        private bool _last_value;
        private bool _value;

        public InputButton(string unity_button_name)
        {
            this._button_name = unity_button_name;
        }

        public void Tick()
        {
            bool new_value = Input.GetButton(_button_name);
            _last_value = _value;
            _value = new_value;
        }
        
        bool IInput.has_changed
        {
            get
            {
                return _value != _last_value;
            }
        }

        bool IInputButton.is_pressed
        {
            get
            {
                return _value == true;
            }
        }

        bool IInputButton.is_released
        {
            get
            {
                return _value == false;
            }
        }

        bool IInputButton.was_pressed
        {
            get
            {
                return _value == true && _last_value == false;
            }
        }

        bool IInputButton.was_released
        {
            get
            {
                return _value == false && _last_value == true;
            }
        }
    }

    public class InputAxis : IInputVector<float>
    {
        private string _axis_name;

        private float _last_value;
        private float _value;

        private bool _inverse;

        public InputAxis(string unity_axis_name, bool inverse_axis = false)
        {
            this._axis_name = unity_axis_name;
        }

        public void Tick()
        {
            float new_value = Input.GetAxis(_axis_name);
            if (_inverse)
            {
                // @improvement : Check if the axis range is between 0 & 1 or -1 & 1 to remap acordingly
                new_value = -new_value;
            }
            _last_value = _value;
            _value = new_value;
        }

        public bool has_changed
        {
            get
            {
                return _value != _last_value;
            }
        }

        public float value
        {
            get
            {
                return _value;
            }
        }

        public float last_value
        {
            get
            {
                return _last_value;
            }
        }
    }

    public class InputTwoAxis : IInputVector<Vector2>
    {
        private InputAxis   _x;
        private InputAxis   _y;

        private Vector2     _last_value;
        private Vector2     _value;

        public InputTwoAxis(InputAxis x_axis, InputAxis y_axis)
        {
            this._x = x_axis;
            this._y = y_axis;
        }

        public void Tick()
        {
            _x.Tick();
            _y.Tick();

            Vector2 new_value = new Vector2(_x.value, _y.value);

            _last_value = _value;
            _value      = new_value;
        }

        public bool has_changed
        {
            get
            {
                return _value != _last_value;
            }
        }

        public Vector2 value
        {
            get
            {
                return _value;
            }
        }

        public Vector2 last_value
        {
            get
            {
                return _last_value;
            }
        }
    }
}