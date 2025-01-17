using System.Collections.Generic;
using Unity.VisualScripting;
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

        _generate_blocks();
    }

    private void _clear_blocks()
    {
        foreach (var item in _block_items.Values)
            GameObject.Destroy(item.gameObject);

        _block_items.Clear();

        for (int x = 0; x < _blocks.GetLength(0); x++)
            for (int y = 0; y < _blocks.GetLength(1); y++)
                _blocks[x, y].id = 0;
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
                var block = _blocks[x, y] = new Block() { id = _next_block_id(), x = x, y = y, type = block_type.type };

                var block_item = _create_block_item(block.id, block_type, block.x, block.y);
                block_item.transform.position = _get_cell_pos(x, y);
                _block_items.Add(block.id, block_item);
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
    }

    private void _draw_cell(int x, int y)
    {
        Gizmos.DrawWireCube(_get_cell_pos(x, y), Vector2.one * _cell_size);
    }
}

#if UNITY_EDITOR
class GameInspector : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif
