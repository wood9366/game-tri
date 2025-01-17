using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    private GameObject _template_block_item;

    [System.Serializable]
    public class BlockType
    {
        public int type;
        public Color color;
    }

    [SerializeField]
    private List<BlockType> _block_types = new List<BlockType>();

    [SerializeField]
    private float _cell_size = 0.2f;

    [SerializeField]
    private Vector2 _origin = Vector2.zero;

    [SerializeField]
    private int _num_cols = 10;

    [System.Serializable]
    class CellLine
    {
        public int s = 0;
        public int e = 0;
    }

    [SerializeField]
    private List<CellLine> _rows = new List<CellLine>();

    class Cell
    {
        public int x;
        public int y;
    }

    class Eliminate
    {
        public List<Cell> cells = new List<Cell>();
    }

    class Block
    {
        public int id = 0;
        public int x;
        public int y;
        public int type;
    }

    private Block[,] _blocks;
    private Dictionary<int, BlockItem> _block_items = new Dictionary<int, BlockItem>();
    private int _start_block_id = 1;

    private int _next_block_id() => _start_block_id++;
    private int GetNumCols() => _num_cols;
    private int GetNumRows() => _rows.Count;

    void Start()
    {
        _blocks = new Block[_num_cols, _rows.Count];
        for (int x = 0; x < GetNumCols(); x++)
            for (int y = 0; y < GetNumRows(); y++)
                _blocks[x, y] = new Block();

        GameStart();
    }

    internal void GameStart()
    {
        GenerateBlocks();
        GenerateBlockItems();
    }

    private void _clear_block_items()
    {
        foreach (var item in _block_items.Values)
            GameObject.Destroy(item.gameObject);

        _block_items.Clear();
    }

    private void _clear_blocks()
    {
        for (int x = 0; x < GetNumCols(); x++)
            for (int y = 0; y < GetNumRows(); y++)
                _blocks[x, y].id = 0;
    }

    internal void DoFill()
    {
        _change_blocks.Clear();

        // find empty blocks on each col
        for (int x = 0; x < GetNumCols(); x++)
        {
            for (int y = 0; y < GetNumRows(); y++)
            {
                var block = _blocks[x, y];

                if (block.id != 0)
                    break;

                var block_type = _block_types[Random.Range(0, _block_types.Count)];

                block.id = _next_block_id();
                block.type = block_type.type;

                _change_blocks.Add(block.id, new Cell() { x = x, y = y });
            }
        }

        foreach (var item in _change_blocks)
        {
            var block = _blocks[item.Value.x, item.Value.y];
            var block_type = _block_types.Find(x => x.type == block.type);

            _create_block_item(item.Key, block_type, item.Value.x, item.Value.y);
        }
    }

    private Dictionary<int, Cell> _change_blocks = new Dictionary<int, Cell>();

    internal void DoDrop()
    {
        _change_blocks.Clear();
        
        // cal drop blocks on each col
        for (int x = 0; x < GetNumCols(); x++)
        {
            int num_empty = 0;
            int y = GetNumRows() - 1;

            // from bottom to top check link empty block
            while (y >= 0)
            {
                if (_blocks[x, y].id == 0)
                {
                    num_empty++;
                    y--;
                }
                else
                {
                    if (num_empty > 0)
                    {
                        // move item on top of empty blocks down
                        for (int i = y; i >= 0; i--)
                        {
                            var from_block = _blocks[x, i];
                            var to_block = _blocks[x, i + num_empty];

                            to_block.id = from_block.id;
                            to_block.type = from_block.type;

                            // record drop block id and move to cell
                            if (!_change_blocks.TryGetValue(from_block.id, out var drop_block))
                            {
                                drop_block = new Cell();
                                _change_blocks.Add(from_block.id, drop_block);
                            }

                            drop_block.x = x;
                            drop_block.y = i + num_empty;
                        }

                        for (int i = 0; i < num_empty; i++)
                            _blocks[x, i].id = 0;

                        y += num_empty - 1;
                        num_empty = 0;
                    }
                    else
                    {
                        y--;
                    }
                }
            }
        }

        // update drop block items pos
        foreach (var id2cell in _change_blocks)
        {
            if (_block_items.TryGetValue(id2cell.Key, out var item))
            {
                var cell = id2cell.Value;
                item.transform.position = _get_cell_pos(cell.x, cell.y);
            }
        }
    }

    static private readonly int NUM_MIN_LINK = 3;

    private List<Eliminate> _row_eliminates = new List<Eliminate>();
    private List<Eliminate> _col_eliminates = new List<Eliminate>();
    private HashSet<int> _eliminate_blocks = new HashSet<int>();

    internal void DoEliminate()
    {
        // remove eliminate blocks
        _eliminate_blocks.Clear();

        foreach (var e in _row_eliminates)
        {
            foreach (var c in e.cells)
            {
                var block = _blocks[c.x, c.y];

                _eliminate_blocks.Add(block.id);
                block.id = 0;
            }
        }

        _row_eliminates.Clear();

        foreach (var e in _col_eliminates)
        {
            foreach (var c in e.cells)
            {
                var block = _blocks[c.x, c.y];

                _eliminate_blocks.Add(block.id);
                block.id = 0;
            }
        }

        _col_eliminates.Clear();

        // remove eliminate block items
        foreach (var id in _eliminate_blocks)
        {
            if (_block_items.TryGetValue(id, out var item))
            {
                _block_items.Remove(id);
                GameObject.Destroy(item.gameObject);
            }
        }

        _eliminate_blocks.Clear();
    }

    internal void CheckEliminate()
    {
        _row_eliminates.Clear();
        _col_eliminates.Clear();

        _check_eliminate_by_line(true, ref _row_eliminates);
        _check_eliminate_by_line(false, ref _col_eliminates);
    }

    private void _check_eliminate_by_line(bool is_by_row, ref List<Eliminate> eliminates)
    {
        var num_check_line = is_by_row ? GetNumRows() : GetNumCols();

        for (int i = 0; i < num_check_line; i++)
            _check_eliminate_on_line(is_by_row, i, ref eliminates);
    }

    private void _check_eliminate_on_line(bool is_by_row, int i, ref List<Eliminate> eliminates )
    {
        var num = is_by_row ? GetNumCols() : GetNumRows();

        // check link item from each one on line
        int j = 0;
        while (j < num - NUM_MIN_LINK + 1)
        {
            var block = is_by_row ? _blocks[j, i] : _blocks[i, j];

            if (block.id == 0)
            {
                j++;
                continue;
            }

            int link_num = 0;
            int check_link_num_max = num - j;

            // check link item as long as possible
            for (int k = 1; k < check_link_num_max; k++)
            {
                if (j + k >= num)
                    break;

                var check_block = is_by_row ? _blocks[j + k, i] : _blocks[i, j + k];

                if (check_block.id == 0 || block.type != check_block.type)
                    break;

                link_num = k + 1;
            }

            if (link_num >= NUM_MIN_LINK)
            {
                var estimate = new Eliminate();
                for (int k = 0; k < link_num; k++)
                {
                    if (is_by_row)
                        estimate.cells.Add(new Cell() { x = j + k, y = i });
                    else
                        estimate.cells.Add(new Cell() { x = i, y = j + k });
                }

                eliminates.Add(estimate);

                j += link_num;
            }
            else
                j++;
        }
    }

    internal void GenerateBlocks()
    {
        if (_block_types.Count <= 0)
        {
            Debug.LogError("At least on block type");
            return;
        }

        _clear_blocks();
        for (int x = 0; x < _num_cols; x++)
        {
            for (int y = 0; y < _rows.Count; y++)
            {
                var block_type = _block_types[Random.Range(0, _block_types.Count)];
                var block = _blocks[x, y];

                block.id = _next_block_id();
                block.x = x;
                block.y = y;
                block.type = block_type.type;
            }
        }
    }

    internal void GenerateBlockItems()
    {
        _clear_block_items();

        for (int x = 0; x < GetNumCols(); x++)
        {
            for (int y = 0; y < GetNumRows(); y++)
            {
                var block = _blocks[x, y];
                var block_type = _block_types.Find(x => x.type == block.type);

                if (block_type != null)
                {
                    _create_block_item(block.id, block_type, block.x, block.y);
                }
            }
        }
    }

    private BlockItem _create_block_item(int id, BlockType type, int x, int y)
    {
        var obj = GameObject.Instantiate(_template_block_item, transform);
        obj.name = $"_block_{id}_{x}_{y}_{type.type}";
        obj.transform.localScale = Vector3.one * _cell_size * 0.9f;
        obj.transform.position = _get_cell_pos(x, y);

        var block_item = obj.GetComponent<BlockItem>();
        block_item.Init(id, x, y, type);

        _block_items.Add(id, block_item);

        return block_item;
    }

    private Vector2 _get_cell_pos(int x, int y)
    {
        return new Vector2(
            _origin.x - (GetNumCols() * _cell_size) / 2.0f + (x + 0.5f) * _cell_size,
            _origin.y + (GetNumRows() * _cell_size) / 2.0f - (y + 0.5f) * _cell_size);
    }

#if UNITY_EDITOR
    public void Save()
    {
        StringBuilder str = new StringBuilder();
        for (int x = 0; x < _blocks.GetLength(0); x++)
        {
            for (int y = 0; y < _blocks.GetLength(1); y++)
            {
                var block = _blocks[x, y];
                if (!(x == 0 && y == 0))
                    str.Append("|");
                str.Append($"{block.id},{block.x},{block.y},{block.type}");
            }
        }

        PlayerPrefs.SetString("blocks", str.ToString());
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey("blocks"))
            return;

        var str = PlayerPrefs.GetString("blocks");

        //Debug.Log(str);

        foreach (var block_str in str.Split("|"))
        {
            var items = block_str.Split(",");

            if (items.Length < 4)
                continue;

            if (int.TryParse(items[0], out var id) &&
                int.TryParse(items[1], out var x) &&
                int.TryParse(items[2], out var y) &&
                int.TryParse(items[3], out var type))
            {
                var block = _blocks[x, y];

                block.id = id;
                block.x = x;
                block.y = y;
                block.type = type;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int x = 0; x < GetNumCols(); x++)
        {
            for (int y = 0; y < GetNumRows(); y++)
            {
                if (x >= _rows[y].s && x <= GetNumCols() - 1 - _rows[y].e)
                    _draw_cell(x, y);
            }
        }

        Gizmos.color = Color.red;

        foreach (var e in _col_eliminates)
            _draw_estimate(e);

        foreach (var e in _row_eliminates)
            _draw_estimate(e);
    }

    private void _draw_estimate(Eliminate e)
    {
        for (int i = 0; i < e.cells.Count - 1; i++)
        {
            Gizmos.DrawLine(
                _get_cell_pos(e.cells[i].x, e.cells[i].y),
                _get_cell_pos(e.cells[i + 1].x, e.cells[i + 1].y));
        }
    }

    private void _draw_cell(int x, int y)
    {
        var pos = _get_cell_pos(x, y);
        Gizmos.DrawWireCube(pos, Vector2.one * _cell_size);
        if (Application.isPlaying)
        {
            var block = _blocks[x, y];
            var style = _get_cell_label_style();
            var block_type = _block_types.Find(x => x.type == block.type);
            if (block.id == 0)
                style.normal.textColor = Color.gray;
            else
                style.normal.textColor = block_type != null ? block_type.color : Color.gray;
            UnityEditor.Handles.Label(pos, $"{block.id}|{block.type}", style);
        }
    }

    private GUIStyle _cell_label_style;

    private GUIStyle _get_cell_label_style()
    {
        if (_cell_label_style == null)
        {
            _cell_label_style = new GUIStyle(GUI.skin.label);
            _cell_label_style.alignment = TextAnchor.MiddleCenter;
        }

        return _cell_label_style;
    }
#endif
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(Game))]
class GameInspector : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var owner = target as Game;

        if (GUILayout.Button("Save"))
        {
            owner.Save();
        }

        if (GUILayout.Button("Load"))
        {
            owner.Load();
            owner.GenerateBlockItems();
        }

        if (GUILayout.Button("Reset"))
        {
            owner.GenerateBlocks();
            owner.GenerateBlockItems();
        }

        if (GUILayout.Button("Check Eliminate"))
        {
            owner.CheckEliminate();
        }

        if (GUILayout.Button("Do Eliminate"))
        {
            owner.DoEliminate();
        }

        if (GUILayout.Button("Do Drop"))
        {
            owner.DoDrop();
        }

        if (GUILayout.Button("Do Fill"))
        {
            owner.DoFill();
        }

        DrawDefaultInspector();
    }
}
#endif
