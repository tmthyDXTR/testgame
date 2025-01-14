﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

using UnityEngine.Events;

public class CheatPanel : MonoBehaviour
{
    private Button fireButton;
    private Button foodButton;
    private Button sporesButton;
    private Button woodButton;
    private Button enemyButton;

    GameHandler gameHandler;

    public Storage storage;
    public EnemySpawner enemySpawner;
    private SelectionManager selectionManager;

    [SerializeField] private WoodLogs[] woodStorage;
    public GameObject prefab;

    void Awake()
    {
        //this.gameObject.SetActive(false);
        gameHandler = GameObject.Find("GameHandler").GetComponent<GameHandler>();
        storage = GameObject.Find("FoodStorage").GetComponent<Storage>();
        selectionManager = GameObject.Find("SelectionManager").GetComponent<SelectionManager>();

        //fireButton = GameObject.Find("FireCheat").gameObject.GetComponent<Button>();
        //fireButton.onClick.AddListener(AddRemoveFire);

        foodButton = GameObject.Find("FoodCheat").gameObject.GetComponent<Button>();
        foodButton.onClick.AddListener(AddFood);

        sporesButton = GameObject.Find("SporesCheat").gameObject.GetComponent<Button>();
        sporesButton.onClick.AddListener(AddSpores);

        woodStorage = GameObject.Find("WoodStorage").GetComponentsInChildren<WoodLogs>();
        woodButton = GameObject.Find("WoodCheat").gameObject.GetComponent<Button>();
        woodButton.onClick.AddListener(AddWood);

        enemySpawner = enemySpawner.gameObject.GetComponent<EnemySpawner>();
        enemyButton = GameObject.Find("SpawnEnemy").gameObject.GetComponent<Button>();
        enemyButton.onClick.AddListener(AddEnemy);
    }

    private void AddRemoveFire()
    {
        gameHandler.AddFireLife(1);
    }

    private void AddFood()
    {
        storage.Store(StoredItem.Shrooms);
    }

    private void AddSpores()
    {
        storage.Store(StoredItem.Spores);
    }

    private void AddWood()
    {
        foreach (WoodLogs storage in woodStorage)
        {
            if (storage.currentAmount < storage.woodCapacity)
            {
                storage.currentAmount += 1;
                gameHandler.AddWoodToStock(1);
                break;
            }
        }
    }

    private void AddEnemy()
    {
        enemySpawner.SpawnEnemy(1, prefab);
        Debug.Log("Enemy Spawned");
    }

    void Update()
    {
        
    }
}
