//#define USE_BOUND_ROTATION
//#define USE_BOUND_TRANSLATION

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
            
            [Header("Gravity")]
            public Vector3 ascendingGravity;
            public Vector3 descendingGravity;
            public Vector3 diveGravity;
            
            [Header("Wings")]
            public WingsDeployment.Configuration wingsDeploymentConfig;
            public AirPush.Configuration airPushConfig;
            
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
            // Translation
            public Vector3 translation;

            // Rotation
            public Quaternion rotation;
            
            // Velocity
            public Vector3 stored_velocity;

            public float air_gauge = 0;
            public float air_gauge_depletion = 0;

            // Wings
            public WingsDeployment wings_deployment;
            public AirPush air_push;
            //public float wings_deployment;

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

            public void Init(
                float config_start_thrust,
                AirPush.Configuration air_push_cfg,
                WingsDeployment.Configuration wings_cfg
                )
            {
                // Thrust
                thrust = new Smooth<float>(config_start_thrust, thrust_tick);
                air_push = AirPush.Create(air_push_cfg);
                wings_deployment = WingsDeployment.Create(wings_cfg);
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
            state.Init(
                config.minThrustSpeed,
                config.airPushConfig,
                config.wingsDeploymentConfig
                );
        }

        private void WakeUp()
        {

        }

        private void Behave(float dt)
        {
            // Main Body
            MainBody_Behave(dt);
            
            // Cursor
            Cursor_Behave(dt);

            // Bird
            Bird_Behave(dt);

            #region Legacy
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
            #endregion
        }

        #region Wings
        #region WingsDeployment
        public class WingsDeployment
        {
            const string ability_name = "Wings Deployment";
            [System.Serializable]
            public class Configuration
            {
                public float openingSpeed = 1;
                public float closeingSpeed = 0;
            }
            public Configuration config;
            public class State
            {
                public float wings_deployment = 1;
            }
            public State state = new State();
            private WingsDeployment() { }
            public static WingsDeployment Create(Configuration config)
            {
                WingsDeployment o = new WingsDeployment();
                o.config = config;
                return o;
            }
            public void Tick(float target_wings_deployment, float dt)
            {
                if(target_wings_deployment > state.wings_deployment)
                {   // Opening
                    if (config.openingSpeed <= 0)
                    {
                        state.wings_deployment = 0;
                    }
                    else
                    {
                        state.wings_deployment = Mathf.MoveTowards(
                            state.wings_deployment,
                            target_wings_deployment,
                            config.openingSpeed * dt
                        );
                    }
                }
                else
                {   // Closing
                    if (config.closeingSpeed <= 0)
                    {
                        state.wings_deployment = 0;
                    }
                    else
                    {
                        state.wings_deployment = Mathf.MoveTowards(
                            state.wings_deployment,
                            target_wings_deployment,
                            config.closeingSpeed * dt
                        );
                    }
                }
            }


        }
        #endregion
        #region AirPush
        public class AirPush
        {
            public class AbilityIsOnCooldownExeption : Exception
            {
                public AbilityIsOnCooldownExeption()
                {
                }

                public AbilityIsOnCooldownExeption(string message)
                    : base(message)
                {
                }

                public AbilityIsOnCooldownExeption(string message, Exception inner)
                    : base(message, inner)
                {
                }
            }

            const string ability_name = "Air Push";
            [System.Serializable]
            public class Configuration {
                public float height;
                public float ability_duration;
                public float cooldown;
                public AnimationCurve translation_over_time;
                public Vector3 translation_vector;
            }
            public Configuration config;
            public class State
            {
                public float time_since_last_beginned_cast;
                public float time_since_last_set_on_cooldown;
                public float ability_progress;
                public Vector3 this_frame_translation;
            }
            public State state = new State();
            private AirPush() { }
            public static AirPush Create(Configuration config)
            {
                AirPush o = new AirPush();
                o.config = config;
                return o;
            }
            public static void Init()
            {

            }
            public void TryCast(float current_time)
            {
                Debug.Log(RemainingCooldown(current_time));
                if (IsOnCooldown(current_time))
                {
                    float remaining_cooldown = RemainingCooldown(current_time);
                    string err_msg = string.Format("{0} is on cooldown, {1:0.00}s remaining.", ability_name, remaining_cooldown);
                    AbilityIsOnCooldownExeption e = new AbilityIsOnCooldownExeption(err_msg);
                    throw e;
                }
                Cast(current_time);
            }
            public void Cast(float current_time)
            {
                Debug.Log("Cast");
                state.time_since_last_beginned_cast = current_time;
                SetOnCooldown(current_time);
                state.ability_progress = 0;
            }
            public void Tick(float current_time, float dt)
            {
                float time_since_beginned_cast = (current_time - state.time_since_last_beginned_cast);

                // Calculate this frame tranlsation
                float last_ability_progress = state.ability_progress;
                float new_ability_progress = time_since_beginned_cast / config.ability_duration;
                state.this_frame_translation = GetDeltaFrameTranslation(last_ability_progress, new_ability_progress);
                state.ability_progress = new_ability_progress;
            }
            public Vector3 GetDeltaFrameTranslation(float last_progress, float current_progress)
            {
                Vector3 translation_vector = config.translation_vector * config.height;
                Vector3 last_displacement = translation_vector * config.translation_over_time.Evaluate(last_progress);
                Vector3 current_displacement = translation_vector * config.translation_over_time.Evaluate(current_progress);
                Vector3 delta_displacement = current_displacement - last_displacement;
                return delta_displacement;
            }
            public Vector3 GetThisFrameTranslation()
            {
                return state.this_frame_translation;
            }
            // Cooldown
            public void SetOnCooldown(float current_time)
            {
                state.time_since_last_set_on_cooldown = current_time;
            }
            public bool IsOnCooldown(float current_time)
            {
                Debug.Log(string.Format("target : {0} | now : {1}", state.time_since_last_set_on_cooldown + config.cooldown, current_time));
                return current_time < state.time_since_last_set_on_cooldown + config.cooldown;
            }
            public float RemainingCooldown(float current_time)
            {
                return (state.time_since_last_set_on_cooldown + config.cooldown) - current_time;
            }
        }
        #endregion
        #endregion

        private void MainBody_Behave(float dt)
        {
            state.translation = Vector3.zero;
            MainBody_Wings(dt);
            MainBody_TranslationTick(dt);
            MainBody_RotationTick(dt);
            MainBody_ThrustTick(state.translation, dt);
            MainBody_ApplyTransform();
        }

        private void MainBody_Wings(float dt)
        {
            // Input
            var ctrl = config.controller;

            // Wings deployment
            // 0 = Closed
            // 1 = Deployed
            var wingsInput = ctrl.GetWingsInput();
            float wings_deployment = (1 - wingsInput.value);
            state.wings_deployment.Tick(wings_deployment, dt);
            wings_deployment = state.wings_deployment.state.wings_deployment;

            Vector3 original_velocity = state.stored_velocity;
            Vector3 regular_redirected_velocity = transform.forward * original_velocity.magnitude;
            Vector3 dive_redirected_velocity = Vector3.Project(original_velocity, Vector3.up) + Vector3.ProjectOnPlane(original_velocity, Vector3.up).magnitude * Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            //Vector3 redirected_velocity = Vector3.Slerp(state.stored_velocity, transform.forward * state.stored_velocity.magnitude, wings_deployment);
            Vector3 redirected_velocity = Vector3.Lerp(dive_redirected_velocity, regular_redirected_velocity, wings_deployment);

            // Gravity
            float ascending_descending_ratio = Vector3.Dot(Vector3.up, transform.forward) * 0.5f + 0.5f;
            ascending_descending_ratio *= wings_deployment;
            Vector3 gravity = Vector3.Lerp(config.descendingGravity, config.ascendingGravity, ascending_descending_ratio);
                    gravity = Vector3.Lerp(gravity, config.diveGravity, 1 - wings_deployment);
            state.stored_velocity += gravity * dt;
            
            // ARCHIMED
            float archimed_strengh = 0.75f;
            // Archimed Ratio
            // 0 = perpendicular to ground
            // 1 = parallel to ground
            float archimed_ratio = 1 - Mathf.Abs(Vector3.Dot(Vector3.up, transform.forward));
            archimed_ratio *= archimed_strengh;
            archimed_ratio *= wings_deployment;

            // Redirected toward facing direction
            float time_to_reach_target_redirected_velocity = 0.125f;
            float to_target_redirected_velocity = Vector3.Angle(state.stored_velocity, redirected_velocity);
            float angle_redirected_velocity = time_to_reach_target_redirected_velocity > 0 ? (to_target_redirected_velocity * dt) / time_to_reach_target_redirected_velocity : to_target_redirected_velocity;
            state.stored_velocity = Vector3.RotateTowards(state.stored_velocity, redirected_velocity, angle_redirected_velocity * Mathf.Deg2Rad, 0);

            // Archimed Acceleration 
            float down_force = Mathf.Min(state.stored_velocity.y, 0);
            float compensated_force = down_force * (1 - archimed_ratio);
            float time_to_reach_target = 0.25f;
            float to_target = Mathf.Abs(down_force - compensated_force);
            float archimed_acceleration = time_to_reach_target > 0 ? (to_target * dt) / time_to_reach_target : to_target;
            // Archimed Force
            Vector3 archimed_force = Vector3.up * archimed_acceleration;
            
            state.stored_velocity += archimed_force;
            var input = ctrl.GetWingsInput();
            bool triggerAirPush = input.last_value < input.value;
            if (triggerAirPush)
            {
                Debug.Log("TryCast");
                try
                {
                    state.air_push.TryCast(Time.fixedTime);
                }
                catch (AirPush.AbilityIsOnCooldownExeption e)
                {
                    //Debug.Log(e.Message);
                }
            }
            state.air_push.Tick(Time.fixedTime, dt);
            Vector3 air_push_translation = state.air_push.GetThisFrameTranslation();
            state.translation += air_push_translation;

            //const float gauge_scale = 0.50f;
            //const float gauge_depletion_rate = 1f;

            //float current_air_push_forward = state.air_gauge;
            //var input = ctrl.GetWingsInput();
            //float air_push_input = input.value - input.last_value;
            //air_push_input = Mathf.Max(0, air_push_input * gauge_scale);

            //float current_air_gauge = state.air_gauge;
            //float pushed_air_gauge = current_air_gauge + air_push_input;
            //float depleted_air_gauge = Mathf.MoveTowards(pushed_air_gauge, 0, dt * gauge_depletion_rate * pushed_air_gauge);
            //float new_air_gauge = depleted_air_gauge;
            //state.air_gauge = new_air_gauge;

            //// Push up
            //float depletion = Mathf.Abs(pushed_air_gauge - depleted_air_gauge);
            //state.air_gauge_depletion = depletion;
        }

        private void MainBody_TranslationTick(float dt)
        {
            // Input
            var ctrl = config.controller.state;

            /////////////////
            // Translation //
            /////////////////

            // Translation requirement
            Vector3     translation                     = Vector3.zero;

            // Thrust requirements requirement
            Vector3     current_character_forward       = transform.rotation * Vector3.forward;
            float       current_character_thrust_speed  = state.thrust.get_value();

            // Thrust
            Vector3 thrust_translation = current_character_forward* current_character_thrust_speed * dt;
            translation += thrust_translation;

            // Wings air push
            const float air_push_strengh = 1024f;
            Vector3 air_push_acceleration = (transform.rotation * new Vector3(0f, 1f, 0.25f).normalized) * state.air_gauge_depletion * air_push_strengh;
            //state.stored_velocity += air_push_acceleration * dt;
            translation += air_push_acceleration;
            
            // Stored velocity
            translation += state.stored_velocity * dt;

#if USE_BOUND_TRANSLATION
            // Bound hit translation
            float x_stick_magnitude = Mathf.Abs(ctrl.rotation_around_z.get_value());
            float y_stick_magnitude = Mathf.Abs(ctrl.rotation_around_x.get_value());
            Vector3 bounds_hit_translation =
                (1 - x_stick_magnitude) * ctrl.x_translation_hit_bounds.get_value() * config.translationSpeed.x * (transform.rotation * Vector3.right) +
                (1 - y_stick_magnitude) * ctrl.y_translation_hit_bounds.get_value() * config.translationSpeed.y * (transform.rotation * Vector3.up);
            translation += bounds_hit_translation * dt;
#endif

            // Store translation
            state.translation += translation;
        }

        private void MainBody_RotationTick(float dt)
        {
            // Input
            var ctrl = config.controller.state;

            // Rotation requirement
            Quaternion rotation = Quaternion.identity;
            
            //////////////////////
            // Regular Rotation //
            //////////////////////

            // Rotation X (Left-Right)
            float rot_around_x = ctrl.rotation_around_x.get_value();
            rot_around_x *= config.rotationAroundAxisSpeed.x;
            rotation *= Quaternion.AngleAxis(rot_around_x * dt, Vector3.right);

            // Rotation Z (Dorso-Ventral)
            float rot_around_y = ctrl.rotation_around_y.get_value();
            rot_around_y *= config.rotationAroundAxisSpeed.z;
            Quaternion world_rotation = Quaternion.AngleAxis(rot_around_y * dt, Vector3.up);
            rotation *= (Quaternion.Inverse(transform.rotation) * world_rotation) * transform.rotation;
            //rotation *= Quaternion.AngleAxis(rot_around_y * dt, Vector3.up);


            // Rotation Z (Antero-Posterior)
            //float rot_around_z = ctrl.rotation_around_z.get_value();
            //rot_around_z *= config.rotationAroundAxisSpeed.z;
            //rotation *= Quaternion.AngleAxis(rot_around_z * dt, Vector3.forward);

#if USE_BOUND_ROTATION
            ////////////////////
            // Bound Rotation //
            ////////////////////

            // Rotation X (Left-Right)
            float rot_around_x_bound = ctrl.y_translation_hit_bounds.get_value();
            rot_around_x_bound *= config.rotationAroundAxisSpeed.x;
            rotation *= Quaternion.AngleAxis(rot_around_x_bound * dt, -Vector3.right);

            // Rotation Z (Antero-Posterior)
            float rot_around_z_bound = ctrl.x_translation_hit_bounds.get_value();
            rot_around_z_bound *= config.rotationAroundAxisSpeed.z;
            rotation *= Quaternion.AngleAxis(rot_around_z_bound * dt, Vector3.up);

            #region Legacy
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
            #endregion
#endif
            // Store rotation
            state.rotation = rotation;
        }

        private void MainBody_ApplyTransform()
        {
            MainBody_Rotate(state.rotation);
            MainBody_Move(state.translation);
        }

        private void MainBody_Move(Vector3 translation)
        {
            Vector3 current_character_position      = transform.position;
            Vector3 new_character_position          = current_character_position + translation;
            
            //Vector3 current_bird_position           = config.bird.transform.position;
            //Vector3 local_current_bird_position     = (current_bird_position - current_character_position);
            //Vector3 new_bird_position               = new_character_position + translation;

            // Collision
            Ray ray = new Ray(current_character_position, translation);
            const float offset = 0.25f;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, translation.magnitude + offset))
            {
                //new_bird_position = Vector3.MoveTowards(hit.point, current_bird_position, offset + offset);
                //new_character_position = new_bird_position - local_current_bird_position;
                new_character_position = Vector3.MoveTowards(hit.point, current_character_position, offset);

                translation = Vector3.ProjectOnPlane(translation, hit.normal);

                ray.origin = new_character_position;
                ray.direction = translation.normalized;

                //new_character_position = new_character_position + translation;


                if (Physics.Raycast(ray, out hit, translation.magnitude + offset))
                {
                    // new_bird_position = Vector3.MoveTowards(hit.point, new_bird_position, offset);
                    // new_character_position = new_bird_position - local_current_bird_position;
                    new_character_position = Vector3.MoveTowards(hit.point, new_character_position, offset);
                }
                else
                {
                    //new_character_position = new_character_position + translation;
                    new_character_position = new_character_position + translation;
                }

            }

            // Apply position
            transform.position = new_character_position;
        }

        private void MainBody_Rotate(Quaternion rotation)
        {
            Quaternion current_character_rotation   = transform.rotation;
            Quaternion new_character_rotation       = current_character_rotation * rotation;

            //new_character_rotation *= Quaternion.Euler(Vector3.up * 20 * Time.deltaTime);

            // Apply position
            transform.rotation = new_character_rotation;
        }


        private void MainBody_ThrustTick(Vector3 translation, float dt)
        {
            //////////////////
            // Thrust Speed //
            //////////////////
            translation = translation.normalized;

            float up_down_modifier = Vector3.Dot(translation, -Vector3.up);
            //Debug.DrawRay(config.bird.position, translation, Color.red);
            up_down_modifier = up_down_modifier * 0.5f + 0.5f;
            up_down_modifier = Mathf.SmoothStep(0, 1, up_down_modifier);
            up_down_modifier = config.thrustTransition.Evaluate(up_down_modifier);

            float target_thrust = Mathf.Lerp(config.minThrustSpeed, config.maxThrustSpeed, up_down_modifier);

            state.thrust.set_target(target_thrust);
            state.thrust.tick(Mathf.Lerp(config.rechMinThrustTimeMultiplicator, config.rechMaxThrustTimeMultiplicator, up_down_modifier) * dt);
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