﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RecordedHit {

    public double ts;
    public string hit_key;
    public float x;
    public float y;
    public float z;
    public float roll;
    public float pitch;
    public float yaw;
    public bool post_response;
    public float confidence;

    public RecordedHit(string key, float x, float y, float z, bool post_response, Quaternion hmd_rot, float conf) {
        this.ts = Util.timestamp();
        this.hit_key = key;
        this.x = x;
        this.y = y;
        this.z = z;
        this.confidence = conf;
        float _x = hmd_rot.x;
        float _y = hmd_rot.y;
        float _z = hmd_rot.z;
        float _w = hmd_rot.w;
        this.roll  = Mathf.Atan2(2*_y*_w + 2*_x*_z, 1 - 2*_y*_y - 2*_z*_z);
        this.pitch = Mathf.Atan2(2*_x*_w + 2*_y*_z, 1 - 2*_x*_x - 2*_z*_z);
        this.yaw   =  Mathf.Asin(2*_x*_y + 2*_z*_w);
        // Debug.Log(string.Format("Roll: {0} Pitch: {1} Yaw: {2}", this.roll, this.pitch, this.yaw));
        this.post_response = post_response;
    }
}


[System.Serializable]
public class AllHandSpecs {

    public List<HandSpec> hands;
    public List<HandSpec> practice_hands;
    public AllHandSpecs() {
        this.hands = new List<HandSpec>(); // Empty list
        this.practice_hands = new List<HandSpec>(); // Empty list
    }
}


[System.Serializable]
public class HandSpec {

    public List<string> table;
    public List<string> priv;
    public List<int> correct;

    public HandSpec() {
        this.table = new List<string>(); // Empty list
        this.priv = new List<string>(); // Empty list
        this.correct = new List<int>();
    }
}

[System.Serializable]
public class SessionTrial
{

    // To store our different actors data
    public int subject_choice;
    public int trial_id;
    public int points = 0;
    public bool correct = false;
    public bool avoided_prediction = false; 
    public double ts_start;
    public double ts_choice;
    private bool with_adversary = false;
    private bool choice_made = false;
    private bool practice = false;
    public List<RecordedHit> hits;

    public HandSpec hand;

    public SessionTrial(int id, HandSpec hand, bool adversary, bool practice) {
        this.hand = hand;
        this.trial_id = id;
        this.ts_start = Util.timestamp();
        this.with_adversary = adversary;
        this.points = 0;
        this.practice = practice;
        this.hits = new List<RecordedHit>();
    }

    public void StoreResponseAndScore(int pos) {
        this.subject_choice = pos;
        this.ts_choice = Util.timestamp();
        this.choice_made = true;
        this.correct = this.hand.correct[pos] == 1;

        // Score
        if (this.adversarial()) {
            // Make prediction based on eye fixations
            int adversary_prediction = Random.Range(0, 1); // TODO (currently coin flip)
            this.avoided_prediction = adversary_prediction != pos;
            bool successful = this.avoided_prediction && correct;
            this.points += successful ? 1 : 0;
        } else {
            bool successful = correct;
            this.points += successful ? 1 : 0;
        }
    }

    public bool adversarial() {
        return this.with_adversary;
    }

    public bool scored() {
        return !this.practice;
    }

    public bool addHit(string key, Vector3 hitpoint, Quaternion hmd_rot, float conf) {
        this.hits.Add(new RecordedHit(key, hitpoint.x, hitpoint.y, hitpoint.z, this.choice_made, hmd_rot, conf));
        return true;
    }
}
