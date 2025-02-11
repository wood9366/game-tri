//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;
using System.Linq;
using Codice.Client.Common.GameUI;

public class LevelEditorWindow : EditorWindow
{
    [MenuItem("Window/Game/Level Editor")]
    private static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("关卡编辑器");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private LevelData _level_data;

    private void OnGUI()
    {
        GUILayout.BeginVertical(GUILayout.Width(500));

        _level_data = (LevelData)EditorGUILayout.ObjectField("关卡数据", _level_data, typeof(LevelData), false);

        if (_level_data != null)
        {
            _ui_cell_size();
            _ui_cell_diminsion();
        }

        GUILayout.EndVertical();
    }

    private void _ui_cell_size()
    {
        float width = _level_data.cell_size.x;
        float height = _level_data.cell_size.y;

        EditorGUI.BeginChangeCheck();

        height = EditorGUILayout.FloatField("格子宽度", height);
        width = EditorGUILayout.FloatField("格子高度", width);

        if (EditorGUI.EndChangeCheck())
        {
            _level_data.cell_size = new Vector2(width, height);
            _mark_level_data_change();
            _repaint_scene_view();
        }
    }

    private void _ui_cell_diminsion()
    {
        int num_cols = _level_data.cell_diminsion.x;
        int num_rows = _level_data.cell_diminsion.y;

        EditorGUI.BeginChangeCheck();

        num_rows = EditorGUILayout.IntField("行数", num_rows);
        num_cols = EditorGUILayout.IntField("列数", num_cols);

        if (EditorGUI.EndChangeCheck())
        {
            _level_data.cell_diminsion = new Vector2Int(num_cols, num_rows);
            _mark_level_data_change();
            _repaint_scene_view();
        }
    }

    private void _mark_level_data_change()
    {
        EditorUtility.SetDirty(_level_data);
    }

    private void _repaint_scene_view()
    {
        SceneView.lastActiveSceneView.Repaint();
    }

    private bool _is_mouse_pressed = false;
    private Vector2Int _hover_cell_pos = new Vector2Int(-1, -1);

    private void OnSceneGUI(SceneView view)
    {
        if (_level_data == null)
            return;

        Event evt = Event.current;

        Vector2 size = _level_data.cell_diminsion * _level_data.cell_size;
        Vector2 lt = new Vector2(-size.x * 0.5f, size.y * 0.5f);


        if (evt.type == EventType.MouseMove || evt.type == EventType.MouseDrag)
        {
            Vector2Int cell_pos = _cal_cell_pos(evt.mousePosition);

            if (_is_valid_cell_pos(cell_pos))
                _hover_cell_pos = cell_pos;
            else
                _hover_cell_pos = new Vector2Int(-1, -1);
        }

        if (evt.type == EventType.MouseDown)
            _is_mouse_pressed = true;
        else if (evt.type == EventType.MouseUp)
            _is_mouse_pressed = false;

        if (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag)
        {
            Vector2Int cell_pos = _cal_cell_pos(evt.mousePosition);

            if (_is_valid_cell_pos(cell_pos))
            {

                if (evt.button == 0)
                {
                    if (!_level_data.cells.TryGetValue(cell_pos, out var cell))
                    {
                        cell = new LevelData.CellData();
                        _level_data.cells.Add(cell_pos, cell);
                    }
                    cell.block_tid = 1;
                }
                else if (evt.button == 1)
                {
                    _level_data.cells.Remove(cell_pos);
                }

                _mark_level_data_change();
                _repaint_scene_view();

                evt.Use();
            }
        }

        for (int x = 0; x < _level_data.cell_diminsion.x; x++)
        {
            for (int y = 0; y < _level_data.cell_diminsion.y; y++)
            {
                var cell_pos = new Vector2Int(x, y);
                Handles.color = Color.gray;
                Handles.DrawWireCube(_cal_cell_wpos(cell_pos), _level_data.cell_size);
            }
        }


        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/GameTB/blocks.png");
        var sp = assets.Where(x => x is Sprite sp && sp.name == "blocks_0").Select(x => (Sprite)x).FirstOrDefault();

        for (int x = 0; x < _level_data.cell_diminsion.x; x++)
        {
            for (int y = 0; y < _level_data.cell_diminsion.y; y++)
            {
                var cell_pos = new Vector2Int(x, y);
                if (_level_data.cells.TryGetValue(cell_pos, out var cell))
                {
                    Handles.color = Color.green;
                    var pos = _cal_cell_wpos(cell_pos);
                    Handles.DrawWireCube(pos, _level_data.cell_size);
                    _draw_sprite(pos, _level_data.cell_size, sp);
                }
            }
        }

        if (_is_valid_cell_pos(_hover_cell_pos))
        {
            Handles.color = Color.white;
            var pos = _cal_cell_wpos(_hover_cell_pos);
            Handles.DrawWireCube(pos, _level_data.cell_size);
        }
    }

    private void _draw_sprite(Vector2 pos, Vector2 size, Sprite sp)
    {
        Handles.BeginGUI();
        GUI.DrawTextureWithTexCoords(_cal_world_rect(pos, size), sp.texture, _cal_sprite_uv_rect(sp));
        Handles.EndGUI();
    }

    private Rect _cal_sprite_uv_rect(Sprite sp)
    {
        return new Rect()
        {
            x = sp.rect.x / sp.texture.width,
            y = sp.rect.y / sp.texture.height,
            width = sp.rect.width / sp.texture.width,
            height = sp.rect.height / sp.texture.height,
        };
    }

    private Rect _cal_world_rect(Vector2 center, Vector2 size)
    {
        var tr = center + 0.5f * size;
        var bl = center - 0.5f * size;

        var str = HandleUtility.WorldToGUIPoint(tr);
        var sbl = HandleUtility.WorldToGUIPoint(bl);

        return new Rect(sbl, str - sbl);
    }

    private bool _is_valid_cell_pos(Vector2Int cell_pos)
    {
        return cell_pos.x >= 0 && cell_pos.x < _level_data.cell_diminsion.x &&
            cell_pos.y >= 0 && cell_pos.y < _level_data.cell_diminsion.y;
    }

    private Vector2 _cal_cell_wpos(Vector2Int cell_pos)
    {
        Vector2 size = _level_data.cell_diminsion * _level_data.cell_size;
        return new Vector2(-size.x * 0.5f, size.y * 0.5f) +
            new Vector2((cell_pos.x + 0.5f) * _level_data.cell_size.x,
                        -(cell_pos.y + 0.5f) * _level_data.cell_size.y);
    }

    private Vector2Int _cal_cell_pos(Vector2 mouse_pos)
    {
        Vector2 size = _level_data.cell_diminsion * _level_data.cell_size;
        Vector2 lt = new Vector2(-size.x * 0.5f, size.y * 0.5f);
        Vector2 wpos = HandleUtility.GUIPointToWorldRay(mouse_pos).origin;

        Vector2Int cell_pos = new Vector2Int(
            (int)((wpos.x - lt.x) / _level_data.cell_size.x),
            (int)((lt.y - wpos.y) / _level_data.cell_size.y));
        return cell_pos;
    }
}
