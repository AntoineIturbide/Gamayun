using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tweening
{
    public enum TweenType
    {
        FIXED,
        DYNAMIC
    }

    public class Tween<T> : ITickable
    {

        public T current_value;
        public T target_value;
        public T last_value;

        public UnityTick tick_type = UnityTick.UPDATE;

        public float time_factor = 1;
        public Easing.Ease.Interpolation<T> ease_fuction;

        public System.Action change_callback;

        public Tween(
            T start_value,
            UnityTick tick_type = UnityTick.UPDATE,
            Easing.Ease.Interpolation<T> ease_fuction = null,
            float time_factor = 1,
            System.Action change_callback = null
            )
        {

            this.target_value = this.current_value = start_value;
            this.tick_type = tick_type;
            this.time_factor = time_factor;
            this.ease_fuction = ease_fuction;
            this.change_callback = change_callback;

            if (this.tick_type != UnityTick.MANUAL)
            {
                TickManager.AssignFast(this, this.tick_type);
            }
        }

        public T get_value()
        {
            return current_value;
        }
        public void SetTarget(T target)
        {
            target_value = target;
        }

        public void Tick(float dt)
        {
            last_value = current_value;

            if (current_value.Equals(target_value))
            {
                return;
            }

            if (ease_fuction != null)
            {
                current_value = ease_fuction(current_value, target_value, dt, time_factor);
            }

            if (!last_value.Equals(current_value))
            {
                if (change_callback != null)
                {
                    change_callback();
                }
            }
        }
    }
}
namespace Easing
{
    public class Ease
    {
        public delegate T Interpolation<T>(T start, T end, float dt, float factor);
    }
    public class DynaEase
    {
        // Linear
        public static float Linear(float current, float target, float dt, float factor)
        {
            return Mathf.MoveTowards(current, target, dt * factor);
        }

        public static Vector2 Linear(Vector2 current, Vector2 target, float dt, float factor)
        {
            return Vector2.MoveTowards(current, target, dt * factor);
        }

        public static Vector3 Linear(Vector3 current, Vector3 target, float dt, float factor)
        {
            return Vector3.MoveTowards(current, target, dt * factor);
        }

        // Ease Out
        const float out_min = 0.015625f;
        public static float Out(float current, float target, float dt, float factor)
        {
            float delta = Mathf.Abs(target - current);
            return Mathf.MoveTowards(current, target, Mathf.Max(delta * dt * factor, out_min));
        }

        public static Vector2 Out(Vector2 current, Vector2 target, float dt, float factor)
        {
            float delta = Vector2.Distance(target, current);
            return Vector2.MoveTowards(current, target, Mathf.Max(delta * dt * factor, out_min));
        }

        public static Vector3 Out(Vector3 current, Vector3 target, float dt, float factor)
        {
            float delta = Vector3.Distance(target, current);
            return Vector3.MoveTowards(current, target, Mathf.Max(delta * dt * factor, out_min));
        }
    }
}