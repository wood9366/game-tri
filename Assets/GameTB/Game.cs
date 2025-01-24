using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public GameObject _template_btn_level;

    public GameObject _menu_main;
    public GameObject _menu_game;

    public Button _level1;
    public Button _level2;
    public Button _btn_back;

    private GameObject _level;

    void Start()
    {
        _menu_main.SetActive(true);
        _menu_game.SetActive(false);

        _level1.onClick.AddListener(() => _load_level(1));
        _level2.onClick.AddListener(() => _load_level(2));
        _btn_back.onClick.AddListener(_exit_level); 
    }

    private void _exit_level()
    {
        GameObject.Destroy(_level);
        _menu_main.SetActive(true);
        _menu_game.SetActive(false);
    }

    private async void _load_level(int id)
    {
        _menu_main.SetActive(false);
        _menu_game.SetActive(true);

        var h = Addressables.LoadAssetAsync<GameObject>($"Assets/GameTB/Levels/Level_{id}.prefab");
        await h.Task;

        _level = GameObject.Instantiate(h.Result);
        _level.name = "_level";
    }
}
