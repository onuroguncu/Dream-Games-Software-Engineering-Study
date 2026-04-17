using System.Collections.Generic;

//deserialized from Resources/level_XX.json files
//each string in grid represents a cell code(r, g, b, y, s, v, cbTL...)
[System.Serializable]
public class LevelData
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public List<string> grid;
}
