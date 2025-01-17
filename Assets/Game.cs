using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
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

    void Start()
    {
        _blocks = new Block[_num_cols, _rows.Count];
        for (int x = 0; x < _blocks.GetLength(0); x++)
            for (int y = 0; y < _blocks.GetLength(1); y++)
                _blocks[x, y] = new Block();

        GameStart();
    }

    internal async void GameStart()
    {
        _generate_blocks();
        GenerateBlockItems();

        await Task.Delay(3000);

        _check_eliminates();
    }

    private void _clear_block_items()
    {
        foreach (var item in _block_items.Values)
            GameObject.Destroy(item.gameObject);

        _block_items.Clear();
    }

    private void _clear_blocks()
    {
        for (int x = 0; x < _blocks.GetLength(0); x++)
            for (int y = 0; y < _blocks.GetLength(1); y++)
                _blocks[x, y].id = 0;
    }

    static private readonly int NUM_MIN_LINK = 3;

    private List<Eliminate> _row_eliminates = new List<Eliminate>();
    private List<Eliminate> _col_eliminates = new List<Eliminate>();

    internal void _check_eliminates()
    {
        _row_eliminates.Clear();
        _col_eliminates.Clear();
        
        int num_cols = _blocks.GetLength(0);
        int num_rows = _blocks.GetLength(1);

        for (int x = 0; x < num_cols; x++)
        {
            for (int y = 0; y < num_rows - NUM_MIN_LINK + 1; )
            {
                var block = _blocks[x, y];

                int link_num = 0;
                int check_link_num_max = num_rows;

                for (int i = 1; i < check_link_num_max; i++)
                {
                    if (y + i < num_rows && block.type == _blocks[x, y + i].type)
                        link_num = i + 1;
                    else
                        break;
                }

                if (link_num >= NUM_MIN_LINK)
                {
                    var estimate = new Eliminate();
                    for (int i = 0; i < link_num; i++)
                        estimate.cells.Add(new Cell() { x = x, y = y + i });

                    _col_eliminates.Add(estimate);

                    y += link_num;
                }
                else
                    y++;
            }
        }

        for (int y = 0; y < num_rows; y++)
        {
            for (int x = 0; x < num_cols - NUM_MIN_LINK - 1; )
            {
                var block = _blocks[x, y];

                int link_num = 0;
                int check_link_num_max = num_cols;

                for (int i = 1; i < check_link_num_max; i++)
                {
                    if (x + i < num_rows && block.type == _blocks[x + i, y].type)
                        link_num = i + 1;
                    else
                        break;
                }

                if (link_num >= NUM_MIN_LINK)
                {
                    var estimate = new Eliminate();
                    for (int i = 0; i < link_num; i++)
                        estimate.cells.Add(new Cell() { x = x + i, y = y });

                    _row_eliminates.Add(estimate);

                    x += link_num;
                }
                else
                    x++;
            }
        }
    }

    private void _generate_blocks()
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

        for (int x = 0; x < _num_cols; x++)
        {
            for (int y = 0; y < _rows.Count; y++)
            {
                var block = _blocks[x, y];
                var block_type = _block_types.Find(x => x.type == block.type);

                if (block_type != null)
                {
                    var block_item = _create_block_item(block.id, block_type, block.x, block.y);
                    block_item.transform.position = _get_cell_pos(x, y);
                    _block_items.Add(block.id, block_item);
                }
            }
        }
    }

    private BlockItem _create_block_item(int id, BlockType type, int x, int y)
    {
        var obj = GameObject.Instantiate(_template_block_item, transform);
        obj.name = $"_block_{id}_{x}_{y}_{type.type}";
        obj.transform.localScale = Vector3.one * _cell_size * 0.9f;

        var block_item = obj.GetComponent<BlockItem>();
        block_item.Init(id, x, y, type);

        return block_item;
    }

    private Vector2 _get_cell_pos(int x, int y)
    {
        return new Vector2(
            _origin.x - (_num_cols * _cell_size) / 2.0f + (x + 0.5f) * _cell_size,
            _origin.y + (_rows.Count * _cell_size) / 2.0f - (y + 0.5f) * _cell_size);
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

        Debug.Log(str);

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
        for (int x = 0; x < _num_cols; x++)
        {
            for (int y = 0; y < _rows.Count; y++)
            {
                if (x >= _rows[y].s && x <= _num_cols - 1 - _rows[y].e)
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
[CustomEditor(typeof(Game))]
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

        if (GUILayout.Button("Check"))
        {
            owner._check_eliminates();
        }

        DrawDefaultInspector();
    }
}
#endif
