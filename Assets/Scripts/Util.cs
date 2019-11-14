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
 }