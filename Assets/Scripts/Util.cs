using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

 public static class Util
 {
   public static double timestamp()
   {
     // In seconds
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        return (System.DateTime.UtcNow - epochStart).TotalMilliseconds / 1000.0;
   }

   public static void AddCardPattern(Transform card, string pattern) {
      Transform face = card.GetChild(0);
      SetImage(face.gameObject, "Images/" + pattern);
   }

   public static void SetImage(GameObject target, string image_name) {
     Sprite s = Resources.Load<Sprite>(image_name);
     target.GetComponent<SpriteRenderer>().sprite = s;
   }

  // public static void SetImage(Image target, string image_name) {
  //    Sprite s = Resources.Load<Sprite>(image_name);
  //    target.GetComponent<Image>().sprite = s;
  //  }
 }