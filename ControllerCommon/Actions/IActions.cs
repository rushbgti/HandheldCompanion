﻿using System;
using System.Windows.Forms;
using ControllerCommon.Inputs;
using ControllerCommon.Managers;

namespace ControllerCommon.Actions;

[Serializable]
public enum ActionType
{
    Disabled = 0,
    Button = 1,
    Joystick = 2,
    Keyboard = 3,
    Mouse = 4,
    Trigger = 5
}

[Serializable]
public abstract class IActions : ICloneable
{
    protected bool IsToggled;
    protected bool IsTurboed;

    protected ScreenOrientation Orientation = ScreenOrientation.Angle0;

    protected int Period;
    protected object prevValue;
    protected int TurboIdx;

    protected object Value;

    public IActions()
    {
        Period = TimerManager.GetPeriod();
    }

    public ActionType ActionType { get; set; } = ActionType.Disabled;

    public bool Turbo { get; set; }
    public byte TurboDelay { get; set; } = 90;

    public bool Toggle { get; set; }
    public bool AutoRotate { get; set; } = false;

    // Improve me !
    public object Clone()
    {
        return MemberwiseClone();
    }

    public virtual void Execute(ButtonFlags button, bool value)
    {
    }

    public virtual void Execute(ButtonFlags button, short value)
    {
    }

    public virtual void Execute(AxisFlags axis, bool value)
    {
    }

    public virtual void Execute(AxisFlags axis, short value)
    {
    }

    public virtual void SetOrientation(ScreenOrientation orientation)
    {
        Orientation = orientation;
    }
}