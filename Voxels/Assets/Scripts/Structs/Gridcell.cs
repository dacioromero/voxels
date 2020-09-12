using UnityEngine;

// Adapted from http://paulbourke.net/geometry/polygonise/, retreived in April 2018

namespace MarchingCubes
{
  [System.Serializable]//, Unity.Burst.BurstCompile]
  public struct Gridcell
  {
    public static Gridcell one = new Gridcell()
    {
      val1 = 1,
      val2 = 1,
      val3 = 1,
      val4 = 1,
      val5 = 1,
      val6 = 1,
      val7 = 1,
      val8 = 1,
    };

    public static Gridcell zero = new Gridcell()
    {
      val1 = 0,
      val2 = 0,
      val3 = 0,
      val4 = 0,
      val5 = 0,
      val6 = 0,
      val7 = 0,
      val8 = 0,
    };

    public static Vector3Int p1 = new Vector3Int(0, 0, 0);
    public static Vector3Int p2 = new Vector3Int(0, 0, 1);
    public static Vector3Int p3 = new Vector3Int(1, 0, 1);
    public static Vector3Int p4 = new Vector3Int(1, 0, 0);
    public static Vector3Int p5 = new Vector3Int(0, 1, 0);
    public static Vector3Int p6 = new Vector3Int(0, 1, 1);
    public static Vector3Int p7 = new Vector3Int(1, 1, 1);
    public static Vector3Int p8 = new Vector3Int(1, 1, 0);

    public float val1;
    public float val2;
    public float val3;
    public float val4;
    public float val5;
    public float val6;
    public float val7;
    public float val8;

    public float[] Vals { get => new float[] { val1, val2, val3, val4, val5, val6, val7, val8 }; }

    public bool Equals(Gridcell g) => val1 == g.val1 &&
                                      val2 == g.val2 &&
                                      val3 == g.val3 &&
                                      val4 == g.val4 &&
                                      val5 == g.val5 &&
                                      val6 == g.val6 &&
                                      val7 == g.val7 &&
                                      val8 == g.val8;
  }
}
