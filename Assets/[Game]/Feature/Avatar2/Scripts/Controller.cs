using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Avatar2
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

            [Header("Input")]
            public Utility.Controller.GamepadXbox xbox_gamepad;

            
            [Header("Railshooter")]
            // Cursor Displacement X
            public Utility.Controller.AxisNegPosXbox cursorDisplacementX = Utility.Controller.AxisNegPosXbox.LEFT_STICK_X;
            public bool inverseCursDispX;

            // Cursor Displacement Y
            public Utility.Controller.AxisNegPosXbox cursorDisplacementY = Utility.Controller.AxisNegPosXbox.LEFT_STICK_Y;
            public bool inverseCursDispY;
            public float timeToReachTargetDisp = 2f;

            [Header("Orientation")]
            // Rotation Around X
            public Utility.Controller.AxisNegPosXbox rotationAroundX = Utility.Controller.AxisNegPosXbox.RIGHT_STICK_Y;
            public bool inverseRotAroundX = false;
            public float timeToReachTargetRotX = 0.75f;

            // Rotation Around Y
            public Utility.Controller.AxisNegPosXbox rotationAroundY = Utility.Controller.AxisNegPosXbox.RIGHT_STICK_X;
            public bool inverseRotAroundY = true;
            public float timeToReachTargetRotY = 0.75f;


            [Header("Wings")]
            public Utility.Controller.AxisPositiveXbox wings = Utility.Controller.AxisPositiveXbox.RIGHT_TRIGGER;
            public float timeToReachTargetWings = 0.75f;

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
            #region Smoothing
            ///////////////
            // Smoothing //
            ///////////////

            // Rotation Around X
            public Smooth<float> rotation_around_x;
            public float rotation_around_x_time_to_reach_target;
            private float rotation_around_x_stick_tick(float current, float target, float dt)
            {
                float time_to_reach_target = rotation_around_x_time_to_reach_target;
                return time_to_reach_target > 0 ?
                    Mathf.MoveTowards(current, target, dt * (2f * (1f / time_to_reach_target))) :
                    target;
            }

            // Rotation Around Y
            public Smooth<float> rotation_around_y;
            public float rotation_around_y_time_to_reach_target;
            private float rotation_around_y_tick(float current, float target, float dt)
            {
                float time_to_reach_target = rotation_around_y_time_to_reach_target;
                return time_to_reach_target > 0 ?
                    Mathf.MoveTowards(current, target, dt * (2f * (1f / time_to_reach_target))) :
                    target;
            }

            // Cursor Displacement
            public Smooth<Vector2> cursor_displacement;
            public float cursor_displacement_time_to_reach_target;
            private Vector2 cursor_displacement_tick(Vector2 current, Vector2 target, float dt)
            {
                const float linear_speed = 0.0f;
                float time_to_reach_target = cursor_displacement_time_to_reach_target;
                float dist_to_target = Vector2.Distance(current, target);
                return time_to_reach_target > 0 ?
                    Vector2.MoveTowards(current, target, dt * ((dist_to_target * 2f * (1f / time_to_reach_target)) + linear_speed)) :
                    target;
            }

            // X Translation
            public Smooth<float> x_translation_hit_bounds;
            private float x_translation_hit_bounds_tick(float current, float target, float dt)
            {
                const float time_to_reach_target = 0f;
                return time_to_reach_target > 0 ?
                    Mathf.MoveTowards(current, target, dt * (1f / time_to_reach_target)) :
                    target;
            }

            // Y Translation
            public Smooth<float> y_translation_hit_bounds;
            private float y_translation_hit_bounds_tick(float current, float target, float dt)
            {
                const float time_to_reach_target = 0f;
                return time_to_reach_target > 0 ?
                    Mathf.MoveTowards(current, target, dt * (1f / time_to_reach_target)) :
                    target;
            }

            public void Init(
                float rotation_around_x_stick_time_to_reach_target,
                float rotation_around_y_time_to_reach_target,
                float cursor_displacement_time_to_reach_target
                )
            {
                // Cursor Displacement (Rail Shooter)
                cursor_displacement = new Smooth<Vector2>(Vector2.zero, cursor_displacement_tick);
                this.cursor_displacement_time_to_reach_target = cursor_displacement_time_to_reach_target;

                // Rotation Around X
                rotation_around_x = new Smooth<float>(0, rotation_around_x_stick_tick);
                this.rotation_around_x_time_to_reach_target = rotation_around_x_stick_time_to_reach_target;
                // Rotation Around Y
                rotation_around_y = new Smooth<float>(0, rotation_around_y_tick);
                this.rotation_around_y_time_to_reach_target = rotation_around_y_time_to_reach_target;

                // X Translation
                x_translation_hit_bounds = new Smooth<float>(0, x_translation_hit_bounds_tick);
                // Y Translation
                y_translation_hit_bounds = new Smooth<float>(0, y_translation_hit_bounds_tick);
            }

            public void TickSmooth(float dt)
            {
                // Cursor Displacement
                cursor_displacement.tick(dt);

                // Railshooter
                y_translation_hit_bounds.tick(dt);
                x_translation_hit_bounds.tick(dt);

                // Orientation
                // Rotation Around X
                rotation_around_x.tick(dt);
                // Rotation Around Y
                rotation_around_y.tick(dt);
            }
            #endregion

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
            state.Init(
                config.timeToReachTargetRotX,
                config.timeToReachTargetRotY,
                config.timeToReachTargetDisp                
                );
        }
        
        private void HandleInputs()
        {
            var gamepad = config.xbox_gamepad;

            // Cursor Displacement
            //state.cursor_displacement.set_target(Vector2.Scale(gamepad.left_stick.value, new Vector2(1, 1)));
            state.cursor_displacement.set_target(
                new Vector2(
                    (config.inverseCursDispX ? -1 : 1) * gamepad.GetAxisNegPos(config.cursorDisplacementX),
                    (config.inverseCursDispY ? -1 : 1) * gamepad.GetAxisNegPos(config.cursorDisplacementY)                    
                    )                
                );

            // Rotation Around X
            state.rotation_around_x.set_target(
                (config.inverseRotAroundX ? -1 : 1) * gamepad.GetAxisNegPos(config.rotationAroundX)
                );

            // Rotation Around Y
            state.rotation_around_y.set_target(
                (config.inverseRotAroundY ? -1 : 1) * gamepad.GetAxisNegPos(config.rotationAroundY)
                );

        }

        private void Control(Character chara, float dt)
        {

        }
        #endregion

    }
}