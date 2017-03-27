using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Avatar
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
            [Header("Forward Speed")]
            // Forward Speed    
            public float minThrustSpeed;
            public float maxThrustSpeed;
            public AnimationCurve forwardSpeedTransition;

            [Header("Rotation")]
            // Rotation
            // Anteroposterior Axis
            public float anteroposteriorAxisRotationSpeed;
            public float yAxisRotationSpeed
            {
                get
                {
                    return anteroposteriorAxisRotationSpeed;
                }
                set
                {
                    anteroposteriorAxisRotationSpeed = value;
                }
            }
            // Left-Right Axis
            public float leftRightAxisRotationSpeed;
            public float xAxisRotationSpeed
            {
                get
                {
                    return leftRightAxisRotationSpeed;
                }
                set
                {
                    leftRightAxisRotationSpeed = value;
                }
            }
            // Dorsoventral Axis
            public float dorsoventralAxisRotationSpeed;

            [Header("Correction")]
            public float rotationCorrectionStrengh = 2f;
            public float highCorrectionStrengh = 64f;
            public float highCorrectionDepletionRate = 0.5f;

            // Gravity
            [Header("Gravity")]
            public float minGravity = 0;
            public float maxGravity = 32f;
            public float timeToReachMinGravity = 0.25f;
            public float timeToReachMaxGravity = 8.00f;

            // Ascention / Decente Modifier
            [Header("Ascention / Decente Modifier")]
            public float ascentionSpeedModifier = 0.00f;
            public float decenteSpeedModifier = 2.00f;
            public float timeToReachAssentionModifier = 2f;
            public float timeToReachDecenteModifier = 2f;


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
            
            // Correction
            public float correctionRatioSmooth;
            public float highCorrectionRatioSmooth;

            // FX
            public float distToGround = 10;

            // Thrust
            public float accelerationDeceleration;

            // Rotation
            public Quaternion rotation;
            public Quaternion dorsoventralControlRotation;

            // Anteroposterior Axis
            public float anteroposteriorAxisRotationSpeed;

            // Left-Right Axis
            public float leftRightAxisRotationSpeed;

            // Dorsoventral Axis
            public float dorsoventralAxisRotationInterpolation;

            public float dorsoventralAxisAngle(float dorsoventralAxisRotationSpeed)
            {
                return dorsoventralAxisRotationInterpolation * -dorsoventralAxisRotationSpeed * Mathf.Deg2Rad;
            }
            public Quaternion dorsoventralAxisRotation(float dorsoventralAxisRotationSpeed)
            {
                float dorsoventralAxisAngle = this.dorsoventralAxisAngle(dorsoventralAxisRotationSpeed);
                dorsoventralAxisAngle = Mathf.Sin(Mathf.Clamp(dorsoventralAxisAngle, -Mathf.PI/2f, Mathf.PI/2f));
                dorsoventralAxisAngle *= 0.125f;
                return Quaternion.Euler(
                    0,
                    dorsoventralAxisAngle * Mathf.Rad2Deg,
                    dorsoventralAxisAngle * -90f
                );
            }

            // Bump
            public bool isStunned;

            // Gravity
            public Vector3 gravityDirection = Vector3.down;
            public float gravity_velocity;
            private float _gravity_velocity_damp;
            public Vector3 gravityVelocity { get { return gravityDirection * gravity_velocity; } }
            
            public void gravity_tick(
                // Thrust direction
                Vector3 current_forward_direction,
                Vector3 up,
                // Ascention / Decente Modifier
                float min_gravity,
                float max_gravity,
                float time_to_reach_min_gravity,
                float time_to_reach_max_gravity,
                // Time
                float dt
                )
            {
                float interpolation = Mathf.Max(0.0f, Vector3.Dot(current_forward_direction, up));
                interpolation = 1 - Mathf.Pow(Mathf.Abs(Mathf.Cos((Mathf.PI * interpolation) * 0.5f)),0.1f);
                float target_gravity = Mathf.Lerp(min_gravity, max_gravity, interpolation);
                //Debug.Log(target_gravity);
                float time_to_reach_target = Mathf.Lerp(time_to_reach_min_gravity, time_to_reach_max_gravity, interpolation);
                gravity_velocity = Mathf.SmoothDamp(gravity_velocity, target_gravity, ref _gravity_velocity_damp, time_to_reach_target, Mathf.Infinity, dt);
                //Debug.Log(gravity_velocity);
            }

            // Ascention / Decente Modifier
            public float asc_dec_modifier;
            private float _asc_dec_modifier_damp;

            public void asc_dec_modifier_tick(
                // Thrust direction
                Vector3 current_forward_direction,
                Vector3 up,
                // Ascention / 
                float asc_speed_modifier,
                float dec_speed_modifier,
                float time_to_reach_asc_speed_modifier,
                float time_to_reach_dec_speed_modifier,
                // Time
                float dt
                )
            {
                float interpolation = Vector3.Dot(current_forward_direction, up) * 0.5f + 0.5f;
                float target_asc_dec_modifier = Mathf.Lerp(dec_speed_modifier, asc_speed_modifier, interpolation);
                interpolation = Mathf.Abs(Vector3.Dot(current_forward_direction, up));
                target_asc_dec_modifier = Mathf.Lerp(1, target_asc_dec_modifier, interpolation);
                float time_to_reach_target = Mathf.Lerp(time_to_reach_dec_speed_modifier, time_to_reach_asc_speed_modifier, interpolation);
                asc_dec_modifier = Mathf.SmoothDamp(asc_dec_modifier, target_asc_dec_modifier, ref _asc_dec_modifier_damp, time_to_reach_target, Mathf.Infinity, dt);
            }
        }

        public State state = new State();

        public float GetForwardSpeed()
        {
            return Mathf.Lerp(config.minThrustSpeed, config.maxThrustSpeed, config.forwardSpeedTransition.Evaluate(state.accelerationDeceleration * 0.5f + 0.5f));
        }
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

        }

        private void WakeUp()
        {
            state.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        private void Behave(float dt)
        {
            float @float;
            float input = 0;
            Quaternion rotation;
            Ray ray;
            RaycastHit hit;
            state.translation = Vector3.zero;

            // Collision
            ray = new Ray(transform.position + transform.up, transform.forward);
            const float collisionDist = 3f;
            Debug.DrawRay(ray.origin, ray.direction * collisionDist * (GetForwardSpeed() / config.maxThrustSpeed));
            if (Physics.Raycast(ray, out hit, collisionDist * (GetForwardSpeed() / config.maxThrustSpeed)))
            {
                StunAvatar(hit.normal);
            }

            if (state.isStunned)
            {
                return;
            }

            // Ascention / Decente Modifier
            // Tick
            state.asc_dec_modifier_tick(
                transform.forward,
                Vector3.up,
                config.ascentionSpeedModifier,
                config.decenteSpeedModifier,
                config.timeToReachAssentionModifier,
                config.timeToReachDecenteModifier,
                dt
                );
            // Get
            @float = state.asc_dec_modifier;

            // Thrust
            input = GetForwardSpeed();
            input *= @float;

            state.translation += transform.forward * dt * input;

            // Gravity
            // Tick
            state.gravity_tick(
                transform.forward,
                Vector3.up,
                config.minGravity,
                config.maxGravity,
                config.timeToReachMinGravity,
                config.timeToReachMaxGravity,
                dt
                );
            // Get
            Vector3 gravityVelocity = state.gravityVelocity;
            state.translation += gravityVelocity * dt;

            
            // Anteroposterior Axis Rotation
            input /= 128;
            input += 1;
            input = 1;
            rotation = Quaternion.AngleAxis(input * state.anteroposteriorAxisRotationSpeed * dt, state.dorsoventralControlRotation * Vector3.forward);
            state.rotation *= rotation;

            // Left-Right Axis
            rotation = Quaternion.AngleAxis(state.leftRightAxisRotationSpeed * dt, state.dorsoventralControlRotation * Vector3.right);
            state.rotation *= rotation;

            // Dorsoventral additive Left-Right Axis Rotation
            input = state.dorsoventralAxisAngle(config.dorsoventralAxisRotationSpeed);
            rotation = Quaternion.AngleAxis(input * Mathf.Rad2Deg * dt, Vector3.up);
            state.rotation *= rotation;

            //// Dorsoventral Axis
            rotation = state.dorsoventralControlRotation = state.dorsoventralAxisRotation(config.dorsoventralAxisRotationSpeed);

            // Correction
            float correctionRatio = 0;
            Quaternion correctedRotation = state.rotation;

            // Orientation to ground Correction
            // Position to ground Correction
            ray = new Ray(transform.position, (transform.forward * 1.5f * GetForwardSpeed() * dt - transform.up).normalized);
            const float maxCorrectionDist = 32f;
            float distToGround = 1;
            if (Physics.Raycast(ray, out hit, maxCorrectionDist))
            {
                // Debug
                Debug.DrawLine(ray.origin, hit.point, Color.red);
                Debug.DrawRay(hit.point, hit.normal, Color.magenta);

                // Calculate correction ratio
                float dotCorrectionRatio = Vector3.Dot(hit.normal, state.rotation * Vector3.forward);
                if (dotCorrectionRatio >= 0)
                    dotCorrectionRatio = -1f;
                dotCorrectionRatio = 1 + dotCorrectionRatio;
                float distCorrectionRatio = 1 - Vector3.Distance(ray.origin, hit.point)/maxCorrectionDist;
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
                correctedRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane((state.rotation * Vector3.forward), hit.normal), hit.normal);

                state.highCorrectionRatioSmooth += correctionRatio * correctionRatio * dt;
            }

            // Smooth dist to ground
            float deltaDistToGround = Mathf.Abs(state.distToGround - distToGround);
            state.distToGround = Mathf.MoveTowards(state.distToGround, distToGround, (2f * deltaDistToGround + 0.1f) * dt);

            // Smooth correction Ratio
            float deltaCorrectionRatio = Mathf.Abs(correctionRatio - state.correctionRatioSmooth);
            state.correctionRatioSmooth = Mathf.MoveTowards(state.correctionRatioSmooth, correctionRatio, 4f * deltaCorrectionRatio * dt);

            // Correct high
            Vector3 highCorrection = transform.up * config.highCorrectionStrengh * state.highCorrectionRatioSmooth * (GetForwardSpeed() / config.maxThrustSpeed);
            state.highCorrectionRatioSmooth = Mathf.MoveTowards(state.highCorrectionRatioSmooth, 0, state.highCorrectionRatioSmooth * config.highCorrectionDepletionRate * dt);

            // Correct rotation (ground)
            float deltaAngle = Quaternion.Angle(state.rotation, correctedRotation);
            state.rotation = Quaternion.RotateTowards(state.rotation, correctedRotation, config.rotationCorrectionStrengh * deltaAngle * state.correctionRatioSmooth * dt);

            // Correct rotation (fall)
            correctedRotation = Quaternion.LookRotation(state.translation.normalized, transform.up);
            deltaAngle = Quaternion.Angle(state.rotation, correctedRotation);
            state.rotation = Quaternion.RotateTowards(state.rotation, correctedRotation, deltaAngle * 0.5f * dt);

            state.translation += highCorrection * dt;

            transform.position = transform.position + state.translation;

            transform.rotation = state.rotation * rotation;

        }

        private void LateUpdate()
        {
            Quaternion correctedRotation = Quaternion.LookRotation(state.translation.normalized, transform.up);
            Debug.DrawRay(transform.position, correctedRotation * Vector3.forward, Color.red);
        }

        public void OnCollisionEnter(Collision collision)
        {
            //Vector3 collisionNormal = Vector3.zero;
            //foreach(var contact in collision.contacts)
            //{
            //    collisionNormal += contact.normal;
            //}
            //collisionNormal.Normalize();
            //StunAvatar(collisionNormal);
        }

        public void StunAvatar(Vector3 bumpNormal)
        {
            if (state.isStunned)
            {
                StopCoroutine("CollisionStun");
            }
            StartCoroutine(CollisionStun(Mathf.Lerp(0f,1f, (GetForwardSpeed()/config.maxThrustSpeed)), bumpNormal, state.rotation * Vector3.forward * GetForwardSpeed()));
        }

        IEnumerator CollisionStun(float duration, Vector3 bumpNormal, Vector3 velocity)
        {
            state.isStunned = true;
            float startTime = Time.fixedTime;
            float bumpStrengh = 0.25f;
            bumpStrengh *= velocity.magnitude;
            Vector3 bumpForce = bumpNormal + Vector3.ProjectOnPlane(velocity, bumpNormal).normalized;
            bumpForce *= bumpStrengh;

            // Prevent pin-ball effect
            const float pinball_effect_reduction = 0.125f;
            Ray ray = new Ray(transform.position, bumpForce);
            float bumpMagnitude = bumpForce.magnitude;
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit, bumpMagnitude))
            {
                bumpMagnitude = Vector3.Distance(ray.origin, hit.point);
                bumpMagnitude *= pinball_effect_reduction;
                bumpForce = bumpForce.normalized * bumpMagnitude;
            }
            
            Quaternion randomRotation = UnityEngine.Random.rotation;
            while (Time.fixedTime <= startTime + duration)
            {
                yield return new WaitForFixedUpdate();

                float progress = (Time.fixedTime - startTime) / duration;
                float bumpTransition = progress;
                bumpTransition = Mathf.Sin((bumpTransition * Mathf.PI)/2);
                
                Vector3 bumpFoceNow = Vector3.Lerp(bumpForce, Vector3.zero, bumpTransition);
                transform.position = transform.position + bumpForce * Time.fixedDeltaTime;

                Quaternion rotate = Quaternion.Lerp(
                    Quaternion.RotateTowards(Quaternion.identity, randomRotation, (1 - progress) * 4f * 360f * Time.fixedDeltaTime) * Quaternion.Inverse(state.rotation),
                    Quaternion.LookRotation(bumpForce, transform.up) * Quaternion.Inverse(state.rotation),
                    progress
                    );
                state.rotation = state.rotation * rotate;
                transform.rotation = state.rotation;
                state.accelerationDeceleration = Mathf.MoveTowards(state.accelerationDeceleration, 0, progress);
            }
            state.isStunned = false;
        }

        #endregion

    }
}