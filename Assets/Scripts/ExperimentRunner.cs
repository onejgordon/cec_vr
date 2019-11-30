﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.Extras;
using System.IO;
using PupilLabs;

[RequireComponent(typeof(RecordingController))]
public class ExperimentRunner : MonoBehaviour
{
    RecordingController recording;
    private Color SKY_RUNNING = new Color(0, 80, 150);
    private Color SKY_DEFAULT = new Color(50, 50, 50);
    public int N_TRIALS = 5; // Towers to run
    public int MAX_TRIALS = 1; // Set to 0 for production. Just for short debug data collection
    public int SELECTION_SECS = 5;
    public int DECISION_SECS = 10;
    public int ADVERSARY_DELAY_MINS = 10;
    public int EXP_MAX_MINS = 25;
    private int ts_exp_start = 0; // Timestamp
    private int trial_index = 0;
    private SessionTrial current_trial;
    public Transform card;
    public SessionSaver session;
    public Canvas UIcanvas;
    public SteamVR_LaserPointer laserPointer;
    public Canvas hmdcanvas;
    private List<HandSpec> hands;
    private bool buttons_showing = false;
    public bool record = false;
    private List<Transform> cards = new List<Transform>();
    private List<int> hand_order = new List<int>();
    private string condition = null; // "immediate" or "delayed"
    private bool adversary_active = false;

    // Start is called before the first frame update
    void Start()
    {
        this.hands = new List<HandSpec>();
        string path = "./TrialData/hands.json";
        if(File.Exists(path))
        {
            string dataAsJson = File.ReadAllText(path);
            this.hands = JsonUtility.FromJson<AllHandSpecs>(dataAsJson).hands;
            this.RandomizeTrialOrder();
            N_TRIALS = this.hands.Count;
            Debug.Log(string.Format("Loaded {0} hands from {1}", this.hands.Count, path));
        } else Debug.Log(string.Format("{0} doesn't exist", path));
        if (record) recording = GetComponent<RecordingController>();
        this.randomize_condition();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void randomize_condition() {
        int c = Random.Range(0, 1)
        if (c == 0) { this.condition = "immediate"; }
        else { this.condition = "delayed"; }
    }

    private int minutes_in() {
        return (Util.timestamp() - this.ts_exp_start) / 60;
    }

    void RandomizeTrialOrder() {
        // Shuffle and save
        System.Random rng = new System.Random();
        this.hand_order = Enumerable.Range(0,this.towers.Count).ToList();
        this.hand_order = this.hand_order.OrderBy(a => rng.Next()).ToList();
        // Save order to session
        this.session.data.hand_order = this.hand_order;
    }

    public void BeginExperiment() {
        if (record) {
            recording.StartRecording();
        }
        this.ts_exp_start = Util.timestamp();
        BeginNextTrial();
    }

    public void BeginNextTrial() {
        this.trial_index += 1;
        bool show_adversary_info = false;
        bool activate = false;
        int mins = this.minutes_in();
        if (!this.adversary_active) {
            if (this.condition == "delayed") {
                // Check to see if we should activate adversary (X minutes passed)
                activate = mins > ADVERSARY_DELAY_MINS;
            } else {
                // Immediate, activate now
                activate = true;
            }
        }
        if (activate) {
            this.adversary_active = true;
            show_adversary_info = true;
        }
        this.current_trial = new SessionTrial(this.trial_index, this.adversary_active);
        GetComponent<Camera>().backgroundColor = SKY_DEFAULT;
        Debug.Log("Running trial " + this.trial_index.ToString());
        if (mins > EXP_MAX_MINS) Finish();
        else {
            if (show_adversary_info) ShowAdversaryInfoThenTrial(); // Calls RunOneTrial
            else RunOneTrial();
        }
    }

    void ShowAdversaryInfoThenTrial() {
        // TODO: Messaging/tutorial on adversary
        Debug.Log("Following trials will have adversary.");
        RunOneTrial();
    }

    void RunOneTrial() {
        MaybeClearHand();
        int hand_index = this.hand_order[this.trial_index - 1];
        DealHand(this.hands[hand_index]);
        // Immediately start decision stage / timer
        BeginDecisionStage();
    }

    public void SubjectSelection(int position) {
        // Score selection and save trial to session
        this.current_trial.StoreResponse(position);
        bool correct_selection = this.current_trial.hand.correct[position]
        if (this.current_trial.adversarial()) {
            // Make prediction based on eye fixations
            int adversary_prediction = 0; // TODO (currently always left)
            bool avoided_prediction = adversary_prediction != position;
            bool successful = avoided_prediction && correct_selection;
            this.current_trial.points += successful ? 1 : 0;
        } else {
            bool successful = correct_selection;
            this.current_trial.points += successful ? 1 : 0;
        }
        session.AddTrial(this.current_trial);
    }

    public void SubjectSelectCardLeft() {
        this.SubjectSelection(0);
    }

    public void SubjectSelectCardRight() {
        this.SubjectSelection(1);
    }

    public SessionTrial getCurrentTrial() {
        return this.current_trial;
    }

    public void RecordHit(string hitKey, RaycastHit hit, float conf) {
        // Debug.Log("Hit " + hitKey);
        Vector3 hitpoint = hit.point;
        if (this.current_trial != null) {
            this.current_trial.addHit(hitKey, hitpoint, GetComponent<Camera>().transform.rotation, conf);
        }
    }

    void BeginDecisionStage() {
        // Show decision prompt
        ShowButtons();
        Invoke("BeginSelectionStage", DECISION_SECS);
    }

    void BeginSelectionStage() {
        // Await user choice
        // Possibly update "adversary watching" indicator
        GetComponent<Camera>().backgroundColor = SKY_RUNNING;
        Invoke("BeginNextTrial", SELECTION_SECS);
    }

    void MaybeClearHand() {
        for (int i=0; i<this.cards.Count; i++) {
            Destroy(this.cards[i].gameObject);
        }
        this.cards.Clear();
    }

    void DealHand(HandSpec hs) {
        // Deal cards to private hand
        for (int i=0; i<2; i++) {
            string card_id = hs.priv[i];
            float x = 5.0f * i;
            float y = 0.0f;
            float z = 0.0f;
            float rot_x = 90.0f;
            Transform newCard = AddCardToScene(i, x, y, z, rot_x);
        }
        // Deal cards to table
        for (int i=0; i<2; i++) {
            string card_id = hs.table[i];
            float x = 5.0f * i;
            float y = 0.0f;
            float z = 20.0f;
            float rot_x = 0.0f;
            Transform newCard = AddCardToScene(i, x, y, z, rot_x);
        }
        session.data.hand_specs.Add(hs);
    }

    private Transform AddCardToScene(string id, float x, float y, float z, float rot_x) {
        Transform newCard = Instantiate(card, new Vector3(x, y, z), Quaternion.identity);
        // TODO: Rotate
        newCard.localScale = new Vector3(CARD_W, CARD_H, 1);
        newCard.GetComponent<CardBehavior>().setID(id);
        // TODO: Test if necessary to produce gaze target events
        BoxCollider collider = newCard.gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        this.cards.Add(newCard);
        return newCard;
    }

    void Finish() {
        Debug.Log("Done, saving...");
        session.SaveToFile();
        if (record) recording.StopRecording();
        MaybeClearHand();
        CanvasGroup cg = this.hmdcanvas.GetComponent<CanvasGroup>();
        cg.alpha = 1;
    }

    // -------- Not in Use


    void HideButtons() {
        CanvasGroup cg = this.UIcanvas.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.interactable = false;
        this.buttons_showing = false;
        // Hide laser pointer
        this.laserPointer.color.a = 0;
    }

    void ShowButtons() {
        CanvasGroup cg = this.UIcanvas.GetComponent<CanvasGroup>();
        cg.alpha = 1;
        cg.interactable = true;
        this.buttons_showing = true;
        // Show laser pointer
        this.laserPointer.color.a = 1;
    }

}
