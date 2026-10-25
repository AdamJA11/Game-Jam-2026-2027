using UnityEditor;
using UnityEngine;

public static class HuntAudioChecks
{
    [MenuItem("Game Jam/Check Action Sound Rules")]
    public static void Check() { Debug.Log("ACTION_SOUND_RULES_OK: " + HuntAudioTests.Run() + " assertions. Listening and clip assignments need Play-mode checks."); }
}
