using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//owns the grid data structure and all item spawning
//provides coordinate helpers used by blastmanager, fallmanager, and specialitemmanager
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject vasePrefab;
    [SerializeField] private GameObject stonePrefab;
    [SerializeField] private GameObject chaliceBoxPrefab;
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private Image gridBackground;

    private GridItem[,] grid;
    private LevelData levelData;
    private float cellSize;

    //tracks which cells each chalice box master "claims" so shared cells can be reassigned on destroy
    private Dictionary<ChaliceBoxItem, HashSet<(int, int)>> chaliceBoxCells =
        new Dictionary<ChaliceBoxItem, HashSet<(int, int)>>();

    public int Rows => levelData.grid_height;
    public int Cols => levelData.grid_width;
    public RectTransform GridContainer => gridContainer;
    public float CellSize => cellSize;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    //sizes the grid container sets up the background and spawns all items from leveldata
    public void InitGrid(LevelData data)
    {
        levelData = data;
        grid = new GridItem[data.grid_height, data.grid_width];

        cellSize = 100f;

        gridContainer.sizeDelta = new Vector2(cellSize * data.grid_width, cellSize * data.grid_height);
        gridContainer.anchoredPosition = new Vector2(gridContainer.anchoredPosition.x,
            gridContainer.anchoredPosition.y - 50f);
        Canvas.ForceUpdateCanvases();

        if (gridBackground != null)
        {
            gridBackground.sprite = SpriteCache.Get("Art/UI/Gameplay/grid_background");
            gridBackground.type = Image.Type.Sliced;
            RectTransform bgRt = gridBackground.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(-15, -15);
            bgRt.offsetMax = new Vector2(15, 15);
        }

        SpawnItems();
    }

    //iterates the flat grid list from leveldata and spawns each cell item
    //top-down order (highest row first) ensures cbTL masters exist before cbBL/cbBR slaves run
    private void SpawnItems()
    {
        for (int row = levelData.grid_height - 1; row >= 0; row--)
            for (int col = 0; col < levelData.grid_width; col++)
                SpawnItem(row, col, levelData.grid[row * levelData.grid_width + col]);
    }

    //maps cell code strings to spawn calls
    //rand picks a random color cbTR/cbBL cbBR are slave cells of a 2x2 ChaliceBox
    private void SpawnItem(int row, int col, string code)
    {
        if (code == "rand")
        {
            string[] colors = { "r", "g", "b", "y" };
            code = colors[Random.Range(0, colors.Length)];
        }

        switch (code)
        {
            case "r": SpawnCube(row, col, CubeColor.Red); break;
            case "g": SpawnCube(row, col, CubeColor.Green); break;
            case "b": SpawnCube(row, col, CubeColor.Blue); break;
            case "y": SpawnCube(row, col, CubeColor.Yellow); break;
            case "vro": SpawnRocketCube(row, col, isVertical: true); break;
            case "hro": SpawnRocketCube(row, col, isVertical: false); break;
            case "t": SpawnTNTCube(row, col); break;
            case "s": SpawnStone(row, col); break;
            case "v": SpawnVase(row, col); break;
            case "cbTL": SpawnChaliceBox(row, col); break;
            case "cbTR": case "cbBL": case "cbBR":
                SpawnChaliceBoxSlave(row, col, code); break;
        }
    }

    private void SpawnCube(int row, int col, CubeColor color)
    {
        GameObject obj = Instantiate(cubePrefab, gridContainer);
        CubeItem cube = obj.GetComponent<CubeItem>();
        cube.SetColor(color);
        cube.Init(row, col);
        SetPosition(obj.GetComponent<RectTransform>(), row, col);
        grid[row, col] = cube;
    }

    private void SpawnRocketCube(int row, int col, bool isVertical)
    {
        GameObject obj = Instantiate(cubePrefab, gridContainer);
        CubeItem cube = obj.GetComponent<CubeItem>();
        cube.Init(row, col);
        CubeColor randomColor = (CubeColor)Random.Range(0, 4);
        cube.SetColor(randomColor);
        cube.SetSpecialState(isRocket: true, isTnt: false, isVertical: isVertical);
        SetPosition(obj.GetComponent<RectTransform>(), row, col);
        grid[row, col] = cube;
    }

    private void SpawnTNTCube(int row, int col)
    {
        GameObject obj = Instantiate(cubePrefab, gridContainer);
        CubeItem cube = obj.GetComponent<CubeItem>();
        cube.Init(row, col);
        CubeColor randomColor = (CubeColor)Random.Range(0, 4);
        cube.SetColor(randomColor);
        cube.SetSpecialState(isRocket: false, isTnt: true, isVertical: false);
        SetPosition(obj.GetComponent<RectTransform>(), row, col);
        grid[row, col] = cube;
    }

    //called by blastmanager after a group of 4+ cubes is blasted to create a rocket
    public CubeItem SpawnRocket(int row, int col, bool isVertical, CubeColor color)
    {
        GameObject obj = Instantiate(cubePrefab, gridContainer);
        CubeItem cube = obj.GetComponent<CubeItem>();
        cube.Init(row, col);
        cube.SetColor(color);
        cube.SetSpecialState(isRocket: true, isTnt: false, isVertical: isVertical);
        SetPosition(obj.GetComponent<RectTransform>(), row, col);
        grid[row, col] = cube;
        return cube;
    }

    //called by blastmanager after a group of 6+ cubes is blasted to create a TNT
    public CubeItem SpawnTNT(int row, int col, CubeColor color)
    {
        GameObject obj = Instantiate(cubePrefab, gridContainer);
        CubeItem cube = obj.GetComponent<CubeItem>();
        cube.Init(row, col);
        cube.SetColor(color);
        cube.SetSpecialState(isRocket: false, isTnt: true, isVertical: false);
        SetPosition(obj.GetComponent<RectTransform>(), row, col);
        grid[row, col] = cube;
        return cube;
    }

    //called by fallmanager to fill empty cells after items are destroyed
    public CubeItem SpawnRandomCube(int row, int col)
    {
        CubeColor color = (CubeColor)Random.Range(0, 4);
        GameObject obj = Instantiate(cubePrefab, gridContainer);
        CubeItem cube = obj.GetComponent<CubeItem>();
        cube.SetColor(color);
        cube.Init(row, col);
        SetPosition(obj.GetComponent<RectTransform>(), row, col);
        grid[row, col] = cube;
        return cube;
    }

    private void SpawnVase(int row, int col)
    {
        GameObject obj = Instantiate(vasePrefab, gridContainer);
        VaseItem vase = obj.GetComponent<VaseItem>();
        vase.Init(row, col);
        SetPosition(obj.GetComponent<RectTransform>(), row, col);
        grid[row, col] = vase;
    }

    private void SpawnStone(int row, int col)
    {
        GameObject obj = Instantiate(stonePrefab, gridContainer);
        StoneItem stone = obj.GetComponent<StoneItem>();
        stone.Init(row, col);
        SetPosition(obj.GetComponent<RectTransform>(), row, col);
        grid[row, col] = stone;
    }

    //registers a cell to a chalice box both in the grid and in the claims dictionary
    //last writer wins for grid ownership, but ALL claimants are remembered for reassignment on destroy
    private void RegisterChaliceCell(int row, int col, ChaliceBoxItem cb)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Cols) return;
        grid[row, col] = cb;
        if (!chaliceBoxCells.ContainsKey(cb))
            chaliceBoxCells[cb] = new HashSet<(int, int)>();
        chaliceBoxCells[cb].Add((row, col));
    }

    //cbTL is the master. adjacent boxes share columns (col+1 claimed by both this box and the next).
    //SpawnItems runs top-down so masters exist before slaves. shared cells are tracked via chaliceBoxCells
    //so that when one box dies its shared cells are reassigned to the surviving neighbor.
    private void SpawnChaliceBox(int row, int col)
    {
        GameObject obj = Instantiate(chaliceBoxPrefab, gridContainer);
        ChaliceBoxItem cb = obj.GetComponent<ChaliceBoxItem>();
        cb.Init(row, col);
        cb.SetAsMaster();

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(cellSize * 2, cellSize * 2);
        Vector2 pos = GetAnchoredPosition(row, col);
        rt.anchoredPosition = new Vector2(pos.x + cellSize / 2f, pos.y - cellSize / 2f);

        RegisterChaliceCell(row, col, cb);
        RegisterChaliceCell(row, col + 1, cb);
        RegisterChaliceCell(row - 1, col, cb);
        RegisterChaliceCell(row - 1, col + 1, cb);
    }

    //slave cells resolve their master from already-spawned grid data and register their own cell
    //chain delegation: cbBL at row N finds master at row N+1 (already set), sets itself, enabling row N-1 to chain
    private void SpawnChaliceBoxSlave(int row, int col, string code)
    {
        int masterRow = row, masterCol = col;
        if (code == "cbTR") masterCol = col - 1;
        else if (code == "cbBL") masterRow = row + 1;
        else if (code == "cbBR") { masterRow = row + 1; masterCol = col - 1; }

        if (masterRow >= 0 && masterRow < Rows && masterCol >= 0 && masterCol < Cols)
            if (grid[masterRow, masterCol] is ChaliceBoxItem master)
                RegisterChaliceCell(row, col, master);
    }

    //when a box is destroyed, for each cell it claimed:
    //if another live box also claimed that cell, reassign it to that box instead of clearing
    //I think levels 3 and 9 contain manually authored layout errors in their JSON files 
    //that cause visual/logic bugs unrelated to the engine code.
    public void ClearChaliceBoxCells(ChaliceBoxItem master)
    {
        if (!chaliceBoxCells.TryGetValue(master, out var cells)) return;
        chaliceBoxCells.Remove(master);

        foreach (var (r, c) in cells)
        {
            if (grid[r, c] != (GridItem)master) continue; //another box already owns this cell, skip

            //find another live box that also claimed this cell
            ChaliceBoxItem newOwner = null;
            foreach (var kvp in chaliceBoxCells)
                if (kvp.Value.Contains((r, c))) { newOwner = kvp.Key; break; }

            grid[r, c] = newOwner; //null if no other claimant, otherwise the surviving neighbor
        }
    }

    private void SetPosition(RectTransform rt, int row, int col)
    {
        rt.sizeDelta = new Vector2(cellSize, cellSize);
        rt.anchoredPosition = GetAnchoredPosition(row, col);
    }

    //converts grid coordinates to canvas-local anchoredPosition (center-origin)
    public Vector2 GetAnchoredPosition(int row, int col)
    {
        float x = col * cellSize - (Cols * cellSize / 2f) + cellSize / 2f;
        float y = row * cellSize - (Rows * cellSize / 2f) + cellSize / 2f;
        return new Vector2(x, y);
    }

    //converts grid coordinates to world position (used by SpecialItemManager for DOMove targets)
    public Vector3 GetWorldPosition(int row, int col)
    {
        return gridContainer.TransformPoint(
            new Vector3(GetAnchoredPosition(row, col).x,
                        GetAnchoredPosition(row, col).y, 0));
    }

    public GridItem GetItem(int row, int col)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Cols) return null;
        return grid[row, col];
    }

    public void SetItem(int row, int col, GridItem item) => grid[row, col] = item;
    public void ClearCell(int row, int col) => grid[row, col] = null;
}
