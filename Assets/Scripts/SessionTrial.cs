using System.Collections;
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
public class AllTowerSpecs {

    public List<TowerSpec> towers;
    public AllTowerSpecs() {
        this.towers = new List<TowerSpec>(); // Empty list
    }
}


[System.Serializable]
public class TowerSpec {

    public List<BlockSpec> blocks;
    public TowerSpec() {
        this.blocks = new List<BlockSpec>(); // Empty list
    }
}

[System.Serializable]
public class BlockSpec {
    public float x;
    public float w;
    public float h;

    public BlockSpec(float x, float w, float h) {
        this.x = x;
        this.w = w;
        this.h = h;
    }
}

[System.Serializable]
public class SessionTrial
{

    // To store our different actors data
    public string user_response;
    public int trial_id;
    public double ts_start;
    public double ts_response;
    private bool responded = false;
    public List<RecordedHit> hits;

    public TowerSpec tower;

    public SessionTrial(int id) {
        this.tower = new TowerSpec();
        this.trial_id = id;
        this.ts_start = Util.timestamp();
        this.hits = new List<RecordedHit>();
    }

    public void StoreResponse(string response) {
        this.user_response = response;
        this.ts_response = Util.timestamp();
        this.responded = true;
    }

    public void addBlock(float x, float w, float h) {
        this.tower.blocks.Add(new BlockSpec(x, w, h));
    }

    public bool addHit(string key, Vector3 hitpoint, Quaternion hmd_rot, float conf) {
        this.hits.Add(new RecordedHit(key, hitpoint.x, hitpoint.y, hitpoint.z, this.responded, hmd_rot, conf));
        return true;
    }
}
