using UnityEditor;
using UnityEngine;

public static class Phase3FeatureChecks
{
    [MenuItem("Game Jam/Check Phase 3c Rules")]
    public static void Check3c() { Debug.Log("PHASE3C_RULES_OK: " + Phase3cTests.Run() + " assertions."); }
    [MenuItem("Game Jam/Check Phase 3d Rules")]
    public static void Check3d() { Debug.Log("PHASE3D_RULES_OK: " + Phase3dTests.Run() + " assertions."); }
    [MenuItem("Game Jam/Check Phase 3f Rules")]
    public static void Check3f() { Debug.Log("PHASE3F_RULES_OK: " + Phase3fTests.Run() + " assertions. Render/physics checks are manual."); }
}
