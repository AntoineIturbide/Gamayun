using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.Controller
{
    public enum ButtonXbox
    {
        NONE,
        A,
        B,
        X,
        Y,
        LEFT_BUMPER,
        RIGHT_BUMPER,
        START,
        SELECT,
        DPAD_LEFT,
        DPAD_RIGHT,
        DPAD_UP,
        DPAD_DOWN,
        LEFT_STICK_Z,
        RIGHT_STICK_Z
    }

    public enum AxisPositiveXbox
    {
        NONE,
        LEFT_STICK_X_POS,
        LEFT_STICK_X_NEG,
        LEFT_STICK_Y_POS,
        LEFT_STICK_Y_NEG,
        RIGHT_STICK_X_POS,
        RIGHT_STICK_X_NEG,
        RIGHT_STICK_Y_POS,
        RIGHT_STICK_Y_NEG,
        LEFT_TRIGGER,
        RIGHT_TRIGGER
    }

    public enum AxisNegPosXbox
    {
        NONE,
        LEFT_STICK_X,
        LEFT_STICK_Y,
        RIGHT_STICK_X,
        RIGHT_STICK_Y,
        TRIGGERS
    }

    public class GamepadXbox : MonoBehaviour {

        #region Configuration
        /*****************
         * CONFIGURATION *
         *****************/
        [Serializable]
        internal class Configuration
        {
            [Header("Triggers")]
            // Left Trigger
            public string left_trigger_axis_name;
            // Right Trigger
            public string right_trigger_axis_name;
            
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

        // Trigger
        public InputAxis left_trigger;
        public InputAxis right_trigger;

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

            // Trigger
            left_trigger = new InputAxis(
                config.left_trigger_axis_name
                );
            right_trigger = new InputAxis(
                config.right_trigger_axis_name
                );

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

            // Triggers
            left_trigger.Tick();
            right_trigger.Tick();

            // Sticks
            left_stick.Tick();

            right_stick.Tick();

        }
        #endregion

        #region Get
        public IInputVector<float> GetAxisNegPos(AxisNegPosXbox axis)
        {
            switch (axis)
            {
                case AxisNegPosXbox.LEFT_STICK_X:
                    return left_stick.x;
                case AxisNegPosXbox.LEFT_STICK_Y:
                    return left_stick.y;
                case AxisNegPosXbox.RIGHT_STICK_X:
                    return right_stick.x;
                case AxisNegPosXbox.RIGHT_STICK_Y:
                    return right_stick.y;
                default:
                    throw new System.NotImplementedException();
            }
        }
        public IInputVector<float> GetAxisPositive(AxisPositiveXbox axis)
        {
            switch (axis)
            {
                case AxisPositiveXbox.LEFT_TRIGGER:
                    return left_trigger;
                case AxisPositiveXbox.RIGHT_TRIGGER:
                    return right_trigger;
                default:
                    throw new System.NotImplementedException();
            }
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
        public InputAxis   x;
        public InputAxis   y;

        private Vector2     _last_value;
        private Vector2     _value;

        public InputTwoAxis(InputAxis x_axis, InputAxis y_axis)
        {
            this.x = x_axis;
            this.y = y_axis;
        }

        public void Tick()
        {
            x.Tick();
            y.Tick();

            Vector2 new_value = new Vector2(x.value, y.value);

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