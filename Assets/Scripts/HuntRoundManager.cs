using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public sealed class HuntRoundManager : MonoBehaviour
{
    [SerializeField] private CameraIntroduction introduction;
    [SerializeField] private PlayerCombatant[] players = new PlayerCombatant[3];
    [SerializeField] private Text[] statusLabels = new Text[3];
    [SerializeField, Min(1f)] private float apexSpeedMultiplier = 1.35f;
    [SerializeField] private EscapeDoor escapeDoor;
    [Header("Action sounds")]
    [SerializeField] private AudioSource actionAudioSource;
    [SerializeField] private AudioClip killSound = null;
    [SerializeField] private AudioClip apexTransformSound = null;
    [SerializeField, Range(0, 1)] private float killSoundVolume = .7f;
    [SerializeField, Range(0, 1)] private float apexTransformSoundVolume = .7f;
    private readonly HuntAudioEvents audioEvents = new HuntAudioEvents();
    private readonly string[] identityHex = new string[3];
    private readonly Color[] identityColors = new Color[3];
    private bool identityCaptured;
    private ApexTransformationPresentation transformation;
    private RandomItemCoordinator items;
    private bool labelsInitialized;
    private HuntRules.Phase labelPhase;
    private int labelAliveMask, labelApex, labelWinner, labelCountdown;
    private bool labelWave, labelDoor;
    private int pendingContacts;
    private int touchingContacts;
    private bool failed;
    private bool verificationSuspended;
    public HuntRules Rules { get; private set; }
    public bool GameplayAvailable => !failed && !verificationSuspended && Rules != null;
    public void ConfigureDoor(EscapeDoor door) { escapeDoor = door; }
    public PlayerCombatant GetPlayer(int id) => players != null && id >= 0 && id < players.Length ? players[id] : null;
    public void CancelCameraShakes()
    { for (int i = 0; i < 3; i++) if (GetPlayer(i) != null) GetPlayer(i).CancelCameraShake(); }
    public Vector2 GetKillerPosition(int victimId)
    {
        int killer = Rules != null && victimId >= 0 && victimId < 3 ? Rules.KilledBy(victimId) : -1;
        if (players != null && killer >= 0 && killer < players.Length && players[killer] != null)
            return players[killer].transform.position;
        return players != null && victimId >= 0 && victimId < players.Length && players[victimId] != null
            ? (Vector2)players[victimId].transform.position : Vector2.zero;
    }
    public Color GetPlayerColor(int id)
    {
        if (id < 0 || id >= 3 || players == null || id >= players.Length || players[id] == null) return Color.white;
        if (identityCaptured) return identityColors[id];
        var renderer = players[id].GetComponent<SpriteRenderer>();
        return renderer == null ? Color.white : renderer.color;
    }

    public void Configure(CameraIntroduction intro, PlayerCombatant[] characters, Text[] labels)
    { introduction = intro; players = characters; statusLabels = labels; }

    private void Awake()
    {
        Rules = new HuntRules();
        if (!ValidateConfiguration(out string error)) { Fail(error); return; }
        for (int i = 0; i < 3; i++)
        {
            identityColors[i] = players[i].GetComponent<SpriteRenderer>().color;
            identityHex[i] = ColorUtility.ToHtmlStringRGB(identityColors[i]);
            statusLabels[i].supportRichText = true;
            statusLabels[i].color = Color.white;
        }
        identityCaptured = true;
        transformation = GetComponent<ApexTransformationPresentation>();
        items = GetComponent<RandomItemCoordinator>();
        if (actionAudioSource == null) actionAudioSource = GetComponent<AudioSource>();
        if (actionAudioSource == null || killSound == null || apexTransformSound == null)
            Debug.LogWarning("HuntRoundManager: action audio is incomplete. Assign Action Audio Source, Kill Sound, and Apex Transform Sound. Gameplay will continue.", this);
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
        if (failed) return;
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
        Rules.ResolveStep(contacts, escapeDoor != null ? escapeDoor.Occupants() : 0, Time.fixedDeltaTime);
        PlayActionSounds();
        ApplyState();
        if (items != null) items.ResolvePickups();
    }

    private void PlayActionSounds()
    {
        audioEvents.Observe(Rules, out int kills, out bool transformationStarted);
        if (actionAudioSource == null || !actionAudioSource.isActiveAndEnabled) return;
        // PlayOneShot permits the first kill and transformation clips to overlap.
        if (killSound != null)
            for (int i = 0; i < kills; i++) actionAudioSource.PlayOneShot(killSound, Mathf.Clamp01(killSoundVolume));
        if (transformationStarted && apexTransformSound != null)
            actionAudioSource.PlayOneShot(apexTransformSound, Mathf.Clamp01(apexTransformSoundVolume));
        // Never stop on Transforming -> FinalChase or RoundOver. The source owns the natural tail.
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
        if (items != null) items.SyncState();
        for (int i = 0; i < 3; i++)
        {
            bool apex = Rules.ApexId == i;
            bool freeze = Rules.State == HuntRules.Phase.RoundOver || (Rules.State == HuntRules.Phase.Transforming && apex);
            bool canMove = Rules.State != HuntRules.Phase.Opening && !freeze;
            players[i].ApplyState(Rules.IsAlive(i), canMove, freeze, apex, apexSpeedMultiplier);
            if (Rules.State == HuntRules.Phase.RoundOver)
                players[i].HoldCamera();
        }
        if (transformation != null) transformation.Observe();
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        bool wave = transformation != null && transformation.WaveActive;
        bool door = escapeDoor != null && escapeDoor.Configured;
        int countdown = Rules.State == HuntRules.Phase.Transforming ? ApexTransformationMath.CountdownTenths(Rules.TransformRemaining) : 0;
        if (labelsInitialized && labelPhase == Rules.State && labelAliveMask == Rules.AliveMask
            && labelApex == Rules.ApexId && labelWinner == Rules.WinnerId && labelCountdown == countdown && labelWave == wave && labelDoor == door) return;
        labelsInitialized = true;
        labelPhase = Rules.State; labelAliveMask = Rules.AliveMask; labelApex = Rules.ApexId;
        labelWinner = Rules.WinnerId; labelCountdown = countdown; labelWave = wave; labelDoor = door;
        for (int i = 0; i < 3; i++)
        {
            statusLabels[i].text = door && Rules.IsAlive(i) && i != Rules.ApexId
                && (Rules.State == HuntRules.Phase.Transforming || Rules.State == HuntRules.Phase.FinalChase)
                ? "OPEN THE ESCAPE DOOR\nAVOID APEX " + HuntPresentation.Identity(Rules.ApexId, identityHex)
                : HuntPresentation.Status(Rules, i, identityHex, wave);
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
