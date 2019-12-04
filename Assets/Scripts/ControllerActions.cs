using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerActions : MonoBehaviour
{

    public SteamVR_Input_Sources handType; // 1
    public SteamVR_Action_Boolean teleportAction; // 2
    public SteamVR_Action_Boolean grabAction; // 3

    public SteamVR_Action_Vibration vibrateAction;

    // Update is called once per frame
    void Update()
    {
        if (GetTeleportDown())
        {
            print("Teleport " + handType);
        }

        if (GetGrab())
        {
        }


    }

    public bool GetTeleportDown() // 1
    {
        return teleportAction.GetStateDown(handType);
    }

    public bool GetGrab() // 2
    {
        return grabAction.GetState(handType);
    }

    public void Vibrate() {
        this.vibrateAction.Execute(0, 0.7f, 50, 0.5f, SteamVR_Input_Sources.Any);
    }

}
