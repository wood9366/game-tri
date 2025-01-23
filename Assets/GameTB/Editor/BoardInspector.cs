using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class GUIColor : IDisposable
{
    private Color _color = Color.white;

    public GUIColor(Color color)
    {
        _color = GUI.color;
        GUI.color = color;
    }

    public void Dispose()
    {
        GUI.color = _color;
    }
}

[CustomEditor(typeof(Board))]
class BoardInspector : Editor
{
    public override void OnInspectorGUI()
    {
        using (new GUIColor(_is_edit_mode ? Color.green : Color.white))
        {
            if (GUILayout.Button("编辑"))
                _is_edit_mode = !_is_edit_mode;
        }

        if (_is_edit_mode)
        {
            using (new GUIColor(Color.red))
            {
                if (GUILayout.Button("清除所有"))
                {
                    if (_owner._root_blocks != null)
                    {
                        for (int i = _owner._root_blocks.childCount - 1; i >= 0; i--)
                        {
                            var child = _owner._root_blocks.GetChild(i);
                            GameObject.DestroyImmediate(child.gameObject);
                        }
                    }

                    _owner._cells.Clear();
                }
            }

            var options = _block_types_data._blocks.Select(x => x._get_name()).ToList();
            options.Insert(0, "none");
            _sel_block_tid = EditorGUILayout.Popup(new GUIContent("设置方块"), _sel_block_tid, options.ToArray());
        }

        DrawDefaultInspector();
    }

    private Board _owner;
    private Vector2Int _hover_cell_pos = new Vector2Int(-1, -1);
    private bool _is_edit_mode = false;
    private int _sel_block_tid = 0;
    private BlockTypesData _block_types_data;

    private void OnEnable()
    {
        _owner = (Board)target;
        _block_types_data = AssetDatabase.LoadAssetAtPath<BlockTypesData>("Assets/GameTB/BlockTypesData.asset");
    }

    private void OnSceneGUI()
    {
        for (int x = 0; x < _owner._cell_diminsion.x; x++)
        {
            for (int y = 0; y < _owner._cell_diminsion.y; y++)
            {
                var cell_pos = new Vector2Int(x, y);
                Handles.color = Color.gray;
                Handles.DrawWireCube(_cal_cell_wpos(cell_pos), _owner._cell_size);
            }
        }

        for (int x = 0; x < _owner._cell_diminsion.x; x++)
        {
            for (int y = 0; y < _owner._cell_diminsion.y; y++)
            {
                var cell_pos = new Vector2Int(x, y);
                if (_owner._cells.TryGetValue(cell_pos, out var cell))
                {
                    Handles.color = Color.green;
                    var pos = _cal_cell_wpos(cell_pos);
                    Handles.DrawWireCube(pos, _owner._cell_size);
                }
            }
        }

        if (!_is_edit_mode)
            return;

        Event evt = Event.current;

        Vector2 size = _owner._cell_diminsion * _owner._cell_size;
        Vector2 lt = new Vector2(-size.x * 0.5f, size.y * 0.5f);

        if (evt.type == EventType.MouseMove || evt.type == EventType.MouseDrag)
        {
            Vector2Int cell_pos = _cal_cell_pos(evt.mousePosition);

            if (_is_valid_cell_pos(cell_pos))
                _hover_cell_pos = cell_pos;
            else
                _hover_cell_pos = new Vector2Int(-1, -1);
        }

        if (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag)
        {
            Vector2Int cell_pos = _cal_cell_pos(evt.mousePosition);

            if (_is_valid_cell_pos(cell_pos))
            {
                if (evt.button == 0)
                {
                    if (evt.CtrlOrCmd())
                    {
                        if (_owner._cells.TryGetValue(cell_pos, out var block))
                        {
                            if (block != null)
                                GameObject.DestroyImmediate(block.gameObject);
                            _owner._cells.Remove(cell_pos);
                        }
                    }
                    else
                    {
                        if (_owner._cells.TryGetValue(cell_pos, out var block))
                        {
                            if (block == null || block.tid != _sel_block_tid)
                            {
                                if (block != null)
                                    GameObject.DestroyImmediate(block.gameObject);

                                _owner._cells[cell_pos] = _owner._create_block(cell_pos, _sel_block_tid);
                            }
                        }
                        else
                        {
                            _owner._cells.Add(cell_pos, _owner._create_block(cell_pos, _sel_block_tid));
                        }
                    }
                }
                else if (evt.button == 1)
                {
                    if (_owner._cells.TryGetValue(cell_pos, out var block))
                        _sel_block_tid = block.tid;
                }

                _mark_level_data_change();
                _repaint_scene_view();

                evt.Use();
            }
        }

        if (_is_valid_cell_pos(_hover_cell_pos))
        {
            Handles.color = Color.white;
            var pos = _cal_cell_wpos(_hover_cell_pos);
            Handles.DrawWireCube(pos, _owner._cell_size);
        }
    }

    private void _mark_level_data_change()
    {
        EditorUtility.SetDirty(target);
    }

    private void _repaint_scene_view()
    {
        SceneView.lastActiveSceneView.Repaint();
    }

    private bool _is_valid_cell_pos(Vector2Int cell_pos)
    {
        return cell_pos.x >= 0 && cell_pos.x < _owner._cell_diminsion.x &&
            cell_pos.y >= 0 && cell_pos.y < _owner._cell_diminsion.y;
    }

    private Vector2 _cal_cell_wpos(Vector2Int cell_pos)
    {
        return _owner._cal_cell_wpos(cell_pos);
    }

    private Vector2Int _cal_cell_pos(Vector2 mouse_pos)
    {
        return _owner._cal_cell_pos(HandleUtility.GUIPointToWorldRay(mouse_pos).origin);
    }
}
