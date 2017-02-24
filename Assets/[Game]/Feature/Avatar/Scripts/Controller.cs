using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Avatar
{
    public class Controller : MonoBehaviour {

        #region Configuration
        /*****************
         * CONFIGURATION *
         *****************/
        [Serializable]
        public class Configuration
        {
            [Header("Controlled Character")]
            // Character
            public Character character;

            [Header("Snap")]
            // Snap
            public string snapUnityButton;
            
            [Header("Trust")]
            // Acceleration / Deceleration
            public string accelerationDecelerationUnityAxis;
            public bool inverseAaccelerationDecelerationAxis;

            [Header("Rotation")]
            // Anteroposterior Axis
            public string anteroposteriorAxisRotationUnityAxis;
            public bool inverseAnteroposteriorAxisRotation;
            // Left-Right Axis
            public string leftRightAxisRotationUnityAxis;
            public bool inverseLeftRightAxisRotation;
            // Left-Right Axis Additive
            public string leftRightAxisRotationAdditiveUnityAxis;
            // Dorsoventral Axis
            public string dorsoventralAxisRotationUnityAxis;
            public bool inverseDorsoventralAxisRotation;
        }

        // Configuration Instance
        public Configuration config = new Configuration();
        #endregion


        #region State
        /*********
         * STATE *
         *********/
        public class State
        {
            // Trust
            public Smooth<float> thrust;
            private float trust_tick(float current, float target, float dt)
            {
                const float speed = 2f;
                return Mathf.MoveTowards(current, target, dt * speed);
            }

            // Left-Right Axis
            public Smooth<float> left_right_x;
            private float left_right_x_tick(float current, float target, float dt)
            {
                const float speed = 2f;
                return Mathf.MoveTowards(current, target, dt * speed);
            }

            // Dorsoventral Axis
            public Smooth<float> dorsoventral_y;
            private float dorsoventral_y_tick(float current, float target, float dt)
            {
                const float speed = 2f;
                return Mathf.MoveTowards(current, target, dt * speed);
            }

            // Anteroposterior Axis
            public Smooth<float> anteroposterio_z;
            private float anteroposterio_z_tick(float current, float target, float dt)
            {
                const float speed = 2f;
                return Mathf.MoveTowards(current, target, dt * speed);
            }


            public void Init()
            {
                // Trust
                thrust = new Smooth<float>(0, trust_tick);

                // Left-Right Axis
                left_right_x = new Smooth<float>(0, left_right_x_tick);

                // Dorsoventral Axis
                dorsoventral_y = new Smooth<float>(0, dorsoventral_y_tick);

                // Anteroposterior Axis
                anteroposterio_z = new Smooth<float>(0, anteroposterio_z_tick);
            }

            public void TickSmooth(float dt)
            {
                // Trust
                thrust.tick(dt);

                // Left-Right Axis
                left_right_x.tick(dt);

                // Dorsoventral Axis
                dorsoventral_y.tick(dt);

                // Anteroposterior Axis
                anteroposterio_z.tick(dt);
            }

        }

        public State state = new State();
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
            HandleInputs();
            state.TickSmooth(Time.deltaTime);
            Control(config.character, Time.deltaTime);
        }
        #endregion

        #region Behaviour
        /*************
         * BEHAVIOUR *
         *************/
        
        private void Init()
        {
            // Initialize State
            state.Init();
        }
        
        private void HandleInputs()
        {
            Character chara = config.character;
            float @float;

            float thrust = Input.GetAxis(config.accelerationDecelerationUnityAxis);
            float dorsoventral = Input.GetAxis(config.dorsoventralAxisRotationUnityAxis);
            float anteroposterior = Input.GetAxis(config.anteroposteriorAxisRotationUnityAxis);
            float leftRight = Input.GetAxis(config.leftRightAxisRotationUnityAxis);
            float leftRightAdditive = Input.GetAxis(config.leftRightAxisRotationAdditiveUnityAxis);

            if (config.inverseDorsoventralAxisRotation)
                dorsoventral = -dorsoventral;
            if (config.inverseAnteroposteriorAxisRotation)
                anteroposterior = -anteroposterior;

            //// Thrust
            @float = thrust;
            state.thrust.set_target(@float);
            //dist = Mathf.Abs(chara.state.accelerationDeceleration - axisInput);
            //chara.state.accelerationDeceleration =
            //    Mathf.MoveTowards(chara.state.accelerationDeceleration, axisInput, (dist * 1f + 0.125f) * Time.deltaTime);


            // Dorsoventral Axis (y)
            @float = dorsoventral;
            @float *= 1 - (anteroposterior * anteroposterior);
            state.dorsoventral_y.set_target(@float);
            //axisInput *= -Mathf.Cos(axisInput * Mathf.PI) * 0.5f + 0.5f;
            //dist = Mathf.Abs(chara.state.dorsoventralAxisRotationInterpolation - axisInput);
            //chara.state.dorsoventralAxisRotationInterpolation =
            //    Mathf.MoveTowards(chara.state.dorsoventralAxisRotationInterpolation, axisInput, (dist * 4f + 0.5f) * Time.deltaTime);

            // Anteroposterior Axis (z) Rotation
            @float = anteroposterior;
            @float += dorsoventral * Math.Abs(anteroposterior);
            //axisInput *= 1 + Mathf.Abs(dorsoventral);
            @float /= 2f;
            state.anteroposterio_z.set_target(@float);
            //chara.state.anteroposteriorAxisRotationSpeed =
            //    axisInput *                
            //    chara.config.anteroposteriorAxisRotationSpeed
            //    ;

            // Left-Right Axis (x)
            @float = leftRight + leftRightAdditive;
            @float /= 2f;
            @float = Mathf.Tan((@float * Mathf.PI) / 4f);
            if (config.inverseLeftRightAxisRotation)
                @float = -@float;
            state.left_right_x.set_target(@float);

            /*
            chara.state.leftRightAxisRotationSpeed =
                axisInput *
                chara.config.leftRightAxisRotationSpeed
                ;
            */
        }

        private void Control(Character chara, float dt)
        {
            float @float;
            float dist;

            // Thrust
            @float = state.thrust.get_value();
            dist = Mathf.Abs(chara.state.accelerationDeceleration - @float);
            chara.state.accelerationDeceleration =
                Mathf.MoveTowards(chara.state.accelerationDeceleration, @float, (dist * 1f + 0.125f) * Time.deltaTime);

            // Left-Right Axis
            @float = state.left_right_x.get_value();
            chara.state.leftRightAxisRotationSpeed =
                @float *
                chara.config.leftRightAxisRotationSpeed
                ;

            // Dorsoventral Axis (y)
            @float = state.dorsoventral_y.get_value();
            dist = Mathf.Abs(chara.state.dorsoventralAxisRotationInterpolation - @float);
            chara.state.dorsoventralAxisRotationInterpolation =
                Mathf.MoveTowards(chara.state.dorsoventralAxisRotationInterpolation, @float, (dist * 4f + 0.5f) * Time.deltaTime);

            // Anteroposterior Axis Rotation
            @float = state.anteroposterio_z.get_value();
            chara.state.anteroposteriorAxisRotationSpeed =
                @float *
                chara.config.anteroposteriorAxisRotationSpeed
                ;

        }
        #endregion

    }
}