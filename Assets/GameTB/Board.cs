using Cysharp.Threading.Tasks;
using DG.Tweening;
using OdinSerializer;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : SerializedMonoBehaviour
{
    public BlockTypesData _block_types_data;
    public GameObject _template_block;
    public Transform _root_blocks;
    public Vector2Int _cell_diminsion = new Vector2Int(10, 10);
    public Vector2 _cell_size = Vector2.one * 0.1f;
    public Dictionary<Vector2Int, Block> _cells = new Dictionary<Vector2Int, Block>();

    private void Start()
    {
        _col_top_cell_y.Clear();
        for (int x = 0; x < _cell_diminsion.x; x++)
        {
            for (int y = 0; y < _cell_diminsion.y; y++)
            {
                if (_cells.TryGetValue(new Vector2Int(x, y), out var block))
                {
                    _col_top_cell_y.Add(x, y);
                    break;
                }
            }
        }

        _check_all_linked_bricks();
        _update_bricks_shape();
    }

    private Dictionary<int, int> _col_top_cell_y = new Dictionary<int, int>();

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 wpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var cell_pos = _cal_cell_pos(wpos);

            if (_cells.TryGetValue(cell_pos, out var block))
                _on_click_cell(cell_pos);
        }
    }

    private void _on_click_cell(Vector2Int cell_pos)
    {
        if (!_cells.TryGetValue(cell_pos, out var block))
            return;

        // eliminate block depends on its type and subtype
        if (block.data.type == (int)EBlockType.brick)
        {
            // check linked bricks by subtype
            _check_linked_bricks(cell_pos);
        }

        if (_block_linked_cells.Count > 1)
            _process_op_flow();
        else
            block.Shake();
    }

    private async void _process_op_flow()
    {
        List<UniTask> _tasks = new List<UniTask>();

        // eliminate those bricks
        _eliminate_cols.Clear();
        var time_eliminate_delay = Mathf.Min(_block_linked_cells.Count * 0.08f, 0.8f);
        for (int i = 0; i < _block_linked_cells.Count; i++)
        {
            var delay = Mathf.Clamp01((float)i / (_block_linked_cells.Count - 1)) * time_eliminate_delay;
            _tasks.Add(_eliminate_block(_block_linked_cells[i], delay));
        }

        await UniTask.WhenAll(_tasks);

        // drop block by cols
        _drop_cells.Clear();
        _col_new_block_num.Clear();
        for (int x = 0; x < _cell_diminsion.x; x++)
            _check_col_drop(x);

        // generate new block on eliminate columns
        for (int x = 0; x < _cell_diminsion.x; x++)
        {
            if (!_col_new_block_num.TryGetValue(x, out var n))
                continue;

            if (!_col_top_cell_y.TryGetValue(x, out var y_top))
                continue;

            int y = 0;
            while (y < n)
            {
                var cpos = new Vector2Int(x, y_top - n + y); ;
                var new_block = _create_block(cpos, _get_rand_brick_tid());
                var drop_pos = new Vector2Int(x, y_top + y);

                _cells[drop_pos] = new_block;
                _drop_cells.Add((new_block, drop_pos));

                y++;
            }
        }

        // drop cell on board to full empty cell
        _tasks.Clear();
        foreach (var drop_cell in _drop_cells)
            _tasks.Add(drop_cell.Item1.Drop(_cal_cell_wpos(drop_cell.Item2)));

        await UniTask.WhenAll(_tasks);

        // update bricks shape
        _check_all_linked_bricks();
        _update_bricks_shape();
    }

    private int _get_rand_brick_tid()
    {
        var tids = _block_types_data._blocks.Where(x => x.type == (int)EBlockType.brick)
            .Select(x => x.id)
            .ToList();

        return tids[Random.Range(0, tids.Count)];
    }

    private List<(Block, Vector2Int)> _drop_cells = new List<(Block, Vector2Int)>();
    private Dictionary<int, int> _col_new_block_num = new Dictionary<int, int>();

    private void _check_col_drop(int x)
    {
        int num_empty = 0;

        for (int y = _cell_diminsion.y - 1; y >= 0; y--)
        {
            var cpos = new Vector2Int(x, y);

            if (!_cells.TryGetValue(cpos, out var block))
                continue;

            if (block != null)
            {
                if (num_empty > 0)
                {
                    var drop_pos = new Vector2Int(x, y + num_empty);

                    _drop_cells.Add((block, drop_pos));

                    _cells[cpos] = null;
                    _cells[drop_pos] = block;
                }
            }
            else
                num_empty++;
        }

        _col_new_block_num.Add(x, num_empty);
    }

    private HashSet<int> _eliminate_cols = new HashSet<int>();

    private async UniTask _eliminate_block(Vector2Int cell_pos, float delay = 0)
    {
        if (_cells.TryGetValue(cell_pos, out var block))
        {
            await block.Eliminate(0.1f, delay);
            _eliminate_cols.Add(cell_pos.x);
            _cells[cell_pos] = null;
        }
    }

    private void _check_linked_bricks(Vector2Int cell_pos)
    {
        _block_linked_cells.Clear();
        _checked_cells.Clear();

        if (!_cells.TryGetValue(cell_pos, out var block))
            return;

        _check_linked_cell(cell_pos, block.data.subtype);
    }

    private List<List<Vector2Int>> _all_linked_bricks = new List<List<Vector2Int>>();

    private void _check_all_linked_bricks()
    {
        _all_linked_bricks.Clear();

        for (int x = 0; x < _cell_diminsion.x; x++)
        {
            for (int y = 0; y < _cell_diminsion.y; y++)
            {
                var cpos = new Vector2Int(x, y);

                if (!_cells.TryGetValue(cpos, out var block))
                    continue;

                if (_checked_cells.Contains(cpos))
                    continue;

                if (block == null)
                    continue;

                if (block.data.type != (int)EBlockType.brick)
                    continue;

                _block_linked_cells.Clear();
                _check_linked_bricks(cpos);

                if (_block_linked_cells.Count > 0)
                    _all_linked_bricks.Add(new List<Vector2Int>(_block_linked_cells));
            }
        }
    }

    private void _update_bricks_shape()
    {
        var block_types = _block_types_data._blocks
            .Where(x => x.type != (int)EBlockType.brick && x.num_eliminate > 0)
            .ToList();

        block_types.Sort((a, b) => b.num_eliminate.CompareTo(a.num_eliminate));

        foreach (var linked_bricks in _all_linked_bricks)
        {
            foreach (var block_type in block_types)
            {
                if (linked_bricks.Count >= block_type.num_eliminate)
                {
                    foreach (var cpos in linked_bricks)
                    {
                        if (_cells.TryGetValue(cpos, out var block))
                            block.SetShape((EBlockType)block_type.type);
                    }
                    break;
                }
                foreach (var cpos in linked_bricks)
                {
                    if (_cells.TryGetValue(cpos, out var block))
                        block.SetBaseShape();
                }
            }
        }
    }

    private void _check_linked_cell(Vector2Int cell_pos, int link_type)
    {
        if (!_cells.TryGetValue(cell_pos, out var block))
            return;

        if (block == null)
            return;

        if (_checked_cells.Contains(cell_pos))
            return;

        if (block.data.type != (int)EBlockType.brick)
            return;

        if (block.data.subtype != link_type)
            return;

        _checked_cells.Add(cell_pos);
        _block_linked_cells.Add(cell_pos);

        if (cell_pos.x > 0)
            _check_linked_cell(new Vector2Int(cell_pos.x - 1, cell_pos.y), link_type);
        if (cell_pos.x < _cell_diminsion.x - 1)
            _check_linked_cell(new Vector2Int(cell_pos.x + 1, cell_pos.y), link_type);
        if (cell_pos.y > 0)
            _check_linked_cell(new Vector2Int(cell_pos.x, cell_pos.y - 1), link_type);
        if (cell_pos.y < _cell_diminsion.y - 1)
            _check_linked_cell(new Vector2Int(cell_pos.x, cell_pos.y + 1), link_type);
    }

    private HashSet<Vector2Int> _checked_cells = new HashSet<Vector2Int>();
    private List<Vector2Int> _block_linked_cells = new List<Vector2Int>();

    public Vector2 _cal_cell_wpos(Vector2Int cell_pos)
    {
        Vector2 size = _cell_diminsion * _cell_size;
        return new Vector2(-size.x * 0.5f, size.y * 0.5f) +
            new Vector2((cell_pos.x + 0.5f) * _cell_size.x,
                        -(cell_pos.y + 0.5f) * _cell_size.y);
    }

    public Vector2Int _cal_cell_pos(Vector2 wpos)
    {
        Vector2 size = _cell_diminsion * _cell_size;
        Vector2 lt = new Vector2(-size.x * 0.5f, size.y * 0.5f);

        Vector2Int cell_pos = new Vector2Int(
            (int)((wpos.x - lt.x) / _cell_size.x),
            (int)((lt.y - wpos.y) / _cell_size.y));

        return cell_pos;
    }

    public Block _create_block(Vector2Int cell_pos, int tid)
    {
        if (tid == 0)
            return null;

        var obj = GameObject.Instantiate(_template_block, _root_blocks);
        obj.name = $"_block";
        obj.transform.localPosition = _cal_cell_wpos(cell_pos);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var block = obj.GetComponent<Block>();
        block.Init(tid);

        return block;
    }
}
