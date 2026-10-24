using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CameraIntroductionCheck
{
    private const string ArmedKey = "GameJam.Phase3bCheckArmed";
    private static CameraIntroduction intro;
    private static LocalPlayerCameraFollow[] follows;
    private static TopDownPlayerMovement[] players;
    private static Vector3[] startingPositions;
    private static bool[] movementEnabled;
    private static float[] previousSizes;
    private static bool sawOverview;
    private static bool sawApproach;

    static CameraIntroductionCheck()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
    }

    [MenuItem("Game Jam/Arm Phase 3b Check for Next Play")]
    public static void Arm()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("Stop Play mode, arm the check, then press Play."); return; }
        SessionState.SetBool(ArmedKey, true);
        Debug.Log("PHASE3B_CHECK_ARMED: press Play without touching movement keys. The check stops Play when finished.");
    }

    private static void OnPlayState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        { EditorApplication.update -= Tick; return; }
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ArmedKey, false)) return;
        SessionState.SetBool(ArmedKey, false);
        try
        {
            intro = UnityEngine.Object.FindObjectOfType<CameraIntroduction>();
            Require(intro != null && intro.IsRunning, "Early introduction Awake accepts configured cameras before their OnEnable; introduction must not be cancelled.");
            Require(intro.ValidateConfiguration(out string configurationError), configurationError);
            follows = UnityEngine.Object.FindObjectsOfType<LocalPlayerCameraFollow>().OrderBy(f => f.name).ToArray();
            Require(follows.Length == 3, "Three follow cameras.");
            players = follows.Select(f => f.Target.GetComponent<TopDownPlayerMovement>()).ToArray();
            startingPositions = players.Select(p => p.transform.position).ToArray();
            movementEnabled = players.Select(p => p.enabled).ToArray();
            previousSizes = follows.Select(f => f.GetComponent<Camera>().orthographicSize).ToArray();
            Require(follows.All(f => f.AutomaticFollowSuspended) && players.All(p => p.InputBlocked),
                "Startup validation succeeds and acquires camera/input ownership.");
            sawOverview = sawApproach = false;
            RandomSpawnsCheck.ValidateCurrent(false);
            Require(Mathf.Abs(CameraIntroduction.CalculateOverviewSize(new Rect(-21, -13, 42, 26), 16f / 9f, .5f) - 13.5f) < .001f, "16:9 overview framing.");
            Require(Mathf.Abs(CameraIntroduction.CalculateOverviewSize(new Rect(-21, -13, 42, 26), 16f / 10f, .5f) - 13.625f) < .001f, "16:10 overview framing.");
            EditorApplication.update += Tick;
            Tick();
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Tick()
    {
        try
        {
            Require(intro != null && intro.CurrentStage != CameraIntroduction.Stage.Cancelled, "Introduction stays valid.");
            if (intro.IsRunning)
            {
                sawOverview |= intro.CurrentStage == CameraIntroduction.Stage.Overview;
                sawApproach |= intro.CurrentStage == CameraIntroduction.Stage.Approach;
                for (int i = 0; i < 3; i++)
                {
                    Require(players[i].InputBlocked && players[i].enabled == movementEnabled[i], "Input gated without toggling movement component.");
                    Require(Vector3.Distance(players[i].transform.position, startingPositions[i]) < .01f
                        && players[i].GetComponent<Rigidbody2D>().velocity.sqrMagnitude < .0001f, "Players remain at their shuffled spawns.");
                    var camera = follows[i].GetComponent<Camera>();
                    Require(follows[i].AutomaticFollowSuspended, "Follow does not compete with introduction.");
                    Require(camera.orthographicSize <= previousSizes[i] + .01f && camera.orthographicSize >= intro.NormalSize, "Zoom approaches close size without overshoot.");
                    previousSizes[i] = camera.orthographicSize;
                    if (intro.CurrentStage == CameraIntroduction.Stage.Overview)
                        foreach (var position in startingPositions)
                        {
                            var point = camera.WorldToViewportPoint(position);
                            Require(point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1, "Wide view includes all spawn assignments.");
                        }
                }
                Require(intro.Elapsed <= intro.OverviewHold + intro.ApproachDuration + 2f, "Opening finishes on time.");
                return;
            }
            Require(intro.CurrentStage == CameraIntroduction.Stage.Complete && sawOverview && sawApproach, "Overview and approach complete.");
            for (int i = 0; i < 3; i++)
            {
                var camera = follows[i].GetComponent<Camera>();
                Require(!players[i].InputBlocked && players[i].enabled == movementEnabled[i], "Input is released without enabling disabled components.");
                Require(!follows[i].AutomaticFollowSuspended && Mathf.Abs(camera.orthographicSize - intro.NormalSize) < .001f, "Normal follow and zoom restored.");
                Require(Vector3.Distance(camera.transform.position, follows[i].GetFollowPosition(intro.NormalSize, camera.aspect)) < .01f, "Exact gameplay camera pose.");
            }
            intro.enabled = false; intro.enabled = true;
            Require(intro.CurrentStage == CameraIntroduction.Stage.Complete, "Re-enabling does not replay.");
            EditorApplication.update -= Tick;
            Debug.Log("PHASE3B_CHECK_OK: automatic overview, gated movement, smooth approach, exact handoff, original component states, framing, no replay. Running gameplay regression checks.");
            SplitScreenCheck.Run();
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Fail(Exception error)
    {
        EditorApplication.update -= Tick;
        Debug.LogException(error);
        EditorApplication.isPlaying = false;
    }

    private static void Require(bool value, string description)
    {
        if (!value) throw new InvalidOperationException("PHASE3B_CHECK_FAILED: " + description);
    }
}
