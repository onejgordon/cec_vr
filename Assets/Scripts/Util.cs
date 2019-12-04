using System.Collections;
using System.Collections.Generic;
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
      Sprite face_sprite = Resources.Load<Sprite>("Images/" + pattern);
      Transform face = card.GetChild(0);
		  face.GetComponent<SpriteRenderer>().sprite = face_sprite;
   }
 }