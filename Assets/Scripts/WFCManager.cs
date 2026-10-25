using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFCManager : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float tileSize = 1f; // Remettez à 1 si vos blocs sont trop espacés
    public List<WFCTile> allTiles; 

    private Cell[,] grid;

    private void Start()
    {
        StartCoroutine(GenerateGrid());
    }

    private IEnumerator GenerateGrid()
    {
        bool success = false;
        
        while (!success)
        {
            InitializeGrid();
            success = true;
            
            // L'ordinateur calcule tout instantanément sans faire de pause
            while (!IsGridFullyCollapsed())
            {
                if (!CollapseCell()) 
                {
                    success = false;
                    break; 
                }
                Propagate();
            }

            // S'il y a une contradiction mathématique, on attend une frame avant de recommencer (évite de figer Unity)
            if (!success) yield return null;
        }

        // Fait apparaître tous les blocs simultanément à la fin du calcul
        InstantiateTiles();
        Debug.Log("Labyrinthe généré avec succès !");
    }

    private void InitializeGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new Cell(allTiles);
            }
        }
    }

    private bool IsGridFullyCollapsed()
    {
        foreach (var cell in grid)
        {
            if (!cell.collapsed) return false;
        }
        return true;
    }

    private bool CollapseCell() 
    {
        int lowestEntropy = int.MaxValue;
        List<Vector2Int> cellsToCollapse = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y].collapsed)
                {
                    int entropy = grid[x, y].possibleTiles.Count;
                    if (entropy == 0) return false; 

                    if (entropy < lowestEntropy)
                    {
                        lowestEntropy = entropy;
                        cellsToCollapse.Clear();
                        cellsToCollapse.Add(new Vector2Int(x, y));
                    }
                    else if (entropy == lowestEntropy)
                    {
                        cellsToCollapse.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        if (cellsToCollapse.Count == 0) return true;

        Vector2Int chosenCell = cellsToCollapse[Random.Range(0, cellsToCollapse.Count)];
        Cell cell = grid[chosenCell.x, chosenCell.y];

        WFCTile selectedTile = cell.possibleTiles[Random.Range(0, cell.possibleTiles.Count)];
        cell.possibleTiles.Clear();
        cell.possibleTiles.Add(selectedTile);
        cell.collapsed = true;

        return true; 
    }

    private void Propagate()
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y].collapsed) continue;

                    int possibleCount = grid[x, y].possibleTiles.Count;
                    List<WFCTile> validTiles = new List<WFCTile>(grid[x, y].possibleTiles);

                    if (y < height - 1) validTiles.RemoveAll(t => !IsTileValid(t, grid[x, y + 1], "TOP"));
                    if (y > 0) validTiles.RemoveAll(t => !IsTileValid(t, grid[x, y - 1], "BOTTOM"));
                    if (x < width - 1) validTiles.RemoveAll(t => !IsTileValid(t, grid[x + 1, y], "RIGHT"));
                    if (x > 0) validTiles.RemoveAll(t => !IsTileValid(t, grid[x - 1, y], "LEFT"));

                    if (validTiles.Count < possibleCount)
                    {
                        grid[x, y].possibleTiles = validTiles;
                        changed = true; 
                    }
                }
            }
        }
    }

    private bool IsTileValid(WFCTile tileToCheck, Cell neighborCell, string direction)
    {
        foreach (var neighborTile in neighborCell.possibleTiles)
        {
            if (direction == "TOP" && neighborTile.allowedBottom.Contains(tileToCheck)) return true;
            if (direction == "BOTTOM" && neighborTile.allowedTop.Contains(tileToCheck)) return true;
            if (direction == "RIGHT" && neighborTile.allowedLeft.Contains(tileToCheck)) return true;
            if (direction == "LEFT" && neighborTile.allowedRight.Contains(tileToCheck)) return true;
        }
        return false;
    }

    private void InstantiateTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Construit le labyrinthe à partir de la position du générateur
                Vector3 pos = transform.position + new Vector3(x * tileSize, y * tileSize, 0);
                if (grid[x, y].possibleTiles.Count > 0 && grid[x, y].possibleTiles[0].prefab != null)
                {
                    Instantiate(grid[x, y].possibleTiles[0].prefab, pos, Quaternion.identity, transform);
                }
            }
        }
    }
}

public class Cell
{
    public bool collapsed;
    public System.Collections.Generic.List<WFCTile> possibleTiles;

    public Cell(System.Collections.Generic.List<WFCTile> allAvailableTiles)
    {
        collapsed = false;
        possibleTiles = new System.Collections.Generic.List<WFCTile>(allAvailableTiles); 
    }
}