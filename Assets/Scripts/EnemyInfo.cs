﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyInfo : MonoBehaviour
{
    public Enemy enemy;
    [SerializeField] public string name;
    [SerializeField] public float health;
    [SerializeField] public float currentHealth;

    void Awake()
    {
        health = enemy.health;
        currentHealth = health;
        name = enemy.name;

    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log(name + " took " + amount + "Damage");
        if (currentHealth <= 0)
        {
            Death();
        }
    }
    private void Death()
    {
        Debug.Log(name + " died");
        SelectionManager selectionManager = GameObject.Find("SelectionManager").GetComponent<SelectionManager>();
        if (selectionManager.selection.Contains(this.gameObject))
        {
            selectionManager.DeselectAll();
            //selectionManager.selection.Clear();
        }
        GameObject bloodSplatter = Instantiate(Resources.Load("PS_BloodSplatter")) as GameObject;
        bloodSplatter.transform.position = this.transform.position + new Vector3(0, 0.4f, 0);
        Destroy(this.gameObject);
    }
    
}
