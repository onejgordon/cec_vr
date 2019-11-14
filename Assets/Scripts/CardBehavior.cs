using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardBehavior : MonoBehaviour
{
    private Material mat;
    private string card_id; 
    private double last_hit_ts = 0;
    
    Color DEF_COLOR = Color.gray;
    // Start is called before the first frame update
    void Start()
    {
        // mat = GetComponent<Renderer>().material;
        // mat.color = this.defaultColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string getBlockId() {
        return this.block_id;
    }

    public void setID(string id) {
        this.block_id = id;
    }

}
