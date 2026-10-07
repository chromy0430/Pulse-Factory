using System;
using Sirenix.OdinInspector;
using UnityEngine;

public enum PowerPortRole
{
    Free,
    Input,
    Output
}

public enum PowerPortPosition
{
    NorthWest,
    North,
    NorthEast,

    West,
    East,

    SouthWest,
    South,
    SouthEast
}

[Serializable]
public sealed class PowerPortSlot
{
    // ============================================================
    // Setup
    // ============================================================

    [LabelText("Position")]
    [SerializeField]
    private PowerPortPosition position;

    [LabelText("Anchor")]
    [SerializeField]
    private Transform anchor;


    // ============================================================
    // Runtime
    // ============================================================

    [ShowInInspector]
    [ReadOnly]
    [LabelText("Role")]
    public PowerPortRole Role { get; private set; }
        = PowerPortRole.Free;

    [ShowInInspector]
    [ReadOnly]
    [LabelText("Connected Cable")]
    public PowerCable ConnectedCable { get; private set; }


    // ============================================================
    // Properties
    // ============================================================

    public PowerPortPosition Position =>
        position;

    public Transform Anchor =>
        anchor;

    public bool IsFree =>
        Role == PowerPortRole.Free &&
        ConnectedCable == null;


    // ============================================================
    // Connection
    // ============================================================

    public bool TryConnect(
        PowerPortRole role,
        PowerCable cable)
    {
        if (!IsFree)
            return false;

        if (role == PowerPortRole.Free)
            return false;

        if (cable == null)
            return false;

        Role = role;
        ConnectedCable = cable;

        return true;
    }


    public void Disconnect(
        PowerCable cable)
    {
        if (ConnectedCable != cable)
            return;

        ConnectedCable = null;
        Role = PowerPortRole.Free;
    }
}