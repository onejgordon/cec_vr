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
    private Color SKY_ADVERSARY = new Color(200, 50, 50);
    private Color SKY_DEFAULT = new Color(150, 150, 150);
    public bool VISUAL_ADVERSARY_INFO = true;
    public bool QUICK_DEBUG = true;
    public int MAX_TRIALS = 0; // Set to 0 for production. Just for short debug data collection
    private int DECISION_SECS = 10;
    private int PICKUP_SECS = 5; // -1 for infinite time (wait for user choice)
    private int ADVERSARY_ROUNDS_IMMEDIATE = 3; // 3, ~ 1 minute
    private int ADVERSARY_ROUNDS_DELAYED = 15; // 15, ~ 5 minutes 

    public int EXP_MAX_MINS = 25;
    public bool left_handed = false;
    public float CARD_SEP = 0.2f;
    private double ts_exp_start = 0; // Timestamp
    private int trial_index = 0;
    public int practice_rounds = 2;
    private int practice_remaining = 0; 
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
    private ControllerGrab controller_grab;

    private Color DBLUE = new Color(0.0f, 0.0f, 0.6f);
    private Color DGREEN = new Color(0, 0.6f, 0, 1);

    // Start is called before the first frame update
    void Start()
    {
        if (QUICK_DEBUG) {
            DECISION_SECS = 5;
            practice_rounds = 1;
            ADVERSARY_ROUNDS_IMMEDIATE = 1;
            ADVERSARY_ROUNDS_DELAYED = 3;
            MAX_TRIALS = 5;
            this.session_id = "DEBUG";
        } else {
            this.session_id = ((int)Util.timestamp()).ToString();   
        }
        this.session.data.session_id = this.session_id;
        this.session.data.left_handed = this.left_handed;
        this.practice_remaining = this.practice_rounds;
        this.ui = GameObject.Find("UICanvas").GetComponent<UIBehavior>();
        this.hmdCamera = GameObject.Find("Camera").GetComponent<Transform>();
        this.controller = GameObject.FindGameObjectWithTag("Controller").GetComponent<Transform>();
        this.controller_grab = this.controller.GetComponent<ControllerGrab>();
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
            Debug.Log(string.Format("Loaded {0} hands from {1}", this.hands.Count, path));
        } else Debug.Log(string.Format("{0} doesn't exist", path));
        TobiiXR.Start();
        this.randomize_condition();
        this.BeginExperiment();
    }

    // Update is called once per frame
    void Update()
    {
        if (recording && this.current_trial != null) {
            Quaternion hmdRot = hmdCamera.rotation;
            Quaternion ctrlRot = controller.rotation;
            Vector3 gazeOrigin = new Vector3();
            Vector3 gazeDirection = new Vector3();
            float convDistance = -1.0f; // Default when not valid
            bool eitherEyeClosed = false;
            var eyeTrackingData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);
            if (eyeTrackingData.GazeRay.IsValid) {
                gazeOrigin = eyeTrackingData.GazeRay.Origin;
                gazeDirection = eyeTrackingData.GazeRay.Direction;
            }
            eitherEyeClosed = eyeTrackingData.IsLeftEyeBlinking || eyeTrackingData.IsRightEyeBlinking;
            if (eyeTrackingData.ConvergenceDistanceIsValid) {
                convDistance = eyeTrackingData.ConvergenceDistance;
            }
            Record record = new Record(hmdRot, ctrlRot, gazeOrigin, gazeDirection, convDistance, eitherEyeClosed);
            this.current_trial.addRecord(record);
        }
    }

    public void Calibrate() {
        
    }

    private int non_adversary_rounds() {
        if (this.condition == "delayed") return ADVERSARY_ROUNDS_DELAYED;
        else return ADVERSARY_ROUNDS_IMMEDIATE;
    }

    private void randomize_condition() {
        int c = Random.Range(0, 1);
        if (c == 0) { this.condition = "immediate"; }
        else { this.condition = "delayed"; }
        Debug.Log("Condition: " + this.condition);
        this.session.data.condition = this.condition;
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
            this.recording = true;
        }
        this.ts_exp_start = Util.timestamp();
        this.session.data.ts_session_start = this.ts_exp_start;
        GotoNextTrial();
    }

    public void GotoNextTrial() {
        this.trial_index += 1;
        bool show_adversary_info = false;
        bool activate_adversary = false;
        double mins = this.minutes_in();
        if (!this.adversary_active) {
            // Check to see if we should activate adversary
            int real_rounds_in = this.trial_index - this.practice_rounds - 1;
            activate_adversary = real_rounds_in == this.non_adversary_rounds();
        }
        if (activate_adversary) {
            this.adversary_active = true;
            show_adversary_info = true;
            this.session.data.ts_adversary = Util.timestamp();
        }

        if ((MAX_TRIALS != 0 && this.trial_index > MAX_TRIALS) || mins > EXP_MAX_MINS || this.trial_index > this.hands.Count + this.practice_hands.Count) Finish();
        else {
            if (show_adversary_info) ShowAdversaryInfoThenTrial(); // Calls RunOneTrial
            else RunOneTrial();
        }
    }

    void ShowAdversaryInfoThenTrial() {
        if (VISUAL_ADVERSARY_INFO) {
            ui.ShowHUDImageWithDelayedConfirm("Images/adversary", "RunOneTrial");
        } else {
            // string message = "In all following trials, an adversary\n" +
            //     "will be tracking your behavior as you decide, and will attempt to predict\n" +
            //     "which card you are going to select. On each trial, you will earn a point\n" +
            //     "only when you both make a successful set, and when your selection is\n" +
            //     "not predicted by the adversary. When ready, click the trigger button\n" +
            //     "to start the next trial.";
            string message = "You will now move to the next phase. Your experimenter will help you take off the headset.";
            ui.ShowHUDScreenWithDelayedConfirm(message, Color.black, "RunOneTrial");
        }
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
                    "This is practice round {0} of {1}. After the decision phase, try selecting a card and bringing it to your hand. Your choice on these rounds wont affect your score. Click your controller trigger to proceed.",
                    prounds - this.practice_remaining,
                    prounds
                ),
                Color.black, "BeginDecisionStage");
        } else {
            if (first_real) {
                // Show message indicating we're starting real trials
                ui.ShowHUDScreenWithConfirm("Great job. Practice rounds finished. All remaining trials are real and will be scored. Click your controller trigger to proceed.",
                DBLUE, "BeginDecisionStage");
            } else {
                // Immediately start decision stage / timer
                BeginDecisionStage();
            }
        }
    }

    public void FinishTrial() {
        // Score selection and save trial to session
        ui.ClearCountdown();
        ui.ShowHUDMessage("Trial complete");
        
        this.current_trial.SaveToFile();
        this.current_trial.CleanUpData(); // Deletes large data once saved
        session.AddTrial(this.current_trial);
        this.current_trial = null;
        if (PICKUP_SECS != -1) {
            // Time limit, clear timer to avoid double GoTo
            CancelInvoke();
        }
        Invoke("GotoNextTrial", 2);
    }

    public void SubjectSelection(int position) {
        if (this.current_trial != null) {
            this.current_trial.StoreResponseAndScore(position);
            this.FinishTrial();
        } else {
            // Trial already finished, waiting for GotoNextTrial
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
        ui.ShowHUDCountdownMessage(DECISION_SECS, "Decision (don't pick up yet)... ");
        // Show decision prompt
        Invoke("BeginPickupStage", DECISION_SECS);
    }

    void BeginPickupStage() {
        // Await user choice
        this.current_trial.StartSelection();
        GameObject.Find("CardOnTable0").tag = "Grabbable";
        GameObject.Find("CardOnTable1").tag = "Grabbable";
        ui.ClearCountdown();
        GetComponent<Camera>().backgroundColor = SKY_DEFAULT;
        if (PICKUP_SECS > -1) {
            Invoke("FinishTrial", PICKUP_SECS);
            ui.ShowHUDCountdownMessage(PICKUP_SECS, "Pick up card");
        } else {
            ui.ShowHUDMessage("Pick up card");
        }
    }

    void MaybeClearHand() {
        this.controller_grab.ResetState();
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
        session.data.ts_session_end = Util.timestamp();
        session.SaveToFile();
        if (this.recording) this.recording = false;
        MaybeClearHand();
        string results = "All trials finished!\n\n";
        double percent = this.session.data.total_points / this.session.data.total_points_possible;
        int dollars = 0;
        if (percent >= .25 && percent < .4) dollars = 1;
        else if (percent >= .4 && percent < .55) dollars = 2;
        else if (percent >= .55 && percent < .70) dollars = 3;
        else if (percent >= .7 && percent < .85) dollars = 4;
        else if (percent >= .85) dollars = 5;
        results += string.Format("You correctly matched in {0} of {1} trials.\n Of those, you avoided prediction {2} times.\nYour final success rate is {3:0.0}% (${4} bonus).\n\nYour experimenter will help you take off the VR headset.", 
            this.session.data.total_matches,
            this.session.data.total_points_possible, 
            this.session.data.total_points,
            100.0 * percent,
            dollars
            );
        ui.ShowHUDScreen(results, DGREEN);
        Debug.Log(">>>>>> Subject Bonus: $" + dollars.ToString());
        TobiiXR.Stop();
    }

}
