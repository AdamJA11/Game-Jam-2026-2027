using UnityEngine;

[CreateAssetMenu(menuName = "Game Jam/Two Map Rotation")]
public sealed class MapRotation : ScriptableObject
{
    public const string MapAPath = "Assets/Scenes/SplitScreenTest.unity";
    public const string MapBPath = "Assets/Scenes/SplitScreenMapB.unity";
    [SerializeField] private string mapA = MapAPath;
    [SerializeField] private string mapB = MapBPath;
    public string FirstMap => mapA;
    public string SecondMap => mapB;
    public string Next(string current) => MapRotationRules.Next(current, mapA, mapB);
}
