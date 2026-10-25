using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RandomItemCoordinator : MonoBehaviour
{
    [SerializeField] private HuntRoundManager round;
    [SerializeField] private RandomItemPickup[] pickups;
    [SerializeField] private Camera[] views = new Camera[3];
    [SerializeField] private Text[] labels = new Text[3];
    [SerializeField] private TrackerTrails trails;
    [SerializeField] private Rect trailBounds = new Rect(-20, -12, 40, 24);
    [SerializeField, Min(0)] private int speedWeight = 40, trackerWeight = 40, decoyWeight = 20;
    [SerializeField, Min(1)] private float speedMultiplier = 1.5f;
    [SerializeField, Min(.1f)] private float speedDuration = 10, decoyDuration = 5;
#if UNITY_EDITOR
    [SerializeField] private bool forceRewardForPlayChecks = false;
    [SerializeField] private RandomItemKind forcedReward = RandomItemKind.Speed;
#endif
    private readonly PlayerItemState[] effects = { new PlayerItemState(), new PlayerItemState(), new PlayerItemState() };
    private readonly MovingDecoy[] decoys = new MovingDecoy[3];
    private readonly TopDownPlayerMovement[] movement = new TopDownPlayerMovement[3];
    private readonly double[] toastUntil = new double[3];
    private readonly RandomItemKind[] toastKind = new RandomItemKind[3];
    private readonly int[] lastSpeed = { -1, -1, -1 }, lastDecoy = { -1, -1, -1 }, lastToast = { -2, -2, -2 };
    private readonly bool[] lastTracker = new bool[3];
    private bool ready;

    public void Configure(HuntRoundManager manager, RandomItemPickup[] items, Camera[] cameras, Text[] itemLabels, TrackerTrails tracking, Rect bounds)
    { round = manager; pickups = items; views = cameras; labels = itemLabels; trails = tracking; trailBounds = bounds; }

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (round == null || round.gameObject != gameObject || pickups == null || pickups.Length == 0 || trails == null || !trails.enabled
            || views == null || views.Length != 3 || labels == null || labels.Length != 3)
        { error = "Assign the round, pickup list, three cameras/item labels, and TrackerTrails."; return false; }
        if (speedWeight < 0 || trackerWeight < 0 || decoyWeight < 0 || (long)speedWeight + decoyWeight <= 0
            || (long)speedWeight + trackerWeight + decoyWeight > int.MaxValue || !Finite(speedMultiplier) || speedMultiplier < 1
            || !Finite(speedDuration) || speedDuration <= 0 || !Finite(decoyDuration) || decoyDuration <= 0
            || !Finite(trailBounds.x) || !Finite(trailBounds.y) || !Finite(trailBounds.width) || !Finite(trailBounds.height) || trailBounds.width <= 0 || trailBounds.height <= 0)
        { error = "Use valid item weights (Speed or Decoy must remain available), finite speeds/durations, and positive trail bounds."; return false; }
        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i] == null || pickups[i].gameObject.scene != gameObject.scene || !pickups[i].enabled || !pickups[i].gameObject.activeInHierarchy
                || !pickups[i].GetComponent<BoxCollider2D>().isTrigger || !pickups[i].GetComponent<BoxCollider2D>().enabled)
            { error = "Each pickup needs a scene-local BoxCollider2D trigger."; return false; }
            var legacy = pickups[i].GetComponent<DisappearOnPlayerTouch2D>();
            if (legacy != null && legacy.enabled) { error = "Disable the old disappearance-only script on item pickups."; return false; }
            for (int j = 0; j < i; j++) if (pickups[j] == pickups[i]) { error = "Pickup references must be distinct."; return false; }
        }
        for (int i = 0; i < 3; i++)
        {
            if (round.GetPlayer(i) == null || views[i] == null || labels[i] == null
                || views[i].GetComponent<LocalPlayerCameraFollow>() == null || views[i].GetComponent<LocalPlayerCameraFollow>().Target != round.GetPlayer(i).transform)
            { error = "Match each player's camera and item label by identity."; return false; }
            for (int j = 0; j < i; j++) if (views[i] == views[j] || labels[i] == labels[j])
            { error = "Player cameras and item labels must be distinct."; return false; }
        }
        return trails.ValidateLayers(out error);
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void Awake()
    {
        if (!ValidateConfiguration(out string error)) { Debug.LogWarning("RandomItemCoordinator: " + error, this); enabled = false; return; }
        trails.Prepare(round, views, trailBounds);
        for (int i = 0; i < 3; i++)
        {
            movement[i] = round.GetPlayer(i).GetComponent<TopDownPlayerMovement>();
            var obj = new GameObject("Player" + (i + 1) + "MovingDecoy", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(MovingDecoy));
            obj.transform.SetParent(transform, false); obj.layer = round.GetPlayer(i).gameObject.layer;
            decoys[i] = obj.GetComponent<MovingDecoy>(); decoys[i].Prepare(round);
            labels[i].text = string.Empty;
        }
        ready = true;
    }

    // Called by the authoritative manager before applying combat movement state.
    public void SyncState()
    {
        if (!ready) return;
        double now = Time.timeAsDouble;
        for (int i = 0; i < 3; i++)
        {
            if (!round.Rules.IsAlive(i) || round.Rules.State == HuntRules.Phase.RoundOver)
            {
                effects[i].Clear(); trails.ClearViewer(i); if (decoys[i].Active) decoys[i].Stop(); toastUntil[i] = 0;
            }
            movement[i].SetItemSpeedMultiplier(effects[i].SpeedActive(now) ? speedMultiplier : 1);
        }
    }

    public void ResolvePickups()
    {
        if (!ready || !enabled || !round.GameplayAvailable || round.Rules.State == HuntRules.Phase.Opening || round.Rules.State == HuntRules.Phase.RoundOver) return;
        foreach (var pickup in pickups)
        {
            if (!pickup.Available) continue;
            int mask = pickup.Occupants(round);
            int owner = RandomItemRules.ChoosePlayer(mask, max => Random.Range(0, max));
            if (owner < 0) continue;
            var reward = RandomItemRules.Choose(effects[owner].Tracker, speedWeight, trackerWeight, decoyWeight, max => Random.Range(0, max));
#if UNITY_EDITOR
            if (forceRewardForPlayChecks && !(forcedReward == RandomItemKind.Tracker && effects[owner].Tracker)) reward = forcedReward;
#endif
            effects[owner].Give(reward, Time.timeAsDouble, speedDuration);
            if (reward == RandomItemKind.Tracker) trails.Activate(owner);
            if (reward == RandomItemKind.Decoy) decoys[owner].Begin(round.GetPlayer(owner), Random.Range(0, 2) == 0 ? 45 : -45, decoyDuration);
            toastKind[owner] = reward; toastUntil[owner] = Time.timeAsDouble + 2;
            movement[owner].SetItemSpeedMultiplier(effects[owner].SpeedActive(Time.timeAsDouble) ? speedMultiplier : 1);
            pickup.Consume();
        }
    }

    private void Update()
    {
        if (!ready || !round.GameplayAvailable) return;
        SyncState();
        if (round.Rules.State != HuntRules.Phase.RoundOver) trails.Sample(Time.timeAsDouble);
        for (int i = 0; i < 3; i++)
        {
            int speed = effects[i].SpeedSeconds(Time.timeAsDouble);
            int decoy = decoys[i].Active ? Mathf.Max(0, Mathf.CeilToInt((float)(decoys[i].Deadline - Time.timeAsDouble))) : 0;
            int toast = Time.timeAsDouble < toastUntil[i] ? (int)toastKind[i] : -1;
            bool tracking = effects[i].Tracker;
            if (lastSpeed[i] == speed && lastDecoy[i] == decoy && lastToast[i] == toast && lastTracker[i] == tracking) continue;
            lastSpeed[i] = speed; lastDecoy[i] = decoy; lastToast[i] = toast; lastTracker[i] = tracking;
            string text = speed > 0 ? "BOOST " + speed + "s" : string.Empty;
            if (tracking) text += (text.Length > 0 ? " | " : "") + "TRACKER";
            if (decoy > 0) text += (text.Length > 0 ? " | " : "") + "DECOY " + decoy + "s";
            if (toast >= 0) text = (toast == 0 ? "SPEED BOOST" : toast == 1 ? "TRACKER - TRAILS LAST THIS ROUND" : "MOVING DECOY") + "\n" + text;
            labels[i].text = text;
        }
    }

    private void OnDisable()
    {
        if (!ready) return;
        for (int i = 0; i < 3; i++)
        {
            effects[i].Clear(); if (movement[i] != null) movement[i].SetItemSpeedMultiplier(1);
            if (decoys[i] != null && decoys[i].Active) decoys[i].Stop();
            if (trails != null) trails.ClearViewer(i);
            if (labels[i] != null) labels[i].text = string.Empty;
        }
    }
}
