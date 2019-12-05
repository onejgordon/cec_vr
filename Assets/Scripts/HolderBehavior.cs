using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HolderBehavior : MonoBehaviour
{
    private Material back;
    private Material support;
    
    void Start()
    {
        back = gameObject.transform.GetChild(0).GetComponent<Renderer>().material;
        support = gameObject.transform.GetChild(1).GetComponent<Renderer>().material;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setHighlight(bool on) {
        Color c = on ? Color.blue : Color.gray;
        back.color = c;
        support.color = c;
    }
}
