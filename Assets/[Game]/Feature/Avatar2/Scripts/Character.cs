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

            [Header("Translation")]
            public Vector2 translationSpeed;

            [Header("Rotation")]
            // Rotation
            public Vector3 rotationAroundAxisSpeed;
            
            [Header("Gravity")]
            public Vector3 ascendingGravity;
            public Vector3 descendingGravity;
            public Vector3 diveGravity;
            public Vector3 stallGravity;

            [Header("Speed")]
            public float ascendingTargetSpeed   = 0;
            public float glidingTargetSpeed     = 128;
            public float descendingTargetSpeed  = 256;
            public float divingTargetSpeed      = 512;

            //public float speedSmoothing = 0.25f;
            public float speedGainFactor = 0.25f;
            public float speedLossFactor = 0.25f;

            [Header("Dive")]
            public float diveLossOfControll = 1f;
            public float diveRotationFactor = 2f;

            public float get_min_target_speed()
            {
                return ascendingTargetSpeed;
            }
            public float get_max_target_speed()
            {
                return divingTargetSpeed;
            }

            [Header("Wings")]
            public WingsDeployment.Configuration wingsDeploymentConfig;
            public AirPush.Configuration airPushConfig;

            [Header("Ground Hit Prevention")]
            public float prenvetionDistance = 48f;
            public float prenvetionTranslationStrenght = 8f;
            public float prenvetionRotationStrenght = 1f;
            [Range(0,1)]
            public float prenvetionRotationMaxStrenght = 1f;

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


            // Stall fake gravity
            public Tweening.Tween<Vector3> fake_stall_gravity =
                new Tweening.Tween<Vector3>(
                    Vector3.zero,
                    UnityTick.FIXED_UPDATE,
                    Easing.DynaEase.Out,
                    1/0.25f
                    );

            // Ground hit prevention
            public Tweening.Tween<float> ground_hit_prevention =
                new Tweening.Tween<float>(
                    0,
                    UnityTick.FIXED_UPDATE,
                    Easing.DynaEase.Out,
                    1/2f
                    );

            // Wings
            public WingsDeployment wings_deployment;
            public AirPush air_push;

            public Tweening.Tween<float> speed =
                new Tweening.Tween<float>(0, UnityTick.FIXED_UPDATE, Easing.DynaEase.Out);
            
            public void Init(
                AirPush.Configuration air_push_cfg,
                WingsDeployment.Configuration wings_cfg
                )
            {
                // Air push setup
                air_push = AirPush.Create(air_push_cfg);

                // Wings deplyment setup
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
                config.airPushConfig,
                config.wingsDeploymentConfig
                );
        }
        
        private void Behave(float dt)
        {
            
            // Main Body
            MainBody_Behave(dt);
            
            // Cursor
            //Cursor_Behave(dt);

            // Bird
            //Bird_Behave(dt);

        }

        #region Wings

        // Deployment
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

        // Air push
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
                if(new_ability_progress > 1f)
                {
                    state.this_frame_translation = Vector3.zero;
                    return;
                }
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
                //Debug.Log(string.Format("target : {0} | now : {1}", state.time_since_last_set_on_cooldown + config.cooldown, current_time));
                return current_time < state.time_since_last_set_on_cooldown + config.cooldown;
            }
            public float RemainingCooldown(float current_time)
            {
                return (state.time_since_last_set_on_cooldown + config.cooldown) - current_time;
            }
        }
        #endregion

        // Slow
        Tweening.Tween<float> slow_factor =
            new Tweening.Tween<float>(0, UnityTick.UPDATE, Easing.DynaEase.Out);

        #endregion

        private void MainBody_Behave(float dt)
        {
            // Reset translation applyed at the end of each frames
            state.translation = Vector3.zero;
            state.rotation = Quaternion.identity;

            MainBody_Wings(dt);

            AirPushTick(dt);

            GroundHitPreventionTick(dt);

            MainBody_SpeedCorrection();
            MainBody_TranslationTick(dt);

            MainBody_RotationTick(dt);
            MainBody_RotationCorrectionTick(dt);

            MainBody_ApplyTransform();
        }

        private void MainBody_Wings(float dt)
        {
            // Input
            var ctrl = config.controller;

            // Wings deployment
            // 0 = Closed
            // 1 = Deployed
            var wings_input = ctrl.GetWingsInput();                             // Left trigger input
            float wings_deployment = (1 - wings_input.value);                   // Convert to target wings deployment
            state.wings_deployment.Tick(wings_deployment, dt);                  // Refresh wings deplyment
            wings_deployment = state.wings_deployment.state.wings_deployment;   // Retrieve refreshed wings deployment

            // Fake gravity
            float ascending_descending_ratio = Vector3.Dot(Vector3.up, transform.forward) * 0.5f + 0.5f;
            Vector3 fake_gravity = Vector3.Lerp(config.descendingGravity, config.ascendingGravity, ascending_descending_ratio);
            fake_gravity = Vector3.Lerp(fake_gravity, config.diveGravity, 1 - wings_deployment);

            // Stall gravity
            float dot = Vector3.Dot(transform.forward, Vector3.up);
            dot = Mathf.Clamp01(dot);
            float stall_factor = state.speed.get_value() / config.glidingTargetSpeed;
            stall_factor = 1 - stall_factor;
            stall_factor *= dot * dot;
            state.fake_stall_gravity.SetTarget(config.stallGravity * stall_factor);

            // Slow
            slow_factor.SetTarget(ctrl.state.slow_input);

            // Calculate velocity
            Vector3 velocity = transform.forward * state.speed.get_value();
            velocity += fake_gravity;
            velocity += state.fake_stall_gravity.get_value();

            // Apply velocity
            state.translation += velocity * dt;
        }

        protected void AirPushTick(float dt)
        {
            // Input
            var ctrl = config.controller;
            var input = ctrl.GetWingsInput();

            bool triggerAirPush = input.last_value < input.value;
            if (triggerAirPush)
            {
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
            air_push_translation = transform.rotation * air_push_translation;
            state.translation += air_push_translation;
        }

        protected void GroundHitPreventionTick(float dt)
        {
            // Position to ground Correction

            float correction_ratio = 0;

            Ray ray = new Ray(
                transform.position,
                (
                    transform.forward * 1.5f * state.speed.get_value() * dt +                    
                    -transform.up * ( 1 + (state.speed.get_value() / config.get_max_target_speed())) * config.prenvetionDistance * dt
                    ).normalized
                );
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, config.prenvetionDistance))
            {
                // Debug
                Debug.DrawLine(ray.origin, hit.point, Color.red);
                Debug.DrawRay(hit.point, hit.normal * 10f, Color.magenta);

                float dist_to_hit = Vector3.Distance(ray.origin, hit.point);
                float dist_to_hit_ratio = dist_to_hit / config.prenvetionDistance;
                correction_ratio = 1 - dist_to_hit_ratio;
            }
            state.ground_hit_prevention.SetTarget(correction_ratio * correction_ratio);
            
            state.translation += Vector3.up * state.speed.get_value() * config.prenvetionTranslationStrenght * state.ground_hit_prevention.get_value() * dt;

        }

#if false
        private void MainBody_Wings_OLD(float dt)
        {
            // Input
            var ctrl = config.controller;

            // Wings deployment
            // 0 = Closed
            // 1 = Deployed
            var wings_input = ctrl.GetWingsInput();                             // Left trigger input
            float wings_deployment = (1 - wings_input.value);                   // Convert to target wings deployment
            state.wings_deployment.Tick(wings_deployment, dt);                  // Refresh wings deplyment
            wings_deployment = state.wings_deployment.state.wings_deployment;   // Retrieve refreshed wings deployment

            Vector3 original_velocity = state.stored_velocity;
            Vector3 regular_redirected_velocity = transform.forward * original_velocity.magnitude;
            Vector3 dive_redirected_velocity = Vector3.Project(original_velocity, Vector3.up) + Vector3.ProjectOnPlane(original_velocity, Vector3.up).magnitude * Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
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
            float time_to_reach_target = 0.125f;
            float to_target = Mathf.Abs(down_force - compensated_force);
            float archimed_acceleration = time_to_reach_target > 0 ? (to_target * dt) / time_to_reach_target : to_target;
            // Archimed Force
            Vector3 archimed_force = Vector3.up * archimed_acceleration;
            // Apply archimed
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
            air_push_translation = transform.rotation * air_push_translation;
            state.translation += air_push_translation;
        }
#endif

        private void MainBody_SpeedCorrection()
        {
            // Set smoothed speed's target
            float target_speed = GetTargetSpeed();
            
            // Set target speed
            state.speed.SetTarget(target_speed);

            // Set time factor depending on gain or loss of speed
            bool is_gain = target_speed > state.speed.current_value;
            float time_factor;
            if (is_gain)
            {
                time_factor = 1f / config.speedGainFactor;
            }
            else
            {
                time_factor = 1f / config.speedLossFactor;
            }
            const float slow_factor_strenght = 4f;
            
            // Set time factor depending on slow factor
            float slow_factor = 1 + (1 - this.slow_factor.get_value()) * slow_factor_strenght;
            time_factor *= slow_factor;

            // Apply time factor to tween
            state.speed.time_factor = time_factor;
        }

        private void MainBody_RotationCorrectionTick(float dt)
        {
            DiveRotationCorrectionTick(dt);
            //GroundHitPreventionCorrectionTick(dt);
        }

        /// <summary>
        /// Rotate avatar toward the ground if diving.
        /// </summary>
        /// <param name="dt"></param>
        private void DiveRotationCorrectionTick(float dt)
        {
            float diving = 1 - state.wings_deployment.state.wings_deployment;

            float dot = Vector3.Dot(transform.forward, Vector3.up);
            float x_axis_rotation = dot * 0.5f + 0.5f;
            x_axis_rotation *= 180;
            x_axis_rotation *= config.diveRotationFactor;
            x_axis_rotation *= diving * diving;

            state.rotation *= Quaternion.AngleAxis(x_axis_rotation * dt, Vector3.right);
        }

        /// <summary>
        /// Rotate avatar to prevent him from hitting the ground
        /// </summary>
        /// <param name="dt"></param>
        private void GroundHitPreventionCorrectionTick(float dt)
        {

            // Orientation to ground Correction
            float correctionRatio = 0;
            Quaternion correctedRotation = transform.rotation;
            // Position to ground Correction
            Ray ray = new Ray(transform.position, (transform.forward * 1.5f * state.speed.get_value() * dt - transform.up).normalized);
            RaycastHit hit;
            const float maxCorrectionDist = 64f;
            float distToGround = 1;
            if (Physics.Raycast(ray, out hit, maxCorrectionDist))
            {
                // Debug
                Debug.DrawLine(ray.origin, hit.point, Color.red);
                Debug.DrawRay(hit.point, hit.normal * 10f, Color.magenta);
                                  
                // Calculate correction ratio                                         
                float dotCorrectionRatio = Vector3.Dot(hit.normal, state.rotation * Vector3.forward);
                if (dotCorrectionRatio >= 0)
                    dotCorrectionRatio = -1f;
                dotCorrectionRatio = 1 + dotCorrectionRatio;
                float distCorrectionRatio = 1 - Vector3.Distance(ray.origin, hit.point) / maxCorrectionDist;
                distToGround = 1 - distCorrectionRatio;
                if (
                    Vector3.Dot(hit.normal, state.rotation * Vector3.forward) < 0.1f &&
                    Vector3.Distance(ray.origin, hit.point) < 2f
                    )
                {
                    //StunAvatar(hit.normal);
                }                               
                correctionRatio = dotCorrectionRatio * distCorrectionRatio;

                // Correct rotation
                correctedRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane((state.rotation * Vector3.forward), Vector3.up), hit.normal);

                //state.highCorrectionRatioSmooth += correctionRatio * correctionRatio * dt;
                float delta_angle = Quaternion.Angle(transform.rotation, correctedRotation);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, correctedRotation, delta_angle * dt);
            }

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
                                    
            // Apply stored velocity each frame
            translation += state.stored_velocity * dt;

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
            // Player input
            float rot_around_x = ctrl.rotation_around_x.get_value();
            // Dive
            float diving = 1 - state.wings_deployment.state.wings_deployment;
            rot_around_x *= (1 - diving) * config.diveLossOfControll + (1 - config.diveLossOfControll);
            // Ground hit prevention
            rot_around_x = Mathf.Lerp(
                rot_around_x,
                -config.prenvetionRotationMaxStrenght,
                state.ground_hit_prevention.get_value() * config.prenvetionRotationStrenght
                );

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

            // Store rotation to be applyed later
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

            bool hard_collision = false;
            bool small_collision = false;

            //Vector3 current_bird_position           = config.bird.transform.position;
            //Vector3 local_current_bird_position     = (current_bird_position - current_character_position);
            //Vector3 new_bird_position               = new_character_position + translation;

            // Collision
            Ray ray = new Ray(current_character_position, translation);
            const float offset = 0.25f;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, translation.magnitude + offset))
            {
                small_collision = true;

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

                    hard_collision = true;
                }
                else
                {
                    //new_character_position = new_character_position + translation;
                    new_character_position = new_character_position + translation;
                }

            }

            if (hard_collision)
            {
                state.speed.current_value = 0;
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
        
        public float GetTargetSpeed()
        {
            float dot = Vector3.Dot(Vector3.up, transform.forward);

            float ascending_to_gliding_interpolation =
                Mathf.Clamp01((1 - dot));
            
            float gliding_to_descending_interpolation =
                Mathf.Clamp01(-dot);
            
            float to_diving_interpolation =
               1 - state.wings_deployment.state.wings_deployment;

            float ascending_to_gliding = config.glidingTargetSpeed - config.ascendingTargetSpeed;
            
            float gliding_to_descending = config.descendingTargetSpeed - config.glidingTargetSpeed;

            float ascending_to_descending =
                config.ascendingTargetSpeed +
                ascending_to_gliding * ascending_to_gliding_interpolation +
                gliding_to_descending * gliding_to_descending_interpolation;

            float target_speed =
                Mathf.Lerp(ascending_to_descending, config.divingTargetSpeed, to_diving_interpolation);

            target_speed *= slow_factor.get_value();

            return target_speed;
        }

#endregion

    }
}