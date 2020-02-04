using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerGrab : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Behaviour_Pose controllerPose;
    public SteamVR_Action_Boolean grabAction;
    public SteamVR_Action_Vibration vibrateAction;
    private GameObject collidingObject;
    private GameObject objectInHand;
    private ExperimentRunner exp;
    private HolderBehavior holder;
    private bool inHandZone = false;
    private FixedJoint joint = null;

    void Start() {
        this.exp = GameObject.Find("Camera").GetComponent<ExperimentRunner>();
        this.holder = GameObject.Find("HandHolder").GetComponent<HolderBehavior>();
    }

    // Update is called once per frame
    void Update()
    {
        if (grabAction.GetLastStateDown(handType))
        {
            if (collidingObject)
            {
                GrabObject();
            }
        }

        if (grabAction.GetLastStateUp(handType))
        {
            if (objectInHand)
            {
                ReleaseObject();
            }
        }

    }

    public void DisconnectJoint() {
        if (this.joint != null)
        {
            Debug.Log("Destroying joint");
            this.joint.connectedBody = null;
            Destroy(this.joint);
            this.joint = null;
        }
    }

    public void ResetState() {
        Debug.Log("Resetting controller dynamics...");
        // Reset dynamics state
        // if (this.objectInHand != null) this.objectInHand.tag = "NotGrabbable"; // Prevent immediate re-grab
        this.objectInHand = null;
        this.collidingObject = null;
        this.holder.setHighlight(false);
        this.DisconnectJoint();
    }


    private bool cardPlaceable() {
        return (this.holdingCard() && inHandZone);
    }

    private void Vibrate() {
        // this.vibrateAction.Execute(0, 0.7f, 50, 0.5f, SteamVR_Input_Sources.Any);
    }
    private void SetCollidingObject(Collider col)
    {
        if (collidingObject != null || !col.GetComponent<Rigidbody>())
        {
            // Do nothing if already colliding with something
            return;
        }
        if (col.gameObject.name.StartsWith("Controller")) return;
        // Debug.Log("Setting colliding to " + col.gameObject.ToString());
        collidingObject = col.gameObject;
    }


    public void EnteredHandZone(bool in_zone) {
        inHandZone = in_zone;
    }

    private bool holdingCard() {
        return this.joint != null;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "HandHolder") EnteredHandZone(true);
        SetCollidingObject(other);
        if (this.cardPlaceable()) {
            // Colliding with holder while holding card, provide feedback
            this.Vibrate();
            this.holder.setHighlight(true);
        }
    }

    public void OnTriggerStay(Collider other)
    {
        SetCollidingObject(other);
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "HandHolder") {
            EnteredHandZone(false);
            this.holder.setHighlight(false);
        }
        if (!collidingObject)
        {
            return;
        }

        collidingObject = null;
    }

    private void GrabObject()
    {
        if (collidingObject.tag == "Grabbable") {
            Debug.Log("Grab " + collidingObject.ToString());
            objectInHand = collidingObject;
            collidingObject = null;
            this.joint = AddFixedJoint(objectInHand);
        } else {
            // Debug.Log("Tried to grab ungrabbable object");
        }
    }

    private FixedJoint AddFixedJoint(GameObject objectInHand)
    {
        // Links controller with card
        FixedJoint fx = gameObject.AddComponent<FixedJoint>();
        fx.breakForce = 20000;
        fx.breakTorque = 20000;
        fx.connectedBody = objectInHand.GetComponent<Rigidbody>();
        return fx;
    }

    private void ReleaseObject()
    {
        if (this.holdingCard())
        {
            Debug.Log("Release card");
            bool placeable = this.cardPlaceable();
            this.DisconnectJoint();
            if (placeable) {
                // Releasing card into hand
                // Snap to holder position
                GameObject placeholder = GameObject.Find("Placeholder");
                objectInHand.transform.position = placeholder.transform.position;
                objectInHand.transform.rotation = placeholder.transform.rotation;

                // Save subject selection for this trial and move on
                bool left = objectInHand.GetComponent<CardBehavior>().isLeft();
                if (left) exp.SubjectSelectCardLeft();
                else exp.SubjectSelectCardRight();
                this.holder.setHighlight(false);
            } else {
                // Not releasable on holder, so release to air with velocity
                Debug.Log("Releasing into air");
                objectInHand.GetComponent<Rigidbody>().velocity = controllerPose.GetVelocity();
                objectInHand.GetComponent<Rigidbody>().angularVelocity = controllerPose.GetAngularVelocity();
            }
        }
        objectInHand = null;
    }

}
