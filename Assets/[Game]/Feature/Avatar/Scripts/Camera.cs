using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Avatar
{
    public class Camera : MonoBehaviour
    {
        #region Configuration
        /*****************
         * CONFIGURATION *
         *****************/
        [System.Serializable]
        public class Configuration
        {
            [Header("Targeted Character")]
            // Character
            public Character character;

            [Header("Unity Camera")]
            // Character
            public UnityEngine.Camera unityCamera;

            [Header("Folow Distance")]
            public float minFolowDistance = 18f;
            public float maxFolowDistance = 32f;

            [Header("Fov Distance")]
            public float minFov = 50f;
            public float maxFov = 75f;

            [Header("Fov Distance")]
            public bool rotateWithAvatar;

            [Header("Targeting correction")]
            public bool tergetingCorrectionActivated;

            [Header("Position Offset")]
            public Vector3 targetOffset;
            public Vector3 cameraOffset;
            public float minSpeedPositionOffsetSmoothing = 16f;
            public float maxSpeedPositionOffsetSmoothing = 16f;

            [Header("Rotation Offset")]
            public Vector2 minSpeedRotationOffset = new Vector2(2f,4f);
            public Vector2 maxSpeedRotationOffset = new Vector2(24f, 32f);
            public float minSpeedRotationOffsetSmoothing = 4f;
            public float maxSpeedRotationOffsetSmoothing = 6f;
            [Header("Rotation Offset 2")]
            public float minSpeedRotationOffsetSmoothing2 = 4f;
            public float maxSpeedRotationOffsetSmoothing2 = 6f;
            public float speedRotationOffsetLinearSmoothing2 = 2.5f;
        }

        // Configuration Instance
        public Configuration config;
        #endregion


        #region State
        /*********
         * STATE *
         *********/
		[System.Serializable]
        public class State
        {
            public Vector2 targetOffset;
        }

        public State state = new State();
        #endregion

        public float GetFolowRatio()
        {
            float ratio;
            // Thrust
            ratio = config.character.state.accelerationDeceleration;
            ratio = ratio * 0.5f + 0.5f;
            ratio = config.character.config.forwardSpeedTransition.Evaluate(ratio);
            // Dist to ground
            ratio *= config.character.state.distToGround;
            return ratio;
        }

        public float GetFolowDistance()
        {
            float ratio = GetFolowRatio();
            float folowDistance = Mathf.Lerp(config.minFolowDistance, config.maxFolowDistance, ratio);
            return folowDistance;
        }

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

        private void OnPreCull()
        {
            float fov = GetFolowRatio();
            fov = Mathf.Lerp(config.minFov, config.maxFov, fov);
            config.unityCamera.fieldOfView = fov;
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
            
        }

        private void Behave(float dt)
        {
            Character chara = config.character;

            // Move currentPosition toward target position

            // Position
            Vector3 targetPosition =
                chara.transform.position +
                (-(Vector3.Slerp(chara.transform.forward, chara.state.translation.normalized, 0f)) * GetFolowDistance()) +
                chara.transform.rotation * config.cameraOffset;

            if (chara.state.isStunned)
            {
                targetPosition = transform.position;
            }

            Vector3 currentPosition = transform.position;
            float dist = Vector3.Distance(targetPosition, currentPosition);
            currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, dist * Mathf.Lerp(config.minSpeedPositionOffsetSmoothing, config.maxSpeedPositionOffsetSmoothing, GetFolowRatio()) * dt);
            transform.position = currentPosition;

            // Smooth the rotation offset

            // Rotation
            Vector2 targetOffset = Vector2.zero;
            targetOffset.x =
                Mathf.Lerp(
                    config.minSpeedRotationOffset.x,
                    config.maxSpeedRotationOffset.x,
                    GetFolowRatio()
                    )
                *
                -chara.state.dorsoventralAxisRotationInterpolation
                ;
            targetOffset.y =
                Mathf.Lerp(
                    config.minSpeedRotationOffset.y,
                    config.maxSpeedRotationOffset.y,
                    GetFolowRatio()
                    )
                *
                -(chara.state.leftRightAxisRotationSpeed / chara.config.leftRightAxisRotationSpeed)
                ;
            dist = Vector2.Distance(targetOffset, state.targetOffset);
            targetOffset = state.targetOffset =
                Vector2.MoveTowards(
                    state.targetOffset,
                    targetOffset,
                    Mathf.Lerp(config.minSpeedRotationOffsetSmoothing,
                    config.maxSpeedRotationOffsetSmoothing, GetFolowRatio()
                    ) *
                    dist *
                    dt
                    );

            // Smooth the rotation offset

            Quaternion rotation = transform.rotation;
            targetPosition = chara.transform.position;
            targetPosition += chara.transform.up * 4f;

            if (config.tergetingCorrectionActivated)
            {
                targetPosition += chara.transform.right * targetOffset.x;
                targetPosition += chara.transform.up * targetOffset.y;
                targetPosition += chara.transform.rotation * config.targetOffset;
            }

            // Get up
            Vector3 up;
            if (config.rotateWithAvatar)
            {
                up = chara.transform.up;
            }
            else
            {
                up = Vector3.up;
                float dot = Vector3.Dot(up, chara.transform.forward);
                //dot = dot * dot * dot * dot;
                up = Vector3.SlerpUnclamped(up, chara.transform.up, dot);
                //Debug.Log(dot);
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    targetPosition - transform.position,
                    up
                    );
            float angle = Quaternion.Angle(targetRotation, rotation);
            rotation = Quaternion.RotateTowards(rotation, targetRotation, (angle * Mathf.Lerp(config.minSpeedRotationOffsetSmoothing2, config.maxSpeedRotationOffsetSmoothing2, GetFolowRatio()) + config.speedRotationOffsetLinearSmoothing2) * dt);
            transform.rotation = rotation;
        }
        #endregion
    }
}
