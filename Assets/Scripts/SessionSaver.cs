using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;



[System.Serializable]
public class MySessionData
{
    private List<SessionTrial> trials = new List<SessionTrial>();
    public List<HandSpec> hand_specs = new List<HandSpec>();
    public List<int> hand_order = new List<int>();

    public string session_id;
    public bool left_handed = false;
    public int total_points = 0;
    public int total_points_possible = 0;

    public int CountTrials() {
        return this.trials.Count;
    }

    public void AddTrial(SessionTrial trial) {
        this.trials.Add(trial);
    }
}

public class SessionSaver : MonoBehaviour {
    public const string OUTDIR = "./ExperimentData/TrialData/";

    public MySessionData data = new MySessionData();

    public void AddTrial(SessionTrial trial) {
        if (data.CountTrials() >= trial.trial_id) {
            Debug.Log("Already saved?");
        } else {
            data.AddTrial(trial);
            if (trial.scored()) {
                data.total_points += trial.points;
                data.total_points_possible += 1;
            }
        }
    }

    public int CountTrials() {
        return data.CountTrials();
    }

    public string outfile() {
        return OUTDIR + "session_" + this.data.session_id + "_meta.json";
    }

    public void SaveToFile() {
        string json = JsonUtility.ToJson(this.data);
        string path = this.outfile();
        StreamWriter sw = File.CreateText(path);
        sw.Close();

        File.WriteAllText(path, json);
    }
}