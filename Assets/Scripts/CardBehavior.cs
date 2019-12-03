using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardBehavior : MonoBehaviour
{
    private Material mat;
    private string card_id; 


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string getBlockId() {
        return this.card_id;
    }

    public void setID(string id) {
        this.card_id = id;
    }

}
