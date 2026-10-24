using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public sealed class HuntRoundManager : MonoBehaviour
{
    [SerializeField] private CameraIntroduction introduction;
    [SerializeField] private PlayerCombatant[] players = new PlayerCombatant[3];
    [SerializeField] private Text[] statusLabels = new Text[3];
    [SerializeField, Min(1f)] private float apexSpeedMultiplier = 1.35f;
    private readonly string[] identityHex = new string[3];
    private int pendingContacts;
    private int touchingContacts;
    private bool failed;
    private bool verificationSuspended;
    public HuntRules Rules { get; private set; }

    public void Configure(CameraIntroduction intro, PlayerCombatant[] characters, Text[] labels)
    { introduction = intro; players = characters; statusLabels = labels; }

    private void Awake()
    {
        Rules = new HuntRules();
        if (!ValidateConfiguration(out string error)) { Fail(error); return; }
        for (int i = 0; i < 3; i++)
        {
            identityHex[i] = ColorUtility.ToHtmlStringRGB(players[i].GetComponent<SpriteRenderer>().color);
            statusLabels[i].supportRichText = true;
            statusLabels[i].color = Color.white;
        }
        ApplyState();
    }

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (apexSpeedMultiplier < 1f || float.IsNaN(apexSpeedMultiplier) || float.IsInfinity(apexSpeedMultiplier))
        { error = "Apex Speed Multiplier must be finite and at least 1."; return false; }
        if (introduction == null || players == null || players.Length != 3 || statusLabels == null || statusLabels.Length != 3)
        { error = "Assign the introduction and exactly three combatants/status labels."; return false; }
        for (int i = 0; i < 3; i++)
        {
            if (players[i] == null || !players[i].gameObject.activeInHierarchy || !players[i].IsConfigured(this, i) || statusLabels[i] == null)
            { error = "Combatant " + (i + 1) + " needs matching ID, manager, movement, solid collider, renderer, camera, aura, and label."; return false; }
            for (int j = 0; j < i; j++) if (players[i] == players[j] || statusLabels[i] == statusLabels[j])
            { error = "Combatants and labels must be distinct."; return false; }
        }
        return true;
    }

    public void ReportContact(PlayerCombatant a, PlayerCombatant b)
    {
        if (failed || verificationSuspended || Rules == null || Rules.State == HuntRules.Phase.RoundOver) return;
        if (a.PlayerId < 0 || a.PlayerId >= 3 || b.PlayerId < 0 || b.PlayerId >= 3
            || players[a.PlayerId] != a || players[b.PlayerId] != b) return;
        int bit = HuntRules.ContactBit(a.PlayerId, b.PlayerId);
        touchingContacts |= bit;
        if (Rules.State == HuntRules.Phase.TriangleHunt || Rules.State == HuntRules.Phase.FinalChase) pendingContacts |= bit;
    }

    public void EndContact(PlayerCombatant a, PlayerCombatant b)
    { touchingContacts &= ~HuntRules.ContactBit(a.PlayerId, b.PlayerId); }

    private void Update()
    {
        if (failed || verificationSuspended) return;
        StartWhenReady();
        UpdateLabels();
    }

    private void FixedUpdate()
    {
        if (failed || verificationSuspended) return;
        StartWhenReady();
        if (failed) return;
        // Remember continuing pairs even when sleeping bodies stop producing stay callbacks.
        int contacts = pendingContacts | touchingContacts;
        pendingContacts = 0;
        if (Rules.State == HuntRules.Phase.Transforming)
            Rules.AdvanceTransformation(Time.fixedDeltaTime);
        else
            Rules.ResolveContacts(contacts);
        ApplyState();
    }

    private void StartWhenReady()
    {
        if (introduction == null || introduction.CurrentStage == CameraIntroduction.Stage.Cancelled)
        { Fail("Camera introduction is missing or cancelled; combat has not started."); return; }
        if (Rules.State == HuntRules.Phase.Opening && introduction.CurrentStage == CameraIntroduction.Stage.Complete)
        { Rules.Begin(); pendingContacts = 0; ApplyState(); }
    }

    private void ApplyState()
    {
        for (int i = 0; i < 3; i++)
        {
            bool apex = Rules.ApexId == i;
            bool freeze = Rules.State == HuntRules.Phase.RoundOver || (Rules.State == HuntRules.Phase.Transforming && apex);
            bool canMove = Rules.State != HuntRules.Phase.Opening && !freeze;
            players[i].ApplyState(Rules.IsAlive(i), canMove, freeze, apex, apexSpeedMultiplier);
            if (Rules.State == HuntRules.Phase.RoundOver)
                players[i].HoldCamera();
        }
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        for (int i = 0; i < 3; i++)
        {
            statusLabels[i].text = HuntPresentation.Status(Rules, i, identityHex);
        }
    }

    private void Fail(string error)
    {
        if (failed) return;
        failed = true;
        // No input lock can be accidentally released by the opening's later handoff.
        if (players != null) foreach (var player in players) if (player != null)
            player.ApplyState(true, false, true, false);
        Debug.LogError("HuntRoundManager: " + error, this);
    }

    // Only used by the explicit editor physics checks; prevents their teleports starting combat.
    public bool SetVerificationSuspended(bool value)
    { bool old = verificationSuspended; verificationSuspended = value; pendingContacts = touchingContacts = 0; return old; }
}
