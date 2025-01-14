﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class AddRemoveShroomerButton : MonoBehaviour, IPointerClickHandler
{
    public JobManager jobManager;
    public Window_WorkerBank window_WorkerBank;
    public UnityEvent leftClick;
    public UnityEvent middleClick;
    public UnityEvent rightClick;

    void Awake()
    {
        jobManager = GameObject.Find("Workers").GetComponent<JobManager>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log("Left click");
            leftClick.Invoke();
            jobManager.MoveWorkerToJob("Unemployed", "Shroomer");
        }

        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            middleClick.Invoke();
            Debug.Log("Middle click");
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("Right click");
            rightClick.Invoke();
            jobManager.MoveWorkerToJob("Shroomer", "Unemployed");
        }
    }
}
