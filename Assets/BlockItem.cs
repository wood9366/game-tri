using UnityEngine;

public class BlockItem : MonoBehaviour
{
    private int _id;
    private int _type;

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

    public void Init(int id, Game.BlockType type)
    {
        _id = id;
        _type = type.type;
        _sr.color = type.color;
    }
}
