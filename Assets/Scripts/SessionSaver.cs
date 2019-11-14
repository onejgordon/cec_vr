using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;



[System.Serializable]
public class MySessionData
{
    public List<SessionTrial> trials = new List<SessionTrial>();
    public List<HandSpec> hand_specs = new List<TowerSpec>();
    public List<int> hand_order = new List<int>();
}

public class SessionSaver : MonoBehaviour {
    const string OUTDIR = "./TrialData/";

    public MySessionData data = new MySessionData();

    public void AddTrialResult(SessionTrial trial) {
        if (data.trials.Count >= trial.trial_id) {
            Debug.Log("Already saved?");
        } else {
            data.trials.Add(trial);
        }
    }

    public string outfile() {
        return OUTDIR + "session_data_" + ((int)(Util.timestamp())).ToString() + ".json";
    }

    public void SaveToFile() {
        string json = JsonUtility.ToJson(this.data);
        string path = this.outfile();
        StreamWriter sw = File.CreateText(path);
        sw.Close();

        File.WriteAllText(path, json);
    }
}