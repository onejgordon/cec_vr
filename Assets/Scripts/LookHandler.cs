using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.G2OM;
using Tobii.XR;

public class LookHandler : MonoBehaviour, IGazeFocusable 
{
    private ExperimentRunner exp;
    public bool recordDetailedHits = false;
    bool gazedAt = false;
    double last_gaze_start_ts = 0.0f;
    private string name;

    void Start() {
        this.exp = GameObject.Find("Camera").GetComponent<ExperimentRunner>();
        this.name = this.objectName();
    }
    void Update()
    {
        if (gazedAt && this.recordDetailedHits) {
            // Get eye tracking data in world space
            var eyeTrackingData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);
            
            // Check if gaze ray is valid
            if (eyeTrackingData.GazeRay.IsValid) {
                // The origin of the gaze ray is a 3D point
                var rayOrigin = eyeTrackingData.GazeRay.Origin;
                // The direction of the gaze ray is a normalized direction vector
                var rayDirection = eyeTrackingData.GazeRay.Direction;
            }   
        }

    }

    private string objectName() {
        string name = gameObject.name;
        if (name == "Face") {
            // Use parent's name which specifies card type/position
            name = gameObject.transform.parent.gameObject.name;
        }
        return name;
    }

    public void GazeFocusChanged(bool focused) {
        this.gazedAt = focused;
        if (focused) {
            // Start gaze timer
            this.last_gaze_start_ts = Util.timestamp();
            Debug.Log("Starting fixation on " + this.name);
        } else {
            // Stop gaze timer and record fixation
            if (this.last_gaze_start_ts > 0.0f) {
                Debug.Log(">> Adding fixation on " + this.name);
                SessionTrial trial = exp.getCurrentTrial();
                if (trial != null) trial.addFixation(this.name, this.last_gaze_start_ts, Util.timestamp());
                this.last_gaze_start_ts = 0.0f;
            }
        }
    }

}

