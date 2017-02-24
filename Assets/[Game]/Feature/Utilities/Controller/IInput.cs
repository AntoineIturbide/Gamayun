using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.Controller
{
    public interface IInput
    {
        bool has_changed { get; }
    }

    public interface IInputButton : IInput
    {
        bool was_pressed { get; }
        bool was_released { get; }
        bool is_pressed { get; }
        bool is_released { get; }
    }

    public interface IInputVector<T> : IInput where T : struct
    {
        T value { get; }
        T last_value { get; }
    }

}