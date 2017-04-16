using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnityTick
{
    UPDATE,
    FIXED_UPDATE,
    LATE_UPDATE,
    MANUAL
}

public interface ITickable
{
    void Tick(float dt);
}

[DisallowMultipleComponent]
public class TickManager : MonoBehaviour {
    private static List<ITickable> update_tickable_list = new List<ITickable>();
    private static List<ITickable> fixed_update_tickable_list = new List<ITickable>();
    private static List<ITickable> late_update_tickable_list = new List<ITickable>();

    public static void AssignFast(ITickable tickable, UnityTick tick_type)
    {
        switch (tick_type)
        {
            case UnityTick.UPDATE:
                update_tickable_list.Add(tickable);
                break;
            case UnityTick.FIXED_UPDATE:
                fixed_update_tickable_list.Add(tickable);
                break;
            case UnityTick.LATE_UPDATE:
                late_update_tickable_list.Add(tickable);
                break;
        }
    }

    public static void ReassignFast(ITickable tickable, UnityTick from_tick_type, UnityTick to_tick_type)
    {
        // Remove
        switch (from_tick_type)
        {
            case UnityTick.UPDATE:
                update_tickable_list.Remove(tickable);
                break;
            case UnityTick.FIXED_UPDATE:
                fixed_update_tickable_list.Remove(tickable);
                break;
            case UnityTick.LATE_UPDATE:
                late_update_tickable_list.Remove(tickable);
                break;
        }

        // Add
        AssignFast(tickable, to_tick_type);
    }

    public static void Reassign(ITickable tickable, UnityTick tick_type)
    {
        // Remove
        switch (tick_type)
        {
            case UnityTick.UPDATE:
                fixed_update_tickable_list.Remove(tickable);
                late_update_tickable_list.Remove(tickable);
                break;
            case UnityTick.FIXED_UPDATE:
                update_tickable_list.Remove(tickable);
                late_update_tickable_list.Remove(tickable);
                break;
            case UnityTick.LATE_UPDATE:
                update_tickable_list.Remove(tickable);
                fixed_update_tickable_list.Remove(tickable);
                break;
        }

        AssignFast(tickable, tick_type);
    }

    protected void Update()
    {
        foreach(var tickable in update_tickable_list)
        {
            tickable.Tick(Time.deltaTime);
        }
    }

    protected void FixedUpdate()
    {
        foreach (var tickable in fixed_update_tickable_list)
        {
            tickable.Tick(Time.fixedDeltaTime);
        }
    }

    protected void LateUpdate()
    {
        foreach (var tickable in late_update_tickable_list)
        {
            tickable.Tick(Time.deltaTime);
        }
    }


}
