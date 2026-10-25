using System;
using UnityEngine;

// Three fixed-size world masks, each visible only to its owning gameplay camera.
[DisallowMultipleComponent]
public sealed class TrackerTrails : MonoBehaviour
{
    private const int Width = 320, Height = 192;
    [SerializeField] private int[] privateLayers = { 29, 30, 31 };
    private readonly PersistentTrailGrid[] grids = new PersistentTrailGrid[3];
    private readonly Texture2D[] textures = new Texture2D[3];
    private readonly SpriteRenderer[] renderers = new SpriteRenderer[3];
    private readonly Sprite[] sprites = new Sprite[3];
    private readonly bool[] active = new bool[3], dirty = new bool[3];
    private readonly Vector2[,] previous = new Vector2[3, 3];
    private readonly int[] previousMasks = new int[3];
    private readonly Color32[] palette = new Color32[8];
    private readonly Color32[] clearPixels = new Color32[Width * Height];
    private Camera[] cameras;
    private HuntRoundManager round;
    private Rect bounds;
    private bool prepared;
    private int claimedCameras;
    private double nextUpload;

    public void ConfigureLayers(int[] layers) { privateLayers = layers; }

    public bool ValidateLayers(out string error)
    {
        error = null;
        if (privateLayers == null || privateLayers.Length != 3) { error = "Assign three unused private trail layers."; return false; }
        for (int i = 0; i < 3; i++)
        {
            int layer = privateLayers[i];
            if (layer < 8 || layer > 31 || !string.IsNullOrEmpty(LayerMask.LayerToName(layer)))
            { error = "Trail layers must use unused, unnamed indices 8–31. Run Phase 3f setup again."; return false; }
            for (int j = 0; j < i; j++) if (layer == privateLayers[j]) { error = "Trail layers must be distinct."; return false; }
            foreach (var root in gameObject.scene.GetRootGameObjects()) foreach (var obj in root.GetComponentsInChildren<Transform>(true))
                if (obj.gameObject.layer == layer) { error = "Trail layer is already used by " + obj.name + ". Run Phase 3f setup again."; return false; }
        }
        return true;
    }

    public void Prepare(HuntRoundManager manager, Camera[] views, Rect mapBounds)
    {
        if (prepared) return;
        round = manager; cameras = views; bounds = mapBounds;
        int layerMask = 0; foreach (int layer in privateLayers) layerMask |= 1 << layer;
        for (int mask = 1; mask < 8; mask++)
        {
            Color color = Color.black; int count = 0;
            for (int id = 0; id < 3; id++) if ((mask & (1 << id)) != 0) { color += round.GetPlayerColor(id); count++; }
            color /= count; color.a = .8f; palette[mask] = color;
        }
        for (int viewer = 0; viewer < 3; viewer++)
        {
            previousMasks[viewer] = cameras[viewer].cullingMask;
            claimedCameras = viewer + 1;
            cameras[viewer].cullingMask = (previousMasks[viewer] & ~layerMask) | (1 << privateLayers[viewer]);
            grids[viewer] = new PersistentTrailGrid(Width, Height);
            textures[viewer] = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { name = "Player" + (viewer + 1) + "TrailMask", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            textures[viewer].SetPixels32(clearPixels); textures[viewer].Apply(false);
            sprites[viewer] = Sprite.Create(textures[viewer], new Rect(0, 0, Width, Height), Vector2.one * .5f, 1, 0, SpriteMeshType.FullRect);
            var obj = new GameObject("Player" + (viewer + 1) + "PrivateTrails", typeof(SpriteRenderer));
            obj.transform.SetParent(transform, false); obj.layer = privateLayers[viewer];
            obj.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0);
            Vector3 parentScale = transform.lossyScale;
            obj.transform.localScale = new Vector3(bounds.width / Width / parentScale.x, bounds.height / Height / parentScale.y, 1);
            renderers[viewer] = obj.GetComponent<SpriteRenderer>();
            renderers[viewer].sprite = sprites[viewer];
            renderers[viewer].sharedMaterial = round.GetPlayer(viewer).GetComponent<SpriteRenderer>().sharedMaterial;
            renderers[viewer].sortingLayerID = round.GetPlayer(viewer).GetComponent<SpriteRenderer>().sortingLayerID;
            renderers[viewer].sortingOrder = -4; renderers[viewer].enabled = false;
        }
        prepared = true;
    }

    public void Activate(int viewer)
    {
        if (!prepared || active[viewer]) return;
        active[viewer] = true; renderers[viewer].enabled = true;
        for (int target = 0; target < 3; target++) previous[viewer, target] = round.GetPlayer(target).transform.position;
    }

    public void ClearViewer(int viewer)
    {
        if (!prepared || !active[viewer]) return;
        active[viewer] = false; renderers[viewer].enabled = false;
        grids[viewer].Clear(); textures[viewer].SetPixels32(clearPixels); textures[viewer].Apply(false); dirty[viewer] = false;
    }

    public void Sample(double now)
    {
        if (!prepared) return;
        for (int viewer = 0; viewer < 3; viewer++) if (active[viewer])
            for (int target = 0; target < 3; target++) if (target != viewer && round.Rules.IsAlive(target))
            {
                Vector2 point = round.GetPlayer(target).transform.position, last = previous[viewer, target];
                float distance = Vector2.Distance(point, last);
                if (distance < .4f) continue;
                previous[viewer, target] = point;
                // Teleports are not historical paths. Normal movement gets short directional footprints.
                if (distance > 10) continue;
                int steps = Mathf.CeilToInt(distance / .4f);
                Vector2 direction = (point - last).normalized;
                for (int step = 1; step <= steps; step++)
                {
                    Vector2 center = Vector2.Lerp(last, point, step / (float)steps);
                    Stamp(viewer, target, center);
                    Stamp(viewer, target, center - direction * .12f);
                }
            }
        if (now < nextUpload) return;
        nextUpload = now + .1;
        for (int i = 0; i < 3; i++) if (dirty[i]) { textures[i].Apply(false); dirty[i] = false; }
    }

    private void Stamp(int viewer, int target, Vector2 point)
    {
        int x = Mathf.FloorToInt((point.x - bounds.xMin) / bounds.width * Width);
        int y = Mathf.FloorToInt((point.y - bounds.yMin) / bounds.height * Height);
        if (grids[viewer].Stamp(x, y, target))
        { textures[viewer].SetPixel(x, y, palette[grids[viewer].At(x, y)]); dirty[viewer] = true; }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i < claimedCameras && cameras != null && cameras[i] != null) cameras[i].cullingMask = previousMasks[i];
            if (sprites[i] != null) Destroy(sprites[i]);
            if (textures[i] != null) Destroy(textures[i]);
        }
    }
}
