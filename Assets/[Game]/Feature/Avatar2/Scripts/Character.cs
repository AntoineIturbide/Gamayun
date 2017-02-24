#define USE_BOUND_ROTATION
#define USE_BOUND_TRANSLATION

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utility;

namespace Avatar2
{
    public class Character : MonoBehaviour
    {

        #region Configuration
        /*****************
         * CONFIGURATION *
         *****************/
        [Serializable]
        public class Configuration
        {
            [Header("Controller")]
            public Controller controller;

            [Header("UI")]
            public UnityEngine.Camera unityCamera;
            public RectTransform uiCanvas;
            [Header("Cursor")]
            public RectTransform outerBounds;
            public RectTransform innerBounds;
            public RectTransform cursor;

            [Header("Bird")]
            public Transform bird;

            [Header("Translation")]
            public Vector2 translationSpeed;

            [Header("Rotation")]
            // Rotation
            public Vector3 rotationAroundAxisSpeed;


            [Header("Thrust")]
            public float minThrustSpeed;
            public float maxThrustSpeed;
            public AnimationCurve thrustTransition;
            public float rechMinThrustTimeMultiplicator = 0.5f;
            public float rechMaxThrustTimeMultiplicator = 2.0f;
        }

        // Configuration Instance
        public Configuration config;
        #endregion

        #region State
        /*********
         * STATE *
         *********/
        public class State
        {
            // Rotation
            public Quaternion rotation;
            // Thrust
            public Smooth<float> thrust;
            private float thrust_tick(float current, float target, float dt)
            {
                const float linear_speed = 0.0f;
                const float time_to_reach_target = 4f;
                float dist_to_target = Mathf.Abs(current - target);
                return time_to_reach_target > 0 ?
                    Mathf.MoveTowards(current, target, dt * ((dist_to_target * 2f * (1f / time_to_reach_target)) + linear_speed)) :
                    target;
            }

            public void Init(float config_start_thrust)
            {
                // Thrust
                thrust = new Smooth<float>(config_start_thrust, thrust_tick);
            }
        }

        public State state = new State();
        #endregion

        #region Events
        /**********
         * EVENTS *
         **********/
        public class Events
        {

        }

        public Events events = new Events();
        #endregion

        #region Unity
        /*********
         * UNITY *
         *********/

        private void Awake()
        {
            Init();
        }
        
        private void Start()
        {
            WakeUp();
        }

        private void Update()
        {
        }

        private void FixedUpdate()
        {
            Behave(Time.fixedDeltaTime);
        }

        #endregion

        #region Behaviour
        /*************
         * BEHAVIOUR *
         *************/

        private void Init()
        {
            state.Init(config.minThrustSpeed);
        }

        private void WakeUp()
        {

        }

        private void Behave(float dt)
        {

            var ctrl = config.controller.state;


            float @float;
            Vector3 vector3;
            Quaternion quaternion;
            Ray ray;
            RaycastHit raycastHit;
            
            state.rotation = transform.rotation;

            //////////////
            // Rotation //
            //////////////

            // Rotation X (Left-Right)
            @float = ctrl.rotation_around_x_stick.get_value();
            //float stick_magnitude = Mathf.Abs(@float);
            //@float = Mathf.Lerp(ctrl.y_translation_hit_bounds.get_value(), ctrl.rotation_around_x_stick.get_value(), stick_magnitude);
            quaternion = Quaternion.AngleAxis(@float * config.rotationAroundAxisSpeed.x * dt, Vector3.right);
            state.rotation *= quaternion;


            // Rotation Z (Antero-Posterior)
            @float = ctrl.rotation_around_z.get_value();
            quaternion = Quaternion.AngleAxis(@float * config.rotationAroundAxisSpeed.z * dt, Vector3.forward);
            state.rotation *= quaternion;

            transform.rotation = state.rotation;

            /////////////////
            // Translation //
            /////////////////

            // Translation requirement
            Vector3 current_character_position      = transform.position;
            Vector3     translation                     = Vector3.zero;

            // Thrust requirements requirement
            Vector3     current_character_forward       = transform.rotation * Vector3.forward;
            float       current_character_thrust_speed  = state.thrust.get_value();

            // Thrust
            Vector3 thrust_translation = current_character_forward* current_character_thrust_speed;
            translation += thrust_translation * dt;
#if USE_BOUND_TRANSLATION
            // Bound hit translation
            float x_stick_magnitude = Mathf.Abs(ctrl.rotation_around_z.get_value());
            float y_stick_magnitude = Mathf.Abs(ctrl.rotation_around_x_stick.get_value());
            Vector3 bounds_hit_translation =
                (1 - x_stick_magnitude) * ctrl.x_translation_hit_bounds.get_value() * config.translationSpeed.x * (transform.rotation * Vector3.right) +
                (1 - y_stick_magnitude) * ctrl.y_translation_hit_bounds.get_value() * config.translationSpeed.y * (transform.rotation * Vector3.up);
            translation += bounds_hit_translation * dt;
#endif
            // Apply translation
            Vector3 new_character_position = current_character_position + translation;
            transform.position = new_character_position;


            //////////////////
            // Thrust Speed //
            //////////////////
            translation = translation.normalized;
            float up_down_modifier = Vector3.Dot(translation, -Vector3.up);
            Debug.DrawRay(config.bird.position, translation, Color.red);
            //up_down_modifier = Mathf.Sign(up_down_modifier);
            up_down_modifier = up_down_modifier * 0.5f + 0.5f;
            up_down_modifier = Mathf.SmoothStep(0, 1, up_down_modifier);
            up_down_modifier = config.thrustTransition.Evaluate(up_down_modifier);
            float target_thrust = Mathf.Lerp(config.minThrustSpeed, config.maxThrustSpeed, up_down_modifier);
            state.thrust.set_target(target_thrust);
            state.thrust.tick(Mathf.Lerp(config.rechMinThrustTimeMultiplicator, config.rechMaxThrustTimeMultiplicator, up_down_modifier) * dt);
            Debug.Log(state.thrust.get_value());

#if USE_BOUND_ROTATION
            ////////////////////
            // Bound Rotation //
            ////////////////////

            // Translation requirement
            Quaternion current_character_rotation = transform.rotation;
            Quaternion rotation = Quaternion.identity;

            @float = ctrl.y_translation_hit_bounds.get_value();
            rotation *= Quaternion.AngleAxis(@float * config.rotationAroundAxisSpeed.x * dt, -Vector3.right);

            @float = ctrl.x_translation_hit_bounds.get_value();
            rotation *= Quaternion.AngleAxis(@float * config.rotationAroundAxisSpeed.z * dt, Vector3.up);
            //rotation *= Quaternion.Euler(
            //    config.rotationAroundAxisSpeed.x * ctrl.x_translation_hit_bounds.get_value() * dt,
            //    config.rotationAroundAxisSpeed.y * ctrl.y_translation_hit_bounds.get_value() * dt, 
            //    0
            //    );

            Quaternion new_character_rotation = current_character_rotation * rotation;
            transform.rotation = new_character_rotation;

            //// Thrust requirements requirement
            //Vector3 current_character_forward = transform.rotation * Vector3.forward;
            //float current_character_thrust_speed = 128f;

            //// Thrust
            //Vector3 thrust_translation = current_character_forward * current_character_thrust_speed;
            //translation += thrust_translation * dt;
            //// Bound hit translation
            //float x_stick_magnitude = Mathf.Abs(ctrl.rotation_around_z.get_value());
            //float y_stick_magnitude = Mathf.Abs(ctrl.rotation_around_x_stick.get_value());
            //Vector3 bounds_hit_translation =
            //    (1 - x_stick_magnitude) * ctrl.x_translation_hit_bounds.get_value() * config.translationSpeed.x * (transform.rotation * Vector3.right) +
            //    (1 - y_stick_magnitude) * ctrl.y_translation_hit_bounds.get_value() * config.translationSpeed.y * (transform.rotation * Vector3.up);
            //translation += bounds_hit_translation * dt;
            //// Apply translation
            //Vector3 new_character_position = current_character_position + translation;
            //transform.position = new_character_position;
#endif

            ///////////////////
            // Bird & Cursor //
            ///////////////////

            Cursor_Behave(dt);
            Bird_Behave(dt);

            // Move toward bird
            //current_character_position = transform.position;
            //Vector3 current_bird_position = config.bird.position;
            //Vector3 target_character_position = current_bird_position;
            //// Smoothing
            //const float time_to_reach_target = 8f;
            //float dist_to_target = Vector3.Distance(current_character_position, current_bird_position);
            //new_character_position =
            //    time_to_reach_target > 0 ?
            //    Vector3.MoveTowards(current_character_position, target_character_position, dist_to_target * dt * (1f / time_to_reach_target)) :
            //    target_character_position;

            //transform.position = new_character_position;

        }

        private void Bird_Behave(float dt)
        {
            var ctrl = config.controller.state;

            UnityEngine.Camera unit_camera = config.unityCamera;

            Vector3     current_character_position  = transform.position;
            Vector3     current_camera_position     = unit_camera.transform.position;
            Vector3     from_character_to_camera    = current_camera_position - current_character_position;

            // Allign Bird With Cursor
            RectTransform ui = config.uiCanvas;
            Vector3 cursor_screen_point = config.cursor.position;


            const float max_dist_from_camera = 64f;
            Vector3 cursor_world_point = unit_camera.ScreenToWorldPoint(cursor_screen_point + Vector3.forward * max_dist_from_camera);
            Vector3 from_camera_to_cursor_world_point = cursor_world_point - current_camera_position;

            //Debug.DrawRay(current_camera_position, from_camera_to_cursor_world_point, Color.red);
            //Debug.DrawRay(current_character_position, from_character_to_camera.normalized, Color.blue);

            Vector3 intersection;
            if (!Math3d.LinePlaneIntersection(
                out intersection,
                current_camera_position,
                from_camera_to_cursor_world_point.normalized,
                from_character_to_camera.normalized,
                current_character_position)
                )
                return;

            Vector3 target_bird_position = intersection;
            // @improvement : Smoothing
            Vector3 new_bird_position = target_bird_position;

            config.bird.transform.position = new_bird_position;

        }

        private void Cursor_Behave(float dt)
        {
            var ctrl = config.controller.state;

            float @float;

            // Camera
            UnityEngine.Camera unit_camera = config.unityCamera;

            // Bird
            Vector3 current_bird_world_position = config.bird.transform.position;

            // Rect
            RectTransform outer_rectTransform = config.outerBounds;
            RectTransform inner_rectTransform = config.innerBounds;
            RectTransform cursor_rectTranform = config.cursor;
            
            // Rect
            Rect outer_bounds = outer_rectTransform.rect;
            Rect inner_bounds = inner_rectTransform.rect;

            float aspect_ratio = outer_bounds.width / outer_bounds.height;


            //// Reposition Cursor
            //Vector2 current_cursor_position = cursor_rectTranform.position;
            //Vector3 current_bird_screen_position = unit_camera.WorldToScreenPoint(current_bird_world_position);
            //Vector2 target_cursor_position = new Vector2(current_bird_screen_position.x, current_bird_screen_position.y);
            //// @improvement : Smoothing ?
            //Vector2 new_cursor_position = target_cursor_position;
            //cursor_rectTranform.position = new_cursor_position;


            // Position
            //const float pixel_unit = 1f;
            //Vector2 currentPosition = cursor_rectTranform.anchoredPosition;
            //currentPosition = Rect.PointToNormalized(outer_bounds, currentPosition);

            //Vector2 translation = ctrl.cursor_displacement.get_value();

            //if(Mathf.Sign(translation.x) == Mathf.Sign(ctrl.x_translation_hit_bounds.get_target()))
            //{
            //    translation.x = (1 - Mathf.Abs(ctrl.x_translation_hit_bounds.get_target())) * translation.x;
            //}
            //if (Mathf.Sign(translation.y) == -Mathf.Sign(ctrl.rotation_around_x_bound.get_target()))
            //{
            //    translation.y = (1 - Mathf.Abs(ctrl.rotation_around_x_bound.get_target())) * translation.y;
            //}

            //translation *= pixel_unit;
            //translation = Vector2.Scale(translation, new Vector2(1, aspect_ratio));

            //currentPosition += translation * dt;
            //translation = Vector2.Scale(translation, new Vector2(0.5f, 0.5f)) + new Vector2(0.5f, 0.5f);
            //currentPosition = translation;


            Vector2 current_cursor_position = cursor_rectTranform.anchoredPosition;
            Vector2 target_cursor_position = ctrl.cursor_displacement.get_value();
            target_cursor_position = Vector2.Scale(target_cursor_position, new Vector2(0.5f, 0.5f)) + new Vector2(0.5f, 0.5f);
            target_cursor_position = new Vector2(
                Mathf.Lerp(outer_bounds.xMin, outer_bounds.xMax, target_cursor_position.x),
                Mathf.Lerp(outer_bounds.yMin, outer_bounds.yMax, target_cursor_position.y)
                );
            Vector2 new_cursor_position = target_cursor_position;
            cursor_rectTranform.anchoredPosition = new_cursor_position;


            //currentPosition = Rect.NormalizedToPoint(outer_bounds, currentPosition);
            //cursor_rectTranform.anchoredPosition = currentPosition;

            // X Translation
            //@float =
            //    // Negatives
            //    -Mathf.Clamp01(Mathf.InverseLerp(inner_bounds.xMin, outer_bounds.xMin, currentPosition.x)) +
            //    // Positives
            //    Mathf.Clamp01(Mathf.InverseLerp(inner_bounds.xMax, outer_bounds.xMax, currentPosition.x))
            //    ;
            //@float = @float * @float * @float;
            //ctrl.x_translation_hit_bounds.set_target(@float);
            @float = ctrl.cursor_displacement.get_value().x;
            @float = @float * @float * @float;
            ctrl.x_translation_hit_bounds.set_target(@float);

            // Y Translation
            //@float =
            //     Negatives
            //    -Mathf.Clamp01(Mathf.InverseLerp(inner_bounds.yMin, outer_bounds.yMin, currentPosition.y)) +
            //     Positives
            //    Mathf.Clamp01(Mathf.InverseLerp(inner_bounds.yMax, outer_bounds.yMax, currentPosition.y))
            //    ;
            //@float = @float * @float * @float;
            //ctrl.y_translation_hit_bounds.set_target(@float);
            @float = ctrl.cursor_displacement.get_value().y;
            @float = @float * @float * @float;
            ctrl.y_translation_hit_bounds.set_target(@float);

        }
#endregion

    }
}