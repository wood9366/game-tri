using OdinSerializer;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Drawing.Text;

public enum EBlockType
{
    none = 0,
    brick,
    rocket,
    bomb,
    sphere,

    num
}

public enum ELinkType
{
    none = 0,
    red,
    blue,
    green,
    yellow,

    num
}

public enum ERocketType
{
    none = 0,
    horizental,
    vertical,

    num
}

[System.Serializable]
public class BlockTypeData
{
    public int id;

    [OnValueChanged("_ui_on_type_change")]
    [Dropdown("_ui_get_types")]
    public int type;

    [OnValueChanged("_ui_on_subtype_change")]
    [Dropdown("_ui_get_subtypes")]
    public int subtype;

    public int num_eliminate = 0;

    public Color color = Color.white;

    public Sprite shape;
    public Sprite icon;

    private void _ui_on_type_change()
    {
        _ui_refresh_color();
    }

    private void _ui_on_subtype_change()
    {
        _ui_refresh_color();
    }

    private void _ui_refresh_color()
    {
        if (type == (int)EBlockType.brick)
        {
            if (subtype == (int)ELinkType.red)
                color = Color.red;
            else if (subtype == (int)ELinkType.green)
                color = Color.green;
            else if (subtype == (int)ELinkType.blue)
                color = Color.blue;
            else if (subtype == (int)ELinkType.yellow)
                color = Color.yellow;
            else
                color = Color.white;
        }
        else
            color = Color.white;
    }

    private DropdownList<int> _ui_get_types()
    {
        var list = new DropdownList<int>();
        foreach (var e in System.Enum.GetValues(typeof(EBlockType)))
        {
            if ((int)e < (int)EBlockType.num)
                list.Add(e.ToString(), (int)e);
        }
        return list;
    }

    private DropdownList<int> _ui_get_subtypes()
    {
        var list = new DropdownList<int>();
        if (type == (int)EBlockType.brick || type == (int)EBlockType.sphere)
        {
            foreach (var e in System.Enum.GetValues(typeof(ELinkType)))
            {
                if ((int)e < (int)ELinkType.num)
                    list.Add(e.ToString(), (int)e);
            }
        }
        else if (type == (int)EBlockType.rocket)
        {
            foreach (var e in System.Enum.GetValues(typeof(ERocketType)))
            {
                if ((int)e < (int)ERocketType.num)
                    list.Add(e.ToString(), (int)e);
            }
        }
        else
            list.Add("none", 0);
        return list;
    }

    public string _get_name()
    {
        var name = $"{id}_{(EBlockType)type}";

        if (type == (int)EBlockType.brick || type == (int)EBlockType.sphere)
            name += $"_{(ELinkType)subtype}";
        else if (type == (int)EBlockType.rocket)
            name += $"_{(ERocketType)subtype}";

        return name;
    }
}

[CreateAssetMenu(fileName = "BlockTypesData", menuName = "Game/Block Types Data")]
public class BlockTypesData : SerializedScriptableObject
{
    public List<BlockTypeData> _blocks = new List<BlockTypeData>();
}
