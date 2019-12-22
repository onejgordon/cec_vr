using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.Extras;
using System.IO;
using Tobii.XR;

public class ExperimentRunner : MonoBehaviour
{
    private Color SKY_ADVERSARY = new Color(200, 0, 0);
    private Color SKY_DEFAULT = new Color(150, 150, 150);

    private int N_TRIALS = 5;
    public int MAX_TRIALS = 0; // Set to 0 for production. Just for short debug data collection
    public int PICKUP_SECS = 4; // -1 for infinite time (wait for user choice)
    public int DECISION_SECS = 10;
    public int ADVERSARY_DELAY_MINS = 10;
    public int ADVERSARY_FORCE_AFTER_ROUNDS = 2; // Set to -1 to not force (for production)
    public int EXP_MAX_MINS = 25;
    public bool left_handed = false;
    public float CARD_SEP = 0.2f;
    private double ts_exp_start = 0; // Timestamp
    private int trial_index = 0;
    public int practice_rounds = 2;
    private int practice_remaining = 0; // 0 to disable practice
    private SessionTrial current_trial;
    public Transform card;
    public SessionSaver session;
    public SteamVR_LaserPointer laserPointer;
    private List<HandSpec> hands;
    private List<HandSpec> practice_hands;
    public bool record = false;
    private bool recording = false;
    private List<Transform> cards = new List<Transform>();
    private List<int> hand_order = new List<int>();
    private string condition = null; // "immediate" or "delayed"
    private bool adversary_active = false;
    private bool practicing = false;
    public Transform handholder;
    public Transform table;
    public UIBehavior ui;
    public GameObject room;
    private string session_id;
    private Transform hmdCamera;
    private Transform controller;

    private Color DGREEN = new Color(0.0f, 0.0f, 0.6f);

    // Start is called before the first frame update
    void Start()
    {
        this.session_id = ((int)Util.timestamp()).ToString();
        this.session.data.session_id = this.session_id;
        this.session.data.left_handed = this.left_handed;
        this.practice_remaining = this.practice_rounds;
        this.ui = GameObject.Find("UICanvas").GetComponent<UIBehavior>();
        this.hmdCamera = GameObject.Find("Camera").GetComponent<Transform>();
        this.controller = GameObject.FindGameObjectWithTag("Controller").GetComponent<Transform>();
        this.hands = new List<HandSpec>();
        string path = "./ExperimentData/hands.json";
        if(File.Exists(path))
        {
            string dataAsJson = File.ReadAllText(path);
            AllHandSpecs ahs = JsonUtility.FromJson<AllHandSpecs>(dataAsJson);
            this.hands = ahs.hands;
            this.practice_hands = ahs.practice_hands;
            if (this.practice_hands.Count > 0) this.practicing = true;
            this.RandomizeTrialOrder();
            N_TRIALS = this.hands.Count;
            Debug.Log(string.Format("Loaded {0} hands from {1}", this.hands.Count, path));
        } else Debug.Log(string.Format("{0} doesn't exist", path));
        this.randomize_condition();
        this.BeginExperiment();
    }

    // Update is called once per frame
    void Update()
    {
        if (recording) {
            Quaternion hmdRot = hmdCamera.rotation;
            Quaternion ctrlRot = controller.rotation;
            Vector3 gazeOrigin = new Vector3();
            Vector3 gazeDirection = new Vector3();
            var eyeTrackingData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);
            if (eyeTrackingData.GazeRay.IsValid) {
                gazeOrigin = eyeTrackingData.GazeRay.Origin;
                gazeDirection = eyeTrackingData.GazeRay.Direction;
            }
            Record record = new Record(hmdRot, ctrlRot, gazeOrigin, gazeDirection);
            this.current_trial.addRecord(record);
        }
    }

    private void randomize_condition() {
        int c = Random.Range(0, 1);
        c = 1; // TODO: remove
        if (c == 0) { this.condition = "immediate"; }
        else { this.condition = "delayed"; }
        Debug.Log("Condition: " + this.condition);
    }

    private double minutes_in() {
        return (Util.timestamp() - this.ts_exp_start) / 60;
    }

    void RandomizeTrialOrder() {
        // Shuffle and save
        System.Random rng = new System.Random();
        this.hand_order = Enumerable.Range(0,this.hands.Count).ToList();
        this.hand_order = this.hand_order.OrderBy(a => rng.Next()).ToList();
        // Save order to session
        this.session.data.hand_order = this.hand_order;
    }

    public void BeginExperiment() {
        if (record) {
            StartRecording();
        }
        this.ts_exp_start = Util.timestamp();
        GotoNextTrial();
    }

    public void StartRecording() {
        this.recording = true;
    }

    public void StopRecording() {
        this.recording = false;
    }

    public void GotoNextTrial() {
        this.trial_index += 1;
        bool show_adversary_info = false;
        bool activate = false;
        double mins = this.minutes_in();
        if (!this.adversary_active) {
            if (this.condition == "delayed") {
                // Check to see if we should activate adversary (X minutes passed)
                int real_rounds_in = this.trial_index - this.practice_rounds - 1;
                bool force_adversary = ADVERSARY_FORCE_AFTER_ROUNDS > -1 && real_rounds_in == ADVERSARY_FORCE_AFTER_ROUNDS;
                activate = mins > ADVERSARY_DELAY_MINS || force_adversary;
            } else {
                // Immediate, activate now
                activate = true;
            }
        }
        if (activate) {
            this.adversary_active = true;
            show_adversary_info = true;
        }

        if (mins > EXP_MAX_MINS || this.trial_index > this.hands.Count + this.practice_hands.Count) Finish();
        else {
            if (show_adversary_info) ShowAdversaryInfoThenTrial(); // Calls RunOneTrial
            else RunOneTrial();
        }
    }

    void ShowAdversaryInfoThenTrial() {
        string message = "In all following trials, an adversary\n" +
            "will be tracking your behavior as you decide, and will attempt to predict\n" +
            "which card you are going to select. On each trial, you will earn a point\n" +
            "only when you both make a successful set, and when your selection is\n" +
            "not predicted by the adversary. When ready, click the trigger button\n" +
            "to start the next trial.";
        ui.ShowHUDScreenWithConfirm(message, Color.black, "RunOneTrial");
    }

    void RunOneTrial() {
        Debug.Log("Running trial " + this.trial_index.ToString());
        HandSpec hand;
        bool first_real = false;
        if (this.practice_remaining > 0) {
            // Get a practice hand
            hand = this.practice_hands[practice_remaining - 1];
            this.practice_remaining -= 1;
        } else {
            // Get a real hand
            int hand_index = this.hand_order[this.trial_index - this.practice_rounds - 1];
            hand = this.hands[hand_index];
            if (this.practicing) {
                this.practicing = false;
                first_real = true;
            }
        }
        this.current_trial = new SessionTrial(this.session_id, this.trial_index, hand, this.adversary_active, this.practicing);
        MaybeClearHand();
        DealHand();
        if (this.practicing) {
            int prounds = this.practice_hands.Count;
            ui.ShowHUDScreenWithConfirm(
                string.Format(
                    "This is practice round {0} of {1}. Try selecting a card and bringing it to your hand. Your choice on these rounds wont affect your score. Click your controller trigger to proceed.",
                    prounds - this.practice_remaining,
                    prounds
                ),
                Color.black, "BeginDecisionStage");
        } else {
            if (first_real) {
                // Show message indicating we're starting real trials
                ui.ShowHUDScreenWithConfirm("Great job. Practice rounds finished. All remaining trials are real and will be scored. Click your controller trigger to proceed.",
                DGREEN, "BeginDecisionStage");
            } else {
                // Immediately start decision stage / timer
                BeginDecisionStage();
            }
        }
    }

    public void SubjectSelection(int position) {
        ui.ShowHUDMessage("Trial complete");
        // Score selection and save trial to session
        this.current_trial.StoreResponseAndScore(position);
        this.current_trial.SaveToFile();
        this.current_trial.CleanUpData(); // Deletes large data once saved
        session.AddTrial(this.current_trial);
        if (PICKUP_SECS == -1) {
            // No time limit, waiting for user choice
            // So, now go to next trial after short delay
            Invoke("GotoNextTrial", 2);
        }
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

    void BeginDecisionStage() {
        GetComponent<Camera>().backgroundColor = this.current_trial.adversarial() ? SKY_ADVERSARY : SKY_DEFAULT;
        ui.ShowHUDCountdownMessage(DECISION_SECS, "Decision... ");
        // Show decision prompt
        Invoke("BeginPickupStage", DECISION_SECS);
    }

    void BeginPickupStage() {
        // Await user choice
        // Possibly update "adversary watching" indicator
        this.current_trial.StartSelection();
        GameObject.Find("CardOnTable0").tag = "Grabbable";
        GameObject.Find("CardOnTable1").tag = "Grabbable";
        ui.ClearCountdown();
        GetComponent<Camera>().backgroundColor = SKY_DEFAULT;
        if (PICKUP_SECS > -1) {
            Invoke("GotoNextTrial", PICKUP_SECS);
            ui.ShowHUDCountdownMessage(PICKUP_SECS, "Pick up card");
        } else {
            ui.ShowHUDMessage("Pick up card");
        }
    }

    void MaybeClearHand() {
        for (int i=0; i<this.cards.Count; i++) {
            Destroy(this.cards[i].gameObject);
        }
        this.cards.Clear();
    }

    void DealHand() {
        HandSpec hs = this.current_trial.hand;
        float CARD_LIFT = 0.04f;
        Vector3 table_pos = this.table.localPosition;
        float table_rad = this.table.localScale.z / 2;
        float table_h = this.table.localScale.y;
        // Deal cards to private hand
        for (int i=0; i<2; i++) {
            string card_id = hs.priv[i];
            float x = -0.2f + 2 * CARD_SEP * i;
            float y = this.handholder.position.y;
            float z = this.handholder.position.z;
            float rot = this.handholder.eulerAngles.x - 90.0f;
            Transform newCard = AddCardToScene(card_id, x, y, z);
            newCard.name = "CardInHand" + i.ToString();
            // Kinematic to freeze, and don't need gravity on private cards
            Rigidbody rb = newCard.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            newCard.Rotate(new Vector3(0, 0, rot), Space.Self);
            newCard.Translate(0.02f, 0.004f, 0.0f);
        }
        // Deal cards to table
        for (int i=0; i<2; i++) {
            string card_id = hs.table[i];
            float x = CARD_SEP * (i-0.5f);
            float y = table_pos.y + table_h/2 + CARD_LIFT;
            float z = table_pos.z - table_rad/2;
            Transform newCard = AddCardToScene(card_id, x, y, z);
            newCard.name = "CardOnTable" + i.ToString();
            newCard.GetComponent<CardBehavior>().setPosition(i);
        }
        session.data.hand_specs.Add(hs);
    }

    private Transform AddCardToScene(string id, float x, float y, float z) {
        Transform newCard = Instantiate(card, new Vector3(x, y, z), Quaternion.Euler(0, 90, 90));
        newCard.GetComponent<CardBehavior>().setID(id);
        newCard.SetParent(room.GetComponent<Transform>());
        Util.AddCardPattern(newCard, id);
        this.cards.Add(newCard);
        return newCard;
    }

    void Finish() {
        Debug.Log("Done, saving...");
        session.SaveToFile();
        if (record) StopRecording();
        MaybeClearHand();
        string results = "All trials finished!\n\n";
        results += string.Format("You were successful in {0} of {1} trials.\n\nYour experimenter will help you take off the VR headset.", this.session.data.total_points, this.session.data.total_points_possible);
        ui.ShowHUDScreen(results, Color.green);
    }

}
