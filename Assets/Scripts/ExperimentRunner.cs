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
    public bool LOAD_TOWERS = true;
    public int N_BLOCKS = 10;
    const float MIN_HEIGHT = 1f;
    const float MAX_HEIGHT = 3f;
    const float MIN_WIDTH = 2f;
    const float MAX_WIDTH = 11f;
    const float MIN_OFFSET = -2.5f;
    const float MAX_OFFSET = 2.5f;
    public int N_TRIALS = 5; // Towers to run
    public int MAX_TRIALS = 1; // Set to 0 for production. Just for short debug data collection
    public int PHYSICS_SECS = 5;
    private int trial_index = 0;
    private SessionTrial current_trial;
    public Transform block;
    public SessionSaver session;
    public Canvas UIcanvas;
    public SteamVR_LaserPointer laserPointer;
    public Canvas hmdcanvas;
    private List<TowerSpec> towers;
    private bool buttons_showing = false;
    public bool record = false;
    private List<Transform> blocks = new List<Transform>();
    private List<int> tower_order = new List<int>();
    

    // Start is called before the first frame update
    void Start()
    {
        this.towers = new List<TowerSpec>();
        if (LOAD_TOWERS) {
            string path = "./TrialData/towers.json";
            if(File.Exists(path))
            {
                string dataAsJson = File.ReadAllText(path);
                this.towers = JsonUtility.FromJson<AllTowerSpecs>(dataAsJson).towers;
                this.RandomizeTrialOrder();
                N_TRIALS = this.towers.Count;
                Debug.Log(string.Format("Loaded {0} towers from {1}", this.towers.Count, path));
            } else Debug.Log(string.Format("{0} doesn't exist", path));
        } else {
            GenerateRandomTowerSpecs(); // Stores in this.towers
        }
        if (record) recording = GetComponent<RecordingController>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void RandomizeTrialOrder() {
        // Shuffle and save
        System.Random rng = new System.Random();
        this.tower_order = Enumerable.Range(0,this.towers.Count).ToList();
        this.tower_order = this.tower_order.OrderBy(a => rng.Next()).ToList();
        // Save order to session
        this.session.data.tower_order = this.tower_order;
    }

    void GenerateRandomTowerSpecs() {
        for (int i=0; i<N_TRIALS; i++) {
            TowerSpec tower = new TowerSpec();
            for (int j=0; j<N_BLOCKS; j++) {
                float x_offset = Random.Range(MIN_OFFSET, MAX_OFFSET);
                float width = Random.Range(MIN_WIDTH, MAX_WIDTH);
                float height = Random.Range(MIN_HEIGHT, MAX_HEIGHT);
                tower.blocks.Add(new BlockSpec(x_offset, width, height));
            }
            this.towers.Add(tower);
        }
    }

    public void BeginExperiment() {
        if (record) {
            recording.StartRecording();
        }
        BeginNextTrial();
    }

    public void BeginNextTrial() {
        this.trial_index += 1;
        this.current_trial = new SessionTrial(this.trial_index);
        GetComponent<Camera>().backgroundColor = SKY_DEFAULT;
        Debug.Log("Running experiment " + this.trial_index.ToString());
        if (this.trial_index > N_TRIALS || (MAX_TRIALS != 0 && this.trial_index > MAX_TRIALS)) Finish();
        else RunOneTrial();
    }


    void RunOneTrial() {
        MaybeClearTower();
        int tower_index = this.tower_order[this.trial_index - 1];
        AddTowerToTrial(this.towers[tower_index]);
        // Immediately show buttons
        DecisionPrompt();
    }

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

    void DecisionPrompt() {
        // Show decision prompt, run physics upon selection
        ShowButtons();
    }

    public void UserDecisionFall(string direction) {
        if (this.buttons_showing) {
            HideButtons();
            current_trial.StoreResponse("fall-" + direction);
            session.AddTrialResult(current_trial);
            RunPhysics();
        }
    }

    public void UserDecisionFallLeft() {
        this.UserDecisionFall("left");
    }

    public void UserDecisionFallRight() {
        this.UserDecisionFall("right");
    }

    public void UserDecisionNotFall() {
        if (this.buttons_showing) {
            HideButtons();
            current_trial.StoreResponse("not-fall");
            session.AddTrialResult(current_trial);
            RunPhysics();
        }
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

    void RunPhysics() {
        // Change sky color to indicate sim running
        GetComponent<Camera>().backgroundColor = SKY_RUNNING;
        for (int i=0; i<this.blocks.Count; i++) {
            this.blocks[i].GetComponent<Rigidbody>().useGravity = true;
            this.blocks[i].GetComponent<Rigidbody>().isKinematic = false;
        }
        Invoke("BeginNextTrial", PHYSICS_SECS);
    }

    void MaybeClearTower() {
        for (int i=0; i<this.blocks.Count; i++) {
            Destroy(this.blocks[i].gameObject);
        }
        this.blocks.Clear();
    }

    void AddTowerToTrial(TowerSpec ts) {
        float last_bottom = 0f;
        for (int i=0; i<ts.blocks.Count; i++) {
            BlockSpec bs = ts.blocks[i];
            bool even = i % 2 == 0;
            Transform newBlock = AddBlockToTrial(i, bs.x, last_bottom + bs.h/2, bs.w, bs.h, even);
            last_bottom = newBlock.position.y + bs.h/2;
            if (!LOAD_TOWERS) {
                // Add to session saver (just needed when generating new tower specs)
                this.current_trial.addBlock(bs.x, bs.w, bs.h);
            }
        }
        session.data.tower_specs.Add(ts);
    }

    private Transform AddBlockToTrial(int id, float x, float y, float w, float h, bool even) {
        Transform newBlock = Instantiate(block, new Vector3(x, y, 0), Quaternion.identity);
        newBlock.localScale = new Vector3(w, h, 1);
        newBlock.GetComponent<BlockBehavior>().setID(id);
        // TODO: Test if necessary to produce gaze target events
        BoxCollider collider = newBlock.gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        this.blocks.Add(newBlock);
        return newBlock;
    }

    void Finish() {
        Debug.Log("Done, saving...");
        session.SaveToFile();
        if (record) recording.StopRecording();
        MaybeClearTower();
        CanvasGroup cg = this.hmdcanvas.GetComponent<CanvasGroup>();
        cg.alpha = 1;
    }
}
