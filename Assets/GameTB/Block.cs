using DG.Tweening;
using OdinSerializer;
using UnityEngine;
using UnityEngine.Assertions;
using Cysharp.Threading.Tasks;
using System.Linq;
using TMPro;
using JetBrains.Annotations;

public class Block : SerializedMonoBehaviour
{
    public BlockTypesData _block_types_data;
    public SpriteRenderer _border;
    public SpriteRenderer _shape;

    public int _tid = 0;
    public BlockTypeData _data;

    public int tid => _tid;
    public BlockTypeData data => _data;

    public void Init(int tid)
    {
        _data = _block_types_data._blocks.Find(x => x.id == tid);
        Assert.IsNotNull(_data);

        _tid = _data.id;
        _border.color = _shape.color = _data.color;
        _border.sprite = _data.icon;
        _shape.sprite = _data.shape;

        _shape.gameObject.SetActive(_data.type == (int)EBlockType.brick);
    }

    public async UniTask Eliminate(float duration, float delay = 0)
    {
        transform.DOKill();
        var dt = transform.DOScaleY(0, duration)
            .SetDelay(delay)
            .OnComplete(() => GameObject.Destroy(gameObject));

        await dt.AsyncWaitForCompletion();
    }

    public void SetBaseShape()
    {
        _shape.sprite = _data.shape;
    }

    public void SetShape(EBlockType type)
    {
        var block_type_data = _block_types_data._blocks.Where(x => x.type == (int)type).FirstOrDefault();

        if (block_type_data == null)
            return;

        _shape.sprite = block_type_data.shape;
    }

    public void Shake()
    {
        transform.DOKill();
        transform.localRotation = Quaternion.identity;
        transform.DOShakeRotation(0.3f, new Vector3(0,0,90));
    }

    public async UniTask Drop(Vector2 wpos)
    {
        transform.DOKill();
        var dt = transform.DOLocalMove(wpos, 5)
            .SetEase(Ease.OutBounce)
            .SetSpeedBased(true);

        await dt.AsyncWaitForCompletion();
    }
}
