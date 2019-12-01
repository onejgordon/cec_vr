using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.Extras;

public class LaserPointerHandler : MonoBehaviour
{
public bool selected;
public SteamVR_LaserPointer laserPointer;
public ExperimentRunner expScript;

 void Start()
 {
     laserPointer.PointerIn += PointerInside;
     laserPointer.PointerOut += PointerOutside;
     laserPointer.PointerClick += PointerClick;
     selected = false;
 }
 // Update is called once per frame
 void Update()
 {
     
 }

 public void PointerClick(object sender, PointerEventArgs e) {
    // bool fall = e.target.name == "FallRightButton" || e.target.name == "FallLeftButton";
    // bool notfall = e.target.name == "NotFallButton";
    // if (fall) {
    //     if (e.target.name == "FallRightButton") expScript.UserDecisionFallRight();
    //     else expScript.UserDecisionFallLeft();
    // }
    // else if (notfall) expScript.UserDecisionNotFall();
    // else Debug.Log("Do nothing");
 }

 public void PointerInside(object sender, PointerEventArgs e)
 {
     if (e.target.name == this.gameObject.name && selected==false)
     {
        selected = true;
        Debug.Log("pointer is inside this object" + e.target.name);
        
        
     }        
 }
 public void PointerOutside(object sender, PointerEventArgs e)
 {
     
     if (e.target.name == this.gameObject.name && selected == true)
     {
         selected = false;
         Debug.Log("pointer is outside this object" + e.target.name);
     }
 }
 public bool get_selected_value()
 {
     return selected;
 }
}
