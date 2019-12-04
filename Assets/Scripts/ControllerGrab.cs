using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerGrab : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Behaviour_Pose controllerPose;
    public SteamVR_Action_Boolean grabAction;
    private GameObject collidingObject; // 1
    private GameObject objectInHand; // 2
    private ExperimentRunner exp;
    private bool inHandZone = false;


    private bool cardReleasable() {
        return (objectInHand && inHandZone);
    }

    private void Vibrate() {
        GetComponent<ControllerActions>().Vibrate();
    }
    private void SetCollidingObject(Collider col)
    {
        if (collidingObject || !col.GetComponent<Rigidbody>())
        {
            // Do nothing if already colliding with something
            return;
        }
        collidingObject = col.gameObject;
    }

    void Start() {
        this.exp = GameObject.Find("Camera").GetComponent<ExperimentRunner>();
    }

    // Update is called once per frame
    void Update()
    {
        // 1
        if (grabAction.GetLastStateDown(handType))
        {
            if (collidingObject)
            {
                GrabObject();
            }
        }

        // 2
        if (grabAction.GetLastStateUp(handType))
        {
            if (objectInHand)
            {
                ReleaseObject();
            }
        }

    }

    // 1
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "HandHolder") inHandZone = true;
        SetCollidingObject(other);
        if (this.cardReleasable()) {
            // Colliding with holder while holding card, provide feedback
            this.Vibrate();
        }
    }

    public void OnTriggerStay(Collider other)
    {
        SetCollidingObject(other);
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "HandHolder") inHandZone = false;
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
            AddFixedJoint(objectInHand);
            
        } else {
            Debug.Log("Tried to grab ungrabbable object");
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
        if (GetComponent<FixedJoint>())
        {
            Debug.Log("Release card");
            GetComponent<FixedJoint>().connectedBody = null;
            Destroy(GetComponent<FixedJoint>());
            

            if (this.cardReleasable()) {
                // Releasing card into hand
                // Snap to holder position
                GameObject centerHandCard = GameObject.Find("CardInHand1");
                objectInHand.transform.localPosition = centerHandCard.transform.localPosition + new Vector3(exp.CARD_SEP, 0, 0);
                objectInHand.transform.localRotation = centerHandCard.transform.localRotation;

                // Save subject selection for this trial and move on
                bool left = objectInHand.GetComponent<CardBehavior>().isLeft();
                if (left) exp.SubjectSelectCardLeft();
                else exp.SubjectSelectCardRight();
            } else {
                // Release with velocity
                objectInHand.GetComponent<Rigidbody>().velocity = controllerPose.GetVelocity();
                objectInHand.GetComponent<Rigidbody>().angularVelocity = controllerPose.GetAngularVelocity();
            }
        }
        objectInHand = null;
    }

}
