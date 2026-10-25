using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EscapeStoryPresenter : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform stage;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private Image survivor, apex, ambulance, carIdentity;
    [SerializeField] private Image[] citizens = new Image[6];
    [SerializeField] private Image[] fragments = new Image[12];
    [SerializeField] private Text caption;
    [SerializeField] private Button skip;
    [SerializeField] private Sprite apexSprite;
    [SerializeField, Min(1)] private float duration = 8;
    private Action completed;
    private double started;
    private bool playing;
    private static readonly Vector2[] CitizenPositions = {
        new Vector2(.36f,.60f), new Vector2(.46f,.52f), new Vector2(.42f,.77f),
        new Vector2(.57f,.65f), new Vector2(.63f,.78f), new Vector2(.73f,.64f) };
    public float Duration => duration;

    public void Configure(GameObject root, RectTransform content, CanvasGroup group, Image person, Image monster,
        Image car, Image identity, Image[] people, Image[] pieces, Text text, Button skipButton, Sprite monsterSprite)
    {
        panel = root; stage = content; fade = group; survivor = person; apex = monster;
        ambulance = car; carIdentity = identity; citizens = people; fragments = pieces;
        caption = text; skip = skipButton; apexSprite = monsterSprite;
    }

    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (panel == null || stage == null || fade == null || survivor == null || apex == null || ambulance == null
            || carIdentity == null || caption == null || skip == null || apexSprite == null || ambulance.sprite == null
            || citizens == null || citizens.Length != 6 || fragments == null || fragments.Length != 12
            || !Finite(duration) || duration < 1 || panel == gameObject || transform.IsChildOf(panel.transform))
        { error = "Run Phase 3e setup and assign the story art/UI references and a finite duration of at least 1 second."; return false; }
        var canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay || panel.scene != gameObject.scene)
        { error = "Story panel must belong to this scene's screen-space overlay Canvas."; return false; }
        for (int i = 0; i < 6; i++) if (citizens[i] == null || citizens[i].sprite == null)
        { error = "Assign all six citizen images."; return false; }
        for (int i = 0; i < 12; i++) if (fragments[i] == null)
        { error = "Assign twelve reusable story fragments."; return false; }
        return true;
    }

    private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    private void Awake() { if (panel != null) panel.SetActive(false); }

    public bool TryPlay(Sprite playerSprite, Color identity, Action onFinished)
    {
        if (playing || !isActiveAndEnabled || playerSprite == null || !ValidateConfiguration(out _)) return false;
        survivor.sprite = playerSprite; survivor.color = identity;
        apex.sprite = apexSprite; apex.color = Color.white;
        carIdentity.color = identity;
        completed = onFinished; started = Time.unscaledTimeAsDouble; playing = true;
        skip.onClick.RemoveListener(Skip); skip.onClick.AddListener(Skip);
        panel.SetActive(true); Render(0);
        return true;
    }

    private void Update()
    {
        if (!playing) return;
        if (panel == null || !panel.activeInHierarchy || stage == null || fade == null || skip == null)
        { Finish(); return; }
        float elapsed = (float)(Time.unscaledTimeAsDouble - started);
        if (elapsed >= duration) { Finish(); return; }
        Render(elapsed / duration * 8);
    }

    private static float S(float t, float start, float end) => EscapeStoryState.Segment(t, start, end);
    private void Render(float t)
    {
        fade.alpha = Mathf.Min(S(t, 0, .4f), 1 - S(t, 7.6f, 8));
        skip.gameObject.SetActive(t >= .4f); skip.interactable = t >= .4f;
        caption.text = t >= 6.5f ? "You escaped. They didn't." : "THE STREET OUTSIDE";
        float drive = S(t, 4.5f, 6.8f);
        Vector2 car = new Vector2(Mathf.Lerp(.68f, 1.28f, drive * drive), .32f);
        Place(ambulance, car, 235, 110);
        Place(carIdentity, car + new Vector2(0, .095f), 50, 9);
        survivor.enabled = t < 4.5f;
        Vector2 runner = Vector2.Lerp(new Vector2(.13f,.65f), new Vector2(.65f,.36f), S(t,.4f,4.5f));
        Place(survivor, runner, 65, 75);
        apex.enabled = t >= 2;
        Vector2 monster = Vector2.Lerp(new Vector2(.13f,.65f), CitizenPositions[0], S(t,2,2.9f));
        if (t >= 2.9f) monster = Vector2.Lerp(CitizenPositions[0], CitizenPositions[1], S(t,2.9f,3.6f));
        if (t >= 3.6f) monster = Vector2.Lerp(CitizenPositions[1], new Vector2(.91f,.32f), S(t,3.6f,6.8f));
        Place(apex, monster, 100, 105);
        for (int i = 0; i < 6; i++)
        {
            float hit = i == 0 ? 2.9f : 3.6f;
            bool dead = i < 2 && t >= hit;
            citizens[i].enabled = !dead;
            Vector2 flee = i < 2 ? Vector2.zero : new Vector2((i % 2 == 0 ? -1 : 1) * .11f, .10f) * S(t,2,5);
            Place(citizens[i], CitizenPositions[i] + flee, 58, 68);
            if (i >= 2) continue;
            float progress = S(t, hit, hit + .65f);
            for (int piece = 0; piece < 6; piece++)
            {
                Image fragment = fragments[i * 6 + piece];
                fragment.enabled = dead && progress < 1;
                float a = (piece * 60 + i * 19) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Place(fragment, CitizenPositions[i] + direction * (.10f * progress), 12, 12);
                fragment.rectTransform.localRotation = Quaternion.Euler(0,0,piece * 60 + progress * 160);
                fragment.color = new Color(.85f, .78f, .59f, 1 - progress);
            }
        }
    }

    private void Place(Image image, Vector2 position, float width, float height)
    {
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
        rect.anchoredPosition = new Vector2((position.x - .5f) * stage.rect.width, (position.y - .5f) * stage.rect.height);
        rect.sizeDelta = new Vector2(width, height) * (stage.rect.height / 900);
    }

    private void Skip() { if (playing && Time.unscaledTimeAsDouble - started >= duration * .05) Finish(); }
    private void Finish()
    {
        if (!playing) return;
        Action callback = completed;
        Cancel(); callback?.Invoke();
    }
    public void Cancel()
    {
        playing = false; completed = null;
        if (skip != null) skip.onClick.RemoveListener(Skip);
        if (panel != null) panel.SetActive(false);
    }
    private void OnDisable() { Cancel(); }
}
