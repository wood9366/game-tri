using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class BlockItem : MonoBehaviour
{
    private int _id;
    private int _type;
    private int _x;
    private int _y;

    private SpriteRenderer __sr;

    private SpriteRenderer _sr
    {
        get
        {
            if (__sr == null)
                __sr = GetComponent<SpriteRenderer>();

            return __sr;
        }
    }


    public int type => _type;
    public int x => _x;
    public int y => _y;

    public void Init(int id, int x, int y, Game.BlockType type)
    {
        _id = id;
        _x = x;
        _y = y;
        _type = type.type;
        _sr.color = type.color;
    }
}
