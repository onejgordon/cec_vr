using System.Collections;
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

    public int N_TRIALS = 5; 
    public int MAX_TRIALS = 1; // Set to 0 for production. Just for short debug data collection
    public int SELECTION_SECS = -1; // -1 for infinite time (wait for user choice)
    public int DECISION_SECS = 10; 
    public int ADVERSARY_DELAY_MINS = 10;
    public int EXP_MAX_MINS = 25;
    public float CARD_SEP = 0.2f;
    private double ts_exp_start = 0; // Timestamp
    private int trial_index = 0;
    private int practice_remaining = 2; // 0 to disable practice
    private SessionTrial current_trial;
    public Transform card;
    public SessionSaver session;
    public SteamVR_LaserPointer laserPointer;
    private List<HandSpec> hands;
    private List<HandSpec> practice_hands;
    public bool record = false;
    private List<Transform> cards = new List<Transform>();
    private List<int> hand_order = new List<int>();
    private string condition = null; // "immediate" or "delayed"
    private bool adversary_active = false;
    private bool practicing = false;
    public Transform handholder;
    public Transform table;
    public UIBehavior ui;
    public GameObject room;

    private Color DGREEN = new Color(0.0f, 0.0f, 0.6f);

    

    // Start is called before the first frame update
    void Start()
    {
        this.ui = GameObject.Find("UICanvas").GetComponent<UIBehavior>();
        this.hands = new List<HandSpec>();
        string path = "./TrialData/hands.json";
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
        if (record) recording = GetComponent<RecordingController>();
        this.ShowHideLaser(false);
        this.randomize_condition();
        this.BeginExperiment();
    }

    // Update is called once per frame
    void Update()
    {

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
            recording.StartRecording();
        }
        this.ts_exp_start = Util.timestamp();
        GotoNextTrial();
    }

    public void GotoNextTrial() {
        this.trial_index += 1;
        bool show_adversary_info = false;
        bool activate = false;
        double mins = this.minutes_in();
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
        
        if (mins > EXP_MAX_MINS || this.trial_index > this.hand_order.Count) Finish();
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
            hand = this.practice_hands[practice_remaining - 1];
            this.practice_remaining -= 1;
        } else {
            int hand_index = this.hand_order[this.trial_index - 1];
            hand = this.hands[hand_index];
            if (this.practicing) {
                this.practicing = false;
                first_real = true;
            }
        }
        this.current_trial = new SessionTrial(this.trial_index, hand, this.adversary_active, this.practicing);
        GetComponent<Camera>().backgroundColor = SKY_DEFAULT;
        MaybeClearHand();
        DealHand();
        if (this.practicing) {
            int prounds = this.practice_hands.Count;
            ui.ShowHUDScreenWithConfirm(
                string.Format(
                    "This is practice round {0} of {1}. Click your controller trigger to proceed.",
                    prounds - this.practice_remaining,
                    prounds
                ),
                Color.black, "BeginDecisionStage");
        } else {
            if (first_real) {
                // Show message indicating we're starting real trials
                ui.ShowHUDScreenWithConfirm("Great job. Practice rounds finished. Remaining trials are real. Click your controller trigger to proceed.", 
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
        
        session.AddTrial(this.current_trial);
        if (SELECTION_SECS == -1) {
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

    public void RecordHit(string hitKey, RaycastHit hit, float conf) {
        // Debug.Log("Hit " + hitKey);
        Vector3 hitpoint = hit.point;
        if (this.current_trial != null) {
            this.current_trial.addHit(hitKey, hitpoint, GetComponent<Camera>().transform.rotation, conf);
        }
    }


    void BeginDecisionStage() {
        ui.ShowHUDCountdownMessage(DECISION_SECS, "Decide on card (but do not yet select)");
        // Show decision prompt
        Invoke("BeginSelectionStage", DECISION_SECS);
    }

    void BeginSelectionStage() {
        // Await user choice
        // Possibly update "adversary watching" indicator
        // Make table cards grabbable
        GameObject.Find("CardOnTable0").tag = "Grabbable";
        GameObject.Find("CardOnTable1").tag = "Grabbable";
        ui.ClearCountdown();
        GetComponent<Camera>().backgroundColor = SKY_RUNNING;
        if (SELECTION_SECS > -1) {
            Invoke("GotoNextTrial", SELECTION_SECS);
            ui.ShowHUDCountdownMessage(SELECTION_SECS, "Select your chosen card");
        } else {
            ui.ShowHUDMessage("Select your chosen card");
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
            float x = CARD_SEP * (i-1);
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
            float x = -CARD_SEP/2 + CARD_SEP * i;
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
        if (record) recording.StopRecording();
        MaybeClearHand();
        string results = "All trials finished!\n\n";
        results += string.Format("You were successful in {0} of {1} trials.", this.session.data.total_points, this.session.CountTrials());
        ui.ShowHUDScreen(results, Color.green);
        GameObject.Find("Room").SetActive(false);
    }

    // -------- Not in Use

    void ShowHideLaser(bool show) {
        float alpha = show ? 1 : 0;
        if (this.laserPointer != null) {
            this.laserPointer.color.a = alpha;
        }
    }

}
