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
            public Smooth<float> rotation_around_x_stick;
            private float rotation_around_x_stick_tick(float current, float target, float dt)
            {
                const float time_to_reach_target = 0.75f;
                return time_to_reach_target > 0 ?
                    Mathf.MoveTowards(current, target, dt * (2f * (1f / time_to_reach_target))) :
                    target;
            }

            // Rotation Around Z
            public Smooth<float> rotation_around_z;
            private float rotation_around_z_tick(float current, float target, float dt)
            {
                const float time_to_reach_target = 0.75f;
                return time_to_reach_target > 0 ?
                    Mathf.MoveTowards(current, target, dt * (2f * (1f / time_to_reach_target))) :
                    target;
            }

            // Cursor Displacement
            public Smooth<Vector2> cursor_displacement;
            private Vector2 cursor_displacement_tick(Vector2 current, Vector2 target, float dt)
            {
                const float linear_speed = 0.0f;
                const float time_to_reach_target = 2f;
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

            public void Init()
            {
                // Cursor Displacement
                cursor_displacement = new Smooth<Vector2>(Vector2.zero, cursor_displacement_tick);

                // Rotation Around 
                rotation_around_x_stick = new Smooth<float>(0, rotation_around_x_stick_tick);
                // Rotation Around Y
                rotation_around_z = new Smooth<float>(0, rotation_around_z_tick);
                
                // X Translation
                x_translation_hit_bounds = new Smooth<float>(0, x_translation_hit_bounds_tick);
                // Y Translation
                y_translation_hit_bounds = new Smooth<float>(0, y_translation_hit_bounds_tick);
            }

            public void TickSmooth(float dt)
            {
                // Cursor Displacement
                cursor_displacement.tick(dt);

                // Rotation Around X
                y_translation_hit_bounds.tick(dt);
                rotation_around_x_stick.tick(dt);
                // Rotation Around Y
                x_translation_hit_bounds.tick(dt);
                // Rotation Around Z
                rotation_around_z.tick(dt);
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
            state.Init();
        }
        
        private void HandleInputs()
        {
            var gamepad = config.xbox_gamepad;

            // Cursor Displacement
            state.cursor_displacement.set_target(Vector2.Scale(gamepad.left_stick.value, new Vector2(1, 1)));

            // Rotation Around X (Stick)
            state.rotation_around_x_stick.set_target(gamepad.right_stick.value.y);

            // Rotation Around Y
            state.rotation_around_z.set_target(-gamepad.right_stick.value.x);

        }

        private void Control(Character chara, float dt)
        {

        }
        #endregion

    }
}