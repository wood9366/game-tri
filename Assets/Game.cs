using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    private GameObject _template_block_item;

    [SerializeField]
    private GameObject _template_block_item_select;

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

    private int[,] _cell2block;
    private Dictionary<int, Block> _blocks = new Dictionary<int, Block>();
    private Transform _blocks_root;
    private Dictionary<int, BlockItem> _block_items = new Dictionary<int, BlockItem>();
    private GameObject _block_item_select;
    private int _start_block_id = 1;
    private int _selected_block_id = 0;

    private int _next_block_id() => _start_block_id++;
    private int GetNumCols() => _num_cols;
    private int GetNumRows() => _rows.Count;

    void Start()
    {
        _cell2block = new int[_num_cols, _rows.Count];
        _clear_blocks();

        _block_item_select = GameObject.Instantiate(_template_block_item_select, transform);
        _block_item_select.transform.SetParent(transform);
        _block_item_select.transform.localScale = Vector3.one * _cell_size;
        _block_item_select.name = "_block_selected";

        _blocks_root = new GameObject("_blocks").transform;
        _blocks_root.SetParent(transform);

        _block_item_select.SetActive(false);
        _selected_block_id = 0;

        GameStart();
    }

    enum GameStatus
    {
        none = 0,
        play,
        swap,
        check,
        eliminate,
        drop,
        fill
    }

    private GameStatus _game_status = GameStatus.none;
    private float _status_timer = 0;

    private void _set_game_status(GameStatus game_status, bool force = false)
    {
        if (game_status != _game_status || force)
        {
            _status_timer = 0;
            _game_status = game_status;

            if (_game_status == GameStatus.check)
                CheckEliminate();
            else if (_game_status == GameStatus.eliminate)
                DoEliminate();
            else if (_game_status == GameStatus.drop)
                DoDrop();
            else if (_game_status == GameStatus.fill)
                DoFill();
            else if (_game_status == GameStatus.swap)
                _do_swap(_swap_from, _swap_to);
        }
    }

    private void _do_swap(int from, int to)
    {
        _swap_block(from, to);

        if (_block_items.TryGetValue(from, out var block_item) &&
            _blocks.TryGetValue(from, out var block))
        {
            block_item.transform.position = _get_cell_pos(block.x, block.y);
        }

        if (_block_items.TryGetValue(to, out block_item) &&
            _blocks.TryGetValue(to, out block))
        {
            block_item.transform.position = _get_cell_pos(block.x, block.y);
        }

        CheckEliminate();
    }

    private bool _has_empty_cell()
    {
        for (int x = 0; x < GetNumCols(); x++)
        {
            for (int y = 0; y < GetNumRows(); y++)
            {
                if (_cell2block[x, y] == 0)
                    return true;
            }
        }

        return false;
    }

    private bool _has_eliminate()
    {
        return _row_eliminates.Count > 0 || _col_eliminates.Count > 0;
    }

    private void _update_status()
    {
        float duration = 1;

        _status_timer += Time.deltaTime;

        if (_game_status == GameStatus.check)
        {
            if (_status_timer > duration)
            {
                if (_has_eliminate())
                    _set_game_status(GameStatus.eliminate);
                else if (_has_empty_cell())
                    _set_game_status(GameStatus.fill);
                else
                    _set_game_status(GameStatus.play);
            }
        }
        else if (_game_status == GameStatus.eliminate)
        {
            if (_status_timer > duration)
                _set_game_status(GameStatus.drop);
        }
        else if (_game_status == GameStatus.drop)
        {
            if (_status_timer > duration)
                _set_game_status(GameStatus.check);
        }
        else if (_game_status == GameStatus.fill)
        {
            if (_status_timer > duration)
                _set_game_status(GameStatus.check);
        }
        else if (_game_status == GameStatus.swap)
        {
            if (_status_timer > duration)
            {
                if (_has_eliminate())
                    _set_game_status(GameStatus.eliminate);
                else
                {
                    _do_swap(_swap_from, _swap_to);
                    _set_game_status(GameStatus.play);
                }
            }
        }
    }

    internal void GameStart()
    {
        GenerateBlocks();
        GenerateBlockItems();

        _set_game_status(GameStatus.check);
    }

    private int _swap_from = 0;
    private int _swap_to = 0;

    void _on_click_cell(int x, int y)
    {
        var select_block_id = _cell2block[x, y];

        if (select_block_id != 0)
        {
            if (_selected_block_id == 0)
            {
                _selected_block_id = select_block_id;
            }
            else if (_selected_block_id == select_block_id)
            {
                _selected_block_id = 0;
            }
            else
            {
                var block = _blocks[_selected_block_id];

                if ((x == block.x && (y == block.y - 1 || y == block.y + 1)) ||
                    (y == block.y && (x == block.x - 1 || x == block.x + 1)))
                {
                    _swap_from = _selected_block_id;
                    _swap_to = select_block_id;
                    _set_game_status(GameStatus.swap);
                    _selected_block_id = 0;
                }
            }

            _block_item_select.SetActive(_selected_block_id > 0);
            if (_selected_block_id > 0)
            {
                if (_blocks.TryGetValue(_selected_block_id, out var block))
                    _block_item_select.transform.position = _get_cell_pos(block.x, block.y);
            }
        }
    }

    private void _clear_block_items()
    {
        for (int i = _blocks_root.childCount - 1; i >= 0; i--)
            GameObject.Destroy(_blocks_root.GetChild(i).gameObject);

        _block_items.Clear();
    }

    private void _clear_blocks()
    {
        for (int x = 0; x < GetNumCols(); x++)
            for (int y = 0; y < GetNumRows(); y++)
                _cell2block[x, y] = 0;

        _blocks.Clear();
    }

    internal void DoFill()
    {
        _tmp_blocks.Clear();

        // find empty blocks on each col
        for (int x = 0; x < GetNumCols(); x++)
        {
            for (int y = 0; y < GetNumRows(); y++)
            {
                var block_id = _cell2block[x, y];

                if (block_id != 0)
                    break;

                var block = _create_block(x, y);

                _tmp_blocks.Add(block.id);
            }
        }

        foreach (var block_id in _tmp_blocks)
        {
            if (!_blocks.TryGetValue(block_id, out var block))
                continue;

            var block_type = _block_types.Find(x => x.type == block.type);
            _create_block_item(block.id, block_type, block.x, block.y);
        }
    }
    
    private Block _create_block(int x, int y)
    {
        var block_type = _block_types[Random.Range(0, _block_types.Count)];
        var block = new Block()
        {
            id = _next_block_id(),
            type = block_type.type,
            x = x,
            y = y,
        };

        _cell2block[block.x, block.y] = block.id;
        _blocks.Add(block.id, block);

        return block;
    }

    internal void DoDrop()
    {
        _tmp_blocks.Clear();
        
        // cal drop blocks on each col
        for (int x = 0; x < GetNumCols(); x++)
        {
            int num_empty = 0;
            int y = GetNumRows() - 1;

            // from bottom to top check link empty block
            while (y >= 0)
            {
                if (_cell2block[x, y] == 0)
                    num_empty++;
                else if (num_empty > 0)
                {
                    var block_id = _cell2block[x, y];
                    var to_y = y + num_empty;

                    _move_block(block_id, x, to_y);
                    _tmp_blocks.Add(block_id);
                }

                y--;
            }
        }

        // update drop block items pos
        foreach (var block_id in _tmp_blocks)
        {
            if (_block_items.TryGetValue(block_id, out var block_item) &&
                _blocks.TryGetValue(block_id, out var block))
            {
                block_item.transform.position = _get_cell_pos(block.x, block.y);
            }
        }
    }

    private void _swap_block(int from, int to)
    {
        var block_from = _blocks[from];
        var block_to = _blocks[to];

        _cell2block[block_from.x, block_from.y] = block_to.id;
        _cell2block[block_to.x, block_to.y] = block_from.id;

        var block_from_x = block_from.x;
        var block_from_y = block_from.y;

        block_from.x = block_to.x;
        block_from.y = block_to.y;
        block_to.x = block_from_x;
        block_to.y = block_from_y;
    }

    private void _move_block(int id, int x, int y)
    {
        if (!_blocks.TryGetValue(id, out var block))
            return;

        _cell2block[block.x, block.y] = 0;

        block.x = x;
        block.y = y;
        _cell2block[block.x, block.y] = block.id;
    }

    static private readonly int NUM_MIN_LINK = 3;

    private List<Eliminate> _row_eliminates = new List<Eliminate>();
    private List<Eliminate> _col_eliminates = new List<Eliminate>();
    private HashSet<int> _tmp_blocks = new HashSet<int>();

    internal void DoEliminate()
    {
        // remove eliminate blocks
        _tmp_blocks.Clear();

        foreach (var e in _row_eliminates)
            foreach (var c in e.cells)
                _tmp_blocks.Add(_cell2block[c.x, c.y]);

        _row_eliminates.Clear();

        foreach (var e in _col_eliminates)
            foreach (var c in e.cells)
                _tmp_blocks.Add(_cell2block[c.x, c.y]);

        _col_eliminates.Clear();

        // remove eliminate block items
        foreach (var id in _tmp_blocks)
        {
            if (_blocks.TryGetValue(id, out var block))
            {
                _blocks.Remove(block.id);
                _cell2block[block.x, block.y] = 0;
            }

            if (_block_items.TryGetValue(id, out var item))
            {
                _block_items.Remove(id);
                GameObject.Destroy(item.gameObject);
            }
        }

        _tmp_blocks.Clear();
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
            var block_id = is_by_row ? _cell2block[j, i] : _cell2block[i, j];

            if (block_id == 0)
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

                var check_block_id = is_by_row ? _cell2block[j + k, i] : _cell2block[i, j + k];

                if (check_block_id == 0)
                    break;

                if (!_blocks.TryGetValue(block_id, out var block))
                    break;

                if (!_blocks.TryGetValue(check_block_id, out var check_block))
                    break;

                if (block.type != check_block.type)
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

        for (int x = 0; x < GetNumCols(); x++)
            for (int y = 0; y < GetNumRows(); y++)
                _create_block(x, y);
    }

    internal void GenerateBlockItems()
    {
        _clear_block_items();

        foreach (var block in _blocks.Values)
        {
            var block_type = _block_types.Find(x => x.type == block.type);

            if (block_type != null)
                _create_block_item(block.id, block_type, block.x, block.y);
        }
    }

    private BlockItem _create_block_item(int id, BlockType type, int x, int y)
    {
        var obj = GameObject.Instantiate(_template_block_item, _blocks_root);
        obj.name = $"_block_{id}_{type.type}";
        obj.transform.localScale = Vector3.one * _cell_size * 0.9f;
        obj.transform.position = _get_cell_pos(x, y);

        var block_item = obj.GetComponent<BlockItem>();
        block_item.Init(id, type);

        _block_items.Add(id, block_item);

        return block_item;
    }

    private Vector2 _get_cell_pos(int x, int y)
    {
        return new Vector2(
            _origin.x - (GetNumCols() * _cell_size) / 2.0f + (x + 0.5f) * _cell_size,
            _origin.y + (GetNumRows() * _cell_size) / 2.0f - (y + 0.5f) * _cell_size);
    }

    private void Update()
    {
        _update_status();
        
        if (_game_status == GameStatus.play && Input.GetMouseButtonDown(0))
        {
            Vector2 wpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            var lt = new Vector2(
                _origin.x - (GetNumCols() * _cell_size) / 2.0f,
                _origin.y + (GetNumRows() * _cell_size) / 2.0f);

            var pos = wpos - lt;

            var x = (int)(pos.x / _cell_size);
            var y = (int)(-pos.y / _cell_size);

            if (x >= 0 && x < GetNumCols() && y >= 0 && y < GetNumRows())
            {
                _on_click_cell(x, y);
            }
        }
    }

#if UNITY_EDITOR
    public void Save()
    {
        StringBuilder str = new StringBuilder();
        bool first = true;
        foreach (var block in _blocks.Values)
        {
            if (!first)
                str.Append("|");
            str.Append($"{block.id},{block.x},{block.y},{block.type}");
            first = false;
        }

        PlayerPrefs.SetString("blocks", str.ToString());
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey("blocks"))
            return;

        var str = PlayerPrefs.GetString("blocks");

        //Debug.Log(str);

        _clear_blocks();

        int max_block_id = 0;
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
                var block = new Block()
                {
                    id = id,
                    x = x,
                    y = y,
                    type = type
                };

                _blocks.Add(block.id, block);
                _cell2block[block.x, block.y] = block.id;

                if (id > max_block_id)
                    max_block_id = id;
            }
        }

        _start_block_id = max_block_id + 1;

        _set_game_status(GameStatus.check, true);
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
            var block_id = _cell2block[x, y];
            if (block_id == 0)
                return;

            if (!_blocks.TryGetValue(block_id, out var block))
                return;

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
