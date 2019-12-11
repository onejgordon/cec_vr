using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;

public enum ControllerButton
{
    Menu,
    Touchpad,
    Trigger
}

public interface IControllerAdapter
{
    /// <summary>
    /// The velocity of the controller.
    /// </summary>
    Vector3 Velocity { get; }

    /// <summary>
    /// The angular velocity of the controller.
    /// </summary>
    Vector3 AngularVelocity { get; }

    /// <summary>
    /// The position of the controller in world space. 
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// The rotation of the controller in world space.
    /// </summary>
    Quaternion Rotation { get; }


    /// <summary>
    /// Is a given button pressed or not during this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if button is being pressed this frame, otherwise false.</returns>
    bool GetButtonPress(ControllerButton button);

    /// <summary>
    /// Did a button go from not pressed to pressed this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button went from being up to being pressed down this frame, otherwise false.</returns>
    bool GetButtonPressDown(ControllerButton button);

    /// <summary>
    /// Did a button go from pressed to not pressed this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button went from being pressed down to not being pressed this frame, otherwise false.</returns>
    bool GetButtonPressUp(ControllerButton button);

    /// <summary>
    /// Is a given button being touched or not during this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if button is being touched this frame, otherwise false.</returns>
    bool GetButtonTouch(ControllerButton button);

    /// <summary>
    /// Did a button go from not being touched to being touched this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button went from not being touched to being touched this frame, otherwise false.</returns>
    bool GetButtonTouchDown(ControllerButton button);

    /// <summary>
    /// Did a button go from being touched to not touched this frame.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button went from being touched to not being touched this frame, otherwise false.</returns>
    bool GetButtonTouchUp(ControllerButton button);

    /// <summary>
    /// Trigger a haptic pulse on the controller.
    /// </summary>
    /// <param name="pulseDurationMicroSeconds">Duration of the haptic pulse in microseconds.</param>
    void TriggerHapticPulse(ushort pulseDurationMicroSeconds);

    /// <summary>
    /// Get the touchpad touch position.
    /// </summary>
    /// <returns>Vector2 with the thumb's position on the touchpad.</returns>
    Vector2 GetTouchpadAxis();
}

#if TOBIIXR_SNAPDRAGONVRPROVIDER && UNITY_ANDROID
public class SxrControllerAdapter : IControllerAdapter
{
    private int _previousFrameCount;
    private Vector3 _angularVelocity;
    private Quaternion _controllerLocalRotation;
    private Vector3 _controllerLocalPosition;

    public Vector3 Velocity
    {
        get
        {
            UpdateController();
            return Vector3.zero;
        }
    }

    public Vector3 AngularVelocity
    {
        get
        {
            UpdateController();
            return _angularVelocity;
        }
    }

    public Vector3 Position
    {
        get
        {
            UpdateController();
            return _controllerLocalPosition;
        }
    }

    public Quaternion Rotation
    {
        get
        {
            UpdateController();
            return _controllerLocalRotation;
        }
    }

    public bool GetButtonPress(ControllerButton button)
    {
        return SvrInput.Instance.PrimaryController.GetButton(SvrControllerButtonFrom(button));
    }

    public bool GetButtonPressDown(ControllerButton button)
    {
        return SvrInput.Instance.PrimaryController.GetButtonDown(SvrControllerButtonFrom(button));
    }

    public bool GetButtonPressUp(ControllerButton button)
    {
        return SvrInput.Instance.PrimaryController.GetButtonUp(SvrControllerButtonFrom(button));
    }

    public bool GetButtonTouch(ControllerButton button)
    {
        return SvrInput.Instance.PrimaryController.GetTouch(SvrControllerTouchFrom(button));
    }

    public bool GetButtonTouchDown(ControllerButton button)
    {
        return SvrInput.Instance.PrimaryController.GetTouchDown(SvrControllerTouchFrom(button));
    }

    public bool GetButtonTouchUp(ControllerButton button)
    {
        return SvrInput.Instance.PrimaryController.GetTouchUp(SvrControllerTouchFrom(button));
    }

    public void TriggerHapticPulse(ushort pulseDurationMicroSeconds)
    {
        SvrInput.Instance.PrimaryController.Vibrate(0, pulseDurationMicroSeconds);
    }

    public Vector2 GetTouchpadAxis()
    {
        return SvrInput.Instance.PrimaryController.GetAxis2D(SvrController.svrControllerAxis2D.PrimaryThumbstick);
    }

    private void UpdateController()
    {
        if (Time.frameCount != _previousFrameCount)
        {
            _previousFrameCount = Time.frameCount;

            var state = SvrInput.Instance.PrimaryController.State;
            _controllerLocalPosition = state.position;
            _controllerLocalRotation = state.rotation;
            _angularVelocity = state.gyro;
        }
    }

    private static SvrController.svrControllerButton SvrControllerButtonFrom(ControllerButton button)
    {
        switch (button)
        {
            case ControllerButton.Menu:
                return SvrController.svrControllerButton.Start;
            case ControllerButton.Touchpad:
                return SvrController.svrControllerButton.PrimaryThumbstick;
            case ControllerButton.Trigger:
                return SvrController.svrControllerButton.PrimaryIndexTrigger;
            default:
                throw new System.Exception("Unmapped controller button: " + button.ToString());
        }
    }

    private static SvrController.svrControllerTouch SvrControllerTouchFrom(ControllerButton button)
    {
        switch (button)
        {
            case ControllerButton.Touchpad:
                return SvrController.svrControllerTouch.PrimaryThumbstick;
            case ControllerButton.Menu:
            case ControllerButton.Trigger:
            default:
                throw new System.Exception("Unmapped controller button: " + button.ToString());
        }
    }
}
#else
public class OpenVRControllerAdapter : IControllerAdapter
{
    private readonly List<XRNodeState> _nodeStates = new List<XRNodeState>();
    private uint _controllerIndex = Valve.VR.OpenVR.k_unTrackedDeviceIndexInvalid;
    private Valve.VR.VRControllerState_t _controllerState, _previousControllerState;
    private int _previousFrameCount;
    private Vector3 _angularVelocity;
    private Vector3 _velocity;
    private Quaternion _controllerLocalRotation;
    private Vector3 _controllerLocalPosition;

    // Use the right hand controller for tracking position and rotation.
    private const XRNode ControllerHand = XRNode.RightHand;
    private const Valve.VR.ETrackedControllerRole ControllerRole = Valve.VR.ETrackedControllerRole.RightHand;
    private const uint VibrationAxisId = 0u;

    public Vector3 Velocity
    {
        get
        {
            UpdateController();
            return _velocity;
        }
    }

    public Vector3 AngularVelocity
    {
        get
        {
            UpdateController();
            return _angularVelocity;
        }
    }

    public Vector3 Position
    {
        get
        {
            UpdateController();
            return _controllerLocalPosition;
        }
    }

    public Quaternion Rotation
    {
        get
        {
            UpdateController();
            return _controllerLocalRotation;
        }
    }

    public bool GetButtonPress(ControllerButton button)
    {
        var b = EVRButtonIdFrom(button);
        UpdateController();
        var buttonMask = (1ul << (int)b);
        return (_controllerState.ulButtonPressed & buttonMask) != 0;
    }

    public bool GetButtonPressDown(ControllerButton button)
    {
        var b = EVRButtonIdFrom(button);
        UpdateController();
        var buttonMask = (1ul << (int)b);
        return (_controllerState.ulButtonPressed & buttonMask) != 0 && (_previousControllerState.ulButtonPressed & buttonMask) == 0;
    }

    public bool GetButtonPressUp(ControllerButton button)
    {
        var b = EVRButtonIdFrom(button);
        UpdateController();
        var buttonMask = (1ul << (int)b);
        return (_controllerState.ulButtonPressed & buttonMask) == 0 && (_previousControllerState.ulButtonPressed & buttonMask) != 0;
    }

    public bool GetButtonTouch(ControllerButton button)
    {
        var b = EVRButtonIdFrom(button);
        UpdateController();
        var buttonMask = (1ul << (int)b);
        return (_controllerState.ulButtonTouched & buttonMask) != 0;
    }

    public bool GetButtonTouchDown(ControllerButton button)
    {
        var b = EVRButtonIdFrom(button);
        UpdateController();
        var buttonMask = (1ul << (int)b);
        return (_controllerState.ulButtonTouched & buttonMask) != 0 && (_previousControllerState.ulButtonTouched & buttonMask) == 0;
    }

    public bool GetButtonTouchUp(ControllerButton button)
    {
        var b = EVRButtonIdFrom(button);
        UpdateController();
        var buttonMask = (1ul << (int)b);
        return (_controllerState.ulButtonTouched & buttonMask) == 0 && (_previousControllerState.ulButtonTouched & buttonMask) != 0;
    }

    public void TriggerHapticPulse(ushort pulseDurationMicroSeconds)
    {
        var system = Valve.VR.OpenVR.System;
        if (system != null)
        {
            system.TriggerHapticPulse(_controllerIndex, VibrationAxisId, (char)pulseDurationMicroSeconds);
        }
    }

    public Vector2 GetTouchpadAxis()
    {
        UpdateController();
        return new Vector2(_controllerState.rAxis0.x, _controllerState.rAxis0.y);
    }

    /// <summary>
    /// Updates the controller state from OpenVR and Unity's InputTracking.
    /// </summary>
    private void UpdateController()
    {
        if (Time.frameCount != _previousFrameCount)
        {
            _previousFrameCount = Time.frameCount;
            _previousControllerState = _controllerState;

            var system = Valve.VR.OpenVR.System;
            if (system != null)
            {
                _controllerIndex = system.GetTrackedDeviceIndexForControllerRole(ControllerRole);
                system.GetControllerState(_controllerIndex, ref _controllerState, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Valve.VR.VRControllerState_t)));
            }

            UpdateControllerPositionAndRotation();
        }
    }

    /// <summary>
    /// Updates the position, rotation, and velocity of the controller.
    /// </summary>
    private void UpdateControllerPositionAndRotation()
    {
        // Use Unity's InputTracking to get the velocity and angular velocity of the controller
        InputTracking.GetNodeStates(_nodeStates);
        foreach (var xrNodeState in _nodeStates)
        {
            if (xrNodeState.nodeType != ControllerHand) continue;

            if (!xrNodeState.tracked) return;

            Vector3 velocityLocal;
            if (xrNodeState.TryGetVelocity(out velocityLocal))
            {
                _velocity = velocityLocal;
            }

            Vector3 angularVelocity;
            if (xrNodeState.TryGetAngularVelocity(out angularVelocity))
            {
                _angularVelocity = angularVelocity;
            }

            Vector3 position;
            if (xrNodeState.TryGetPosition(out position))
            {
                _controllerLocalPosition = position;
            }

            Quaternion rotation;
            if (xrNodeState.TryGetRotation(out rotation))
            {
                _controllerLocalRotation = rotation;
            }
        }
    }

    private static Valve.VR.EVRButtonId EVRButtonIdFrom(ControllerButton button)
    {
        switch (button)
        {
            case ControllerButton.Menu:
                return Valve.VR.EVRButtonId.k_EButton_ApplicationMenu;
            case ControllerButton.Touchpad:
                return Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
            case ControllerButton.Trigger:
                return Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
            default:
                throw new System.Exception("Unmapped controller button: " + button.ToString());
        }
    }
}
#endif

public class ControllerManager : MonoBehaviour
{
    private static ControllerManager _instance;

#if TOBIIXR_SNAPDRAGONVRPROVIDER && UNITY_ANDROID
    private IControllerAdapter _controllerAdapter = new SxrControllerAdapter();
#else
    private IControllerAdapter _controllerAdapter = new OpenVRControllerAdapter();
#endif

    /// <summary>
    /// Instance of the controller manager which can be statically accessed.
    /// </summary>
    public static ControllerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ControllerManager>();
            }
            return _instance;
        }
    }

    public Vector3 Velocity
    {
        get
        {
            return transform.TransformVector(_controllerAdapter.Velocity);
        }
    }

    public Vector3 AngularVelocity
    {
        get
        {
            return _controllerAdapter.AngularVelocity;
        }
    }

    public Vector3 Position
    {
        get
        {
            return transform.TransformPoint(_controllerAdapter.Position);
        }
    }

    public Quaternion Rotation
    {
        get
        {
            return transform.rotation * _controllerAdapter.Rotation;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public bool GetButtonPress(ControllerButton button)
    {
        return _controllerAdapter.GetButtonPress(button);
    }

    public bool GetButtonPressDown(ControllerButton button)
    {
        return _controllerAdapter.GetButtonPressDown(button);
    }

    public bool GetButtonPressUp(ControllerButton button)
    {
        return _controllerAdapter.GetButtonPressUp(button);
    }

    public bool GetButtonTouch(ControllerButton button)
    {
        return _controllerAdapter.GetButtonTouch(button);
    }

    public bool GetButtonTouchDown(ControllerButton button)
    {
        return _controllerAdapter.GetButtonTouchDown(button);
    }

    public bool GetButtonTouchUp(ControllerButton button)
    {
        return _controllerAdapter.GetButtonTouchUp(button);
    }

    public void TriggerHapticPulse(ushort pulseDurationMicroSeconds)
    {
        _controllerAdapter.TriggerHapticPulse(pulseDurationMicroSeconds);
    }

    public Vector2 GetTouchpadAxis()
    {
        return _controllerAdapter.GetTouchpadAxis();
    }
}
