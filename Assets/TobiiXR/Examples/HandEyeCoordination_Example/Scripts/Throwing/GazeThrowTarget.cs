// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

using Tobii.G2OM;
using UnityEngine;

/// <summary>
/// Monobehaviour added to each target of the gaze assisted throwing.
/// </summary>
public class GazeThrowTarget : MonoBehaviour, IGazeFocusable
{
    public void GazeFocusChanged(bool hasFocus) { }
}
