using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WFC/Tile Data")]
public class WFCTile : ScriptableObject
{
    public GameObject prefab; // The visual tile
    
    public List<WFCTile> allowedTop;
    public List<WFCTile> allowedRight;
    public List<WFCTile> allowedBottom;
    public List<WFCTile> allowedLeft;
}