﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

public class WorkerAI : MonoBehaviour
{

    #region Variables

    public LayerMask groundLayer;
    public Vector3 nextNode;
    public Collider nextNodeCollider;
    public Collider Target;

    public string currentJob = "Unemployed";
    private float workTime = 0.0f;
    public int workSpeed = 1;
    public int inventorySize = 1;
    public int inventoryAmount = 0;
    public int inventoryWood = 0;
    public int inventoryStone = 0;
    public int animationSpeed = 2;

    [SerializeField] private State state;
    private enum State
    {
        JustSpawned,
        Idle,
        MovingToJobNode,
        WorkingOnJob,
        MovingToStorage,
        LoadToStorage,
        LoadToConstruction,
        TakingFromStorage,
        IdleAtFire,
        MovingToFire,
        ConstructionWork,
    }

    private float distanceToNode;
   // private float distanceToNearestNode = float.MaxValue;

    //SphereCaster for closest JobNode Search
    private Transform searchSphere;
    public Vector3 sphereOrigin;
    public float sphereRadius;
    public LayerMask sphereLayerMask;

    NavMeshAgent m_NavMeshAgent;
    Animator m_Animator;

    #endregion


    #region Target Search Methods

    // Target Search Method
    private Vector3 GetClosestInactiveNodeVector(string nodeTag)
    {
        sphereOrigin = transform.position;
        Collider[] nodeColliders = Physics.OverlapSphere(sphereOrigin, sphereRadius, sphereLayerMask);
        //Debug.Log("Found Node Colliders: " + nodeColliders.Length);

        float bestDistance = 999999.0f;
        
        foreach (Collider node in nodeColliders)
        {
            float distance = Vector3.Distance(node.ClosestPoint(nextNode), transform.position);
            if (node.gameObject.CompareTag(nodeTag))
            {                
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nextNode = node.ClosestPoint(nextNode);                    
                }
            }
        }                
        return nextNode;        
    }

    private Collider GetTarget(string nodeTag)
    {
        sphereOrigin = transform.position;
        Collider[] nodeColliders = Physics.OverlapSphere(sphereOrigin, sphereRadius, sphereLayerMask);

        float bestDistance = 999999.0f;

        foreach (Collider node in nodeColliders)
        {
            float distance = Vector3.Distance(node.ClosestPoint(nextNode), transform.position);
            if (node.gameObject.CompareTag("WorkActive"))
            {
                continue;
            }
            if (node.gameObject.CompareTag(nodeTag))
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    Target = node;
                }
        }
        Debug.Log("Found Inactive Target Collider");
        return Target;
    }

    private Collider GetNodeCollider(string nodeTag)
    {
        sphereOrigin = transform.position;
        Collider[] nodeColliders = Physics.OverlapSphere(sphereOrigin, sphereRadius, sphereLayerMask);
        Debug.Log("Colliders: " + nodeColliders.Length);

        float bestDistance = 999999.0f;

        foreach (Collider node in nodeColliders)
        {
            float distance = Vector3.Distance(node.ClosestPoint(nextNode), transform.position);

            if (node.gameObject.CompareTag(nodeTag))
            {
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nextNodeCollider = node;
                }
            }

                
        }
        return nextNodeCollider;
    }

    private int GetInactiveNodesCount()
    {
        sphereOrigin = transform.position;
        Collider[] nodeColliders = Physics.OverlapSphere(sphereOrigin, sphereRadius, sphereLayerMask);
        int inactiveNodeCounter = 0;

        foreach (Collider node in nodeColliders)
        {
            
            if (node.gameObject.tag =="WorkInactive" || node.gameObject.tag == "Construction")
            {
                inactiveNodeCounter += 1;                                
            }
        }
        Debug.Log("Found Inactive Node Colliders: " + inactiveNodeCounter);
        return inactiveNodeCounter;
    }

    #endregion


    #region GoTo Methods

    void GoToNearestTreeNode()
    {
        m_NavMeshAgent.isStopped = false;
        sphereLayerMask = LayerMask.GetMask("TreeNodes");
        m_NavMeshAgent.SetDestination(GetClosestInactiveNodeVector("WorkInactive"));
        Target = GetTarget("WorkInactive");
        Target.gameObject.tag = "WorkActive";
        m_Animator.SetBool("IsWalking", true);
        m_Animator.SetBool("IsLumbering", false);
        m_NavMeshAgent.stoppingDistance = 3;
        m_Animator.speed = 2;
    }

    void GoToNearestStoneNode()
    {
        m_NavMeshAgent.isStopped = false;
        sphereLayerMask = LayerMask.GetMask("StoneNodes");
        m_NavMeshAgent.SetDestination(GetClosestInactiveNodeVector("WorkInactive"));
        Target = GetTarget("WorkInactive");
        Target.gameObject.tag = "WorkActive";
        m_Animator.SetBool("IsWalking", true); 
        m_Animator.SetBool("IsLumbering", false);
        m_NavMeshAgent.stoppingDistance = 3;
        m_Animator.speed = 2;
    }

    void GoToNearestStorage()
    {
        m_NavMeshAgent.isStopped = false;
        sphereLayerMask = LayerMask.GetMask("Buildings");
        m_NavMeshAgent.SetDestination(GetClosestInactiveNodeVector("Storage"));
        Target = GetTarget("Storage");
        m_Animator.SetBool("IsLumbering", false);
        m_Animator.SetBool("IsWalking", true);
        m_NavMeshAgent.stoppingDistance = 3;
        m_Animator.speed = 2;
    }

    void GoToNearestFire()
    {
        m_NavMeshAgent.isStopped = false;
        sphereLayerMask = LayerMask.GetMask("SafePlaceNodes");
        m_NavMeshAgent.SetDestination(GetClosestInactiveNodeVector("Untagged"));
        Target = GetTarget("Untagged");
        m_Animator.SetBool("IsWalking", true);
        m_Animator.SetBool("IsLumbering", false);
        m_Animator.speed = 2;
    }

    #endregion


    #region Job Methods

    void MineWood()
    {
        m_Animator.SetBool("IsLumbering", true);
        m_Animator.speed = 1;
        workTime += Time.deltaTime;
        transform.LookAt(m_NavMeshAgent.destination);
        if (workTime >= 12 / workSpeed)
        {
            if (Target !=null)
            {
                // Try and find a Nodes Resource script on the gameobject hit.
                TreeNodes treeNode = Target.GetComponent<TreeNodes>();
                // If the Node Resource script component exists...
                if (treeNode != null)
                {
                    // ... the Node should lose resources.
                    treeNode.TakeDamage();
                    inventoryAmount += 1;
                    inventoryWood += 1;
                    Debug.Log("Wood mined");
                    workTime -= (int)workTime;
                }
            }
        }
    }

    void MineStone()
    {
        m_Animator.SetBool("IsLumbering", true);
        m_Animator.speed = 1;
        workTime += Time.deltaTime;
        transform.LookAt(m_NavMeshAgent.destination);
        if (workTime >= 12 / workSpeed)
        {
            // Try and find an Nodes Resource script on the gameobject hit.
            StoneNodes stoneNode = Target.GetComponent<StoneNodes>();
            // If the Node Resource component exists...
            if (stoneNode != null)
            {
                // ... the Node should lose resources.
                stoneNode.TakeDamage();
                inventoryAmount += 1;
                inventoryStone += 1;
                Debug.Log("Stone mined");
            }
            workTime -= (int)workTime;
        }        
    }

    void ConstructBuilding()
    {
        m_Animator.SetBool("IsLumbering", true);
        m_Animator.speed = 1;
        workTime += Time.deltaTime;
        transform.LookAt(m_NavMeshAgent.destination);
        if (workTime >= 12 / workSpeed)
        {
            // Try and find an Buildings Info script on the gameobject to construct.
            BuildingInfo currentHealth = Target.GetComponent<BuildingInfo>();
            // If the Building Info component exists...
            if (currentHealth != null)
            {
                // ... the Building should "gain health".
                currentHealth.GainHealth();
                Debug.Log("Health gained");                
            }
            workTime -= (int)workTime;
        }
        
    }

    #endregion


    #region Resource Methods

    void StoreResourceForConstruction(string resource)
    {
        BuildingInfo reqResources = nextNodeCollider.GetComponent<BuildingInfo>();

        if (resource == "Wood")
        {
            workTime += Time.deltaTime;
            transform.LookAt(m_NavMeshAgent.destination);
            if (workTime >= 1)
            {                
                inventoryAmount -= (int)workTime;
                inventoryWood -= (int)workTime;
                ResourceBank.AddWoodToStock((int)workTime);
                reqResources.ReqResourcePlus("Wood");
                workTime -= (int)workTime;              
            }
        }
        if (resource == "Stone")
        {
            workTime += Time.deltaTime;
            transform.LookAt(m_NavMeshAgent.destination);
            if (workTime >= 1)
            {                
                inventoryAmount -= (int)workTime;
                inventoryStone -= (int)workTime;
                ResourceBank.AddStoneToStock((int)workTime);
                reqResources.ReqResourcePlus("Stone");
                workTime -= (int)workTime;               
            }
        }
    }

    void TakeResourceForConstruction(string resource)
    {
        BuildingInfo reqResources = nextNodeCollider.GetComponent<BuildingInfo>();

        if (resource == "Wood")
        {
            workTime += Time.deltaTime;
            transform.LookAt(m_NavMeshAgent.destination);
            if (workTime >= 1)
            {
                if (ResourceBank.GetWoodStock() > 0)
                {
                    inventoryAmount += (int)workTime;
                    inventoryWood += (int)workTime;
                    ResourceBank.RemoveWoodFromStock((int)workTime);
                    reqResources.ReqResourceMinus("Wood");
                    workTime -= (int)workTime;
                }
                
            }
        }
        if (resource == "Stone")
        {
            workTime += Time.deltaTime;
            transform.LookAt(m_NavMeshAgent.destination);
            if (workTime >= 1)
            {
                if (ResourceBank.GetStoneStock() > 0)
                {
                    inventoryAmount += (int)workTime;
                    inventoryStone += (int)workTime;
                    ResourceBank.RemoveStoneFromStock((int)workTime);
                    reqResources.ReqResourceMinus("Stone");
                    workTime -= (int)workTime;
                }
                
            }
        }
    }

    void TakeResource(string resource)
    {
        if (resource == "Wood")
        {
            workTime += Time.deltaTime;
            if (workTime >= 1)
            {
                inventoryAmount += (int)workTime;
                inventoryWood += (int)workTime;
                ResourceBank.RemoveWoodFromStock((int)workTime);
                workTime -= (int)workTime;
            }
        }
        if (resource == "Stone")
        {
            workTime += Time.deltaTime;
            if (workTime >= 1)
            {
                inventoryAmount += (int)workTime;
                inventoryStone += (int)workTime;
                ResourceBank.RemoveStoneFromStock((int)workTime);
                workTime -= (int)workTime;
            }
        }
    }

    void PutWoodInFire()
    {
        workTime += Time.deltaTime;
        if (workTime >= 1)
        {
            inventoryAmount -= (int)workTime;
            inventoryWood -= (int)workTime;
            ResourceBank.AddWoodToFire((int)workTime);
            workTime -= (int)workTime;
        }
            
    }

    void StoreResource(string resource)
    {
        if (resource == "Wood")
        {
            workTime += Time.deltaTime;
            if (workTime >= 1)
            {
                inventoryAmount -= (int)workTime;
                inventoryWood -= (int)workTime;
                ResourceBank.AddWoodToStock((int)workTime);
                workTime -= (int)workTime;
            }
                
        }
        if (resource == "Stone")
        {
            workTime += Time.deltaTime;
            if (workTime >= 1)
            {
                inventoryAmount -= (int)workTime;
                inventoryStone -= (int)workTime;
                ResourceBank.AddStoneToStock((int)workTime);
                workTime -= (int)workTime;
            }
                
        }
    }

    #endregion



    void Awake()
    {
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponent<Animator>();
        m_Animator.speed = animationSpeed;

        //Visualize Search Sphere
        searchSphere = transform.Find("SearchSphere");
        searchSphere.localScale += new Vector3(sphereRadius, sphereRadius, sphereRadius);

        //Set Spawn Behaviour                
        GoToNearestFire();
        state = State.MovingToFire;

        //Set Inventory Amount
        inventoryAmount = 0;
    }


    void Update()
    {
        switch (state)
        {
            #region State IdleAtFire

            case State.IdleAtFire:
                Debug.Log("State: IdleAtFire");                
                if (currentJob == "Unemployed" || currentJob == "LightWarden")
                {
                    Debug.Log("State: IdleAtFire");
                    inventorySize = 1;
                    m_NavMeshAgent.isStopped = true;
                    m_Animator.SetBool("IsWalking", false);
                    m_Animator.SetBool("IsLumbering", false);
                    transform.LookAt(m_NavMeshAgent.destination);


                    if (currentJob == "LightWarden" && 
                        ResourceBank.GetFireLife() < ResourceBank.GetFireLifeMax() && 
                        ResourceBank.GetWoodStock() > 0)
                    {
                        inventorySize = 1;
                        sphereLayerMask = LayerMask.GetMask("Buildings");
                        if (GetNodeCollider("Storage") != null)
                        {
                            GoToNearestStorage();
                            state = State.MovingToStorage;
                        }                        
                    }
                }

                if (currentJob == "Builder")
                {
                    inventorySize = 5;
                    sphereLayerMask = LayerMask.GetMask("Buildings");
                    GetNodeCollider("Construction");
                    if (GetInactiveNodesCount() != 0)
                    {
                        GoToNearestStorage();
                        state = State.MovingToStorage;
                    }
                }

                if (currentJob == "Woodcutter")
                {
                    inventorySize = 1;
                    sphereLayerMask = LayerMask.GetMask("TreeNodes");
                    if (GetInactiveNodesCount() != 0)
                    {
                        GoToNearestTreeNode();
                        state = State.MovingToJobNode;
                    }                                     
                }

                if (currentJob == "Stonecutter")
                {
                    inventorySize = 1;
                    sphereLayerMask = LayerMask.GetMask("StoneNodes");
                    if (GetInactiveNodesCount() != 0)
                    {
                        GoToNearestStoneNode();
                        state = State.MovingToJobNode;
                    }                         
                }
                break;

            #endregion

            #region State MovingToJobNode

            case State.MovingToJobNode:
                Debug.Log("State: MovingToJobNode");
                if (currentJob == "Unemployed")
                {
                    if (inventoryAmount > 0)
                    {
                        GoToNearestStorage();
                        state = State.MovingToStorage;
                    }
                    else
                    {
                        GoToNearestFire();
                        state = State.MovingToFire;
                    }
                }


                if (m_NavMeshAgent.pathPending)
                {
                    distanceToNode = Vector3.Distance(transform.position, Target.transform.position);
                }
                else
                {
                    distanceToNode = m_NavMeshAgent.remainingDistance;
                }
                Debug.Log("MovingToJobNode - Distance to Job Node: " + distanceToNode);
                if (distanceToNode <= m_NavMeshAgent.stoppingDistance)
                {
                    Debug.Log("Job Node reached");
                    m_Animator.SetBool("IsWalking", false);
                    if (currentJob == "Builder")
                    {
                        BuildingInfo building = Target.GetComponent<BuildingInfo>();
                        if (Target.tag == "Construction" && ( building.currentWood + building.currentStone ) < 
                                (building.costWood + building.costStone))
                        {
                            state = State.LoadToConstruction;
                        }


                        if (Target.tag == "Construction" && (building.currentWood + building.currentStone) ==
                                (building.costWood + building.costStone))
                        {
                            state = State.ConstructionWork;
                        }                  
                        
                        else
                        {
                            Debug.Log("Waiting for remaining ressources on the way");
                        }
                    }
             
                    
                    else
                    {
                        state = State.WorkingOnJob;
                    }
                }                
                break;

            #endregion

            #region State ConstructionWork

            case State.ConstructionWork:
                Debug.Log("State: ConstructionWork");
                if (currentJob == "Builder")
                {
                    m_NavMeshAgent.isStopped = true;
                    sphereLayerMask = LayerMask.GetMask("Buildings");
                    BuildingInfo building = Target.GetComponent<BuildingInfo>();
                    Debug.Log("Building Info Script found");
                    Debug.Log("Constructing Building");
                    ConstructBuilding();
                    //-- Set Building Model during Construction --//
                    if ((int)building.currentHealth <= building.maxHealth * 0.25)
                    {
                        building.SetBuildingModel(1);
                    }
                    if ((int)building.currentHealth <= building.maxHealth * 0.50 && (int)building.currentHealth > building.maxHealth * 0.25)
                    {
                        building.SetBuildingModel(2);
                    }
                    if ((int)building.currentHealth <= building.maxHealth * 0.75 && (int)building.currentHealth > building.maxHealth * 0.50)
                    {
                        building.SetBuildingModel(3);
                    }
                    if ((int)building.currentHealth <= building.maxHealth * 1 && (int)building.currentHealth > building.maxHealth * 0.75)
                    {
                        building.SetBuildingModel(4);
                    }


                    if (building.currentHealth == building.maxHealth)
                    {
                        ClickableObject clickObject = Target.GetComponent<ClickableObject>();
                        clickObject.infoPanel = clickObject.originalInfoPanel;
                        building.ConstructionComplete();

                        if (GetInactiveNodesCount() != 0)
                        {
                            sphereLayerMask = LayerMask.GetMask("Buildings");
                            GetNodeCollider("Construction");
                            Target = GetTarget("Storage");
                            GoToNearestStorage();
                            state = State.MovingToStorage;
                        }
                        else
                        {
                            GoToNearestFire();
                            state = State.MovingToFire;
                        }
                    }
                }
                else
                {
                    GoToNearestFire();
                    state = State.MovingToFire;
                }

                break;

            #endregion

            #region State WorkingOnJob

            case State.WorkingOnJob:
                Debug.Log("State: WorkingOnJob");
                if (inventoryAmount < inventorySize && Target != null)
                {
                    if (currentJob == "Woodcutter" )
                    {
                        m_NavMeshAgent.isStopped = true;
                        Debug.Log("Mining Wood");
                        MineWood();
                    }

                    if (currentJob == "Stonecutter")
                    {
                        m_NavMeshAgent.isStopped = true;
                        Debug.Log("Mining Stone");
                        MineStone();
                    }                 
                    
                    if (currentJob == "Unemployed")
                    {
                        if (inventoryAmount > 0)
                        {
                            GoToNearestStorage();
                            state = State.MovingToStorage;
                        }
                        else
                        {
                            GoToNearestFire();
                            state = State.MovingToFire;
                        }
                    }                    
                }
                
                else
                {
                    Debug.Log("Job Done");
                    m_Animator.speed = animationSpeed;

                    if (currentJob == "Woodcutter" || currentJob == "Stonecutter")
                    {
                        if (Target != null)
                        {
                            Target.gameObject.tag = "WorkInactive";
                            GoToNearestStorage();
                            state = State.MovingToStorage;
                        }
                        
                        else
                        {
                            GoToNearestStorage();
                            state = State.MovingToStorage;
                        }
                    }
                }                                                                      
                break;

            #endregion

            #region State MovingToStorage

            case State.MovingToStorage:
                Debug.Log("State: MovingToStorage");
                m_Animator.SetBool("IsWalking", true);
                m_Animator.speed = 2;
                if (m_NavMeshAgent.pathPending)
                {
                    distanceToNode = Vector3.Distance(transform.position, Target.transform.position);
                }
                else
                {
                    distanceToNode = m_NavMeshAgent.remainingDistance;
                }
                Debug.Log("MovingToStorage - Distance to Storage Node: " + distanceToNode);
                if (distanceToNode <= m_NavMeshAgent.stoppingDistance)
                {
                    m_Animator.SetBool("IsWalking", false);
                    m_Animator.SetBool("IsLumbering", false);

                    if (currentJob == "Unemployed")
                    {
                        if (inventoryAmount > 0)
                        {
                            state = State.LoadToStorage;
                        }
                        else
                        {
                            GoToNearestFire();
                            state = State.MovingToFire;
                        }                                                                
                    }

                    if (currentJob == "LightWarden" || currentJob == "Builder")
                    {                        
                        state = State.TakingFromStorage;
                    }

                    if (currentJob == "Stonecutter" || currentJob == "Woodcutter")
                    {
                        state = State.LoadToStorage;
                    }                     
                }
                break;

            #endregion

            #region State TakingFromStorage

            case State.TakingFromStorage:
                Debug.Log("State: TakingFromStorage");
                if (currentJob == "LightWarden")
                {
                    if (inventoryAmount < inventorySize && ResourceBank.GetWoodStock() > 0)
                    {
                        Debug.Log("Taking Wood");
                        TakeResource("Wood");
                    }                    
                    else
                    {
                        Debug.Log("Taking complete");                        
                        GoToNearestFire();
                        state = State.MovingToFire;                        
                    }
                }

                if (currentJob == "Unemployed")
                {
                    if (inventoryAmount > 0)
                    {
                        Debug.Log("Loading to Storage");
                        if (inventoryWood > 0)
                        {
                            StoreResource("Wood");
                        }
                        if (inventoryStone > 0)
                        {
                            StoreResource("Stone");
                        }
                    }
                    else
                    {
                        Debug.Log("Storing complete");
                        GoToNearestFire();
                        state = State.MovingToFire;
                    }
                }

                if (currentJob == "Builder")
                {
                    BuildingInfo reqResources = nextNodeCollider.GetComponent<BuildingInfo>();
                    Debug.Log("Required Wood for Construction: " + reqResources.GetReqResource("Wood"));
                    Debug.Log("Required Stone for Construction: " + reqResources.GetReqResource("Stone"));
                    
                    if (inventoryAmount < inventorySize && reqResources.moreResourcesNeeded() == true)
                    {
                        
                        if (reqResources.GetReqResource("Wood") > 0)
                        {
                            TakeResourceForConstruction("Wood");
                        }
                        if (reqResources.GetReqResource("Stone") > 0)
                        {
                            TakeResourceForConstruction("Stone");
                        }
                                             
                    }                
                    
                    if (inventoryAmount == inventorySize || reqResources.moreResourcesNeeded() == false)
                    {
                        Debug.Log("Taking Resources complete");
                        Target = nextNodeCollider;
                        m_NavMeshAgent.SetDestination(Target.gameObject.transform.position);                       
                        m_Animator.SetBool("IsWalking", true);
                        m_NavMeshAgent.stoppingDistance = 3;

                        state = State.MovingToJobNode;
                    }
                }                                             
                break;

            #endregion

            #region State LoadToStorage

            case State.LoadToStorage:
                Debug.Log("State: LoadingToStorage");
                if (inventoryAmount > 0)
                {
                    workTime += Time.deltaTime;
                    if (workTime >= 1)
                    {
                        if (currentJob == "Unemployed")
                        {
                            if (nextNodeCollider.tag == "Construction")
                            {
                                if (inventoryWood > 0)
                                {
                                    StoreResourceForConstruction("Wood");
                                }
                                if (inventoryStone > 0)
                                {
                                    StoreResourceForConstruction("Stone");
                                }
                            }
                            else
                            {
                                if (inventoryWood > 0)
                                {
                                    StoreResource("Wood");
                                }
                                if (inventoryStone > 0)
                                {
                                    StoreResource("Stone");
                                }
                            }
                        }

                        if (currentJob == "LightWarden")
                        {
                            PutWoodInFire();
                        }
                        if (currentJob == "Woodcutter")
                        {
                            StoreResource("Wood");
                        }
                        if (currentJob == "Stonecutter")
                        {
                            StoreResource("Stone");
                        }
                    }
                    
                }
                else
                {
                    Debug.Log("Loading complete");
                    if (currentJob == "Unemployed")
                    {
                        GoToNearestFire();
                        m_NavMeshAgent.stoppingDistance = 5;
                        state = State.MovingToFire;
                    }
                    if (currentJob == "LightWarden")
                    {                        
                        state = State.IdleAtFire;
                    }
                    if (currentJob == "Woodcutter")
                    {
                        sphereLayerMask = LayerMask.GetMask("TreeNodes");
                        if (GetInactiveNodesCount() != 0)
                        {
                            GoToNearestTreeNode();
                            state = State.MovingToJobNode;
                        }
                        else
                        {
                            GoToNearestFire();
                            m_NavMeshAgent.stoppingDistance = 5;
                            state = State.MovingToFire;
                        }
                        
                       
                    }

                    if (currentJob == "Stonecutter")
                    {
                        sphereLayerMask = LayerMask.GetMask("StoneNodes");
                        if (GetInactiveNodesCount() != 0)
                        {
                            GoToNearestStoneNode();
                            state = State.MovingToJobNode;
                        }
                        else
                        {
                            GoToNearestFire();
                            m_NavMeshAgent.stoppingDistance = 5;
                            state = State.MovingToFire;
                        }
                    }
                }
                break;

            #endregion

            #region State LoadToConstruction

            case State.LoadToConstruction:
                Debug.Log("State: LoadToConstruction");
                if (currentJob == "Unemployed" && inventoryAmount == 0)
                {
                    GoToNearestFire();
                    state = State.MovingToFire;
                }

                if (inventoryAmount > 0)
                {
                    BuildingInfo buildingInfo = nextNodeCollider.GetComponent<BuildingInfo>();
                    workTime += Time.deltaTime;
                    if (workTime >= 1)
                    {
                        if (inventoryWood > 0)
                        {
                            inventoryAmount -= (int)workTime;
                            inventoryWood -= (int)workTime;
                            buildingInfo.CurrentResourcePlus("Wood");
                            workTime -= (int)workTime;
                        }                                                      
                    }
                    workTime += Time.deltaTime;
                    if (workTime >= 1)
                    {
                        if (inventoryStone > 0)
                        {
                            inventoryAmount -= (int)workTime;
                            inventoryStone -= (int)workTime;
                            buildingInfo.CurrentResourcePlus("Stone");
                            workTime -= (int)workTime;
                        }
                    }                        
                }
                else
                {
                    BuildingInfo buildingInfo = nextNodeCollider.GetComponent<BuildingInfo>();
                    if (buildingInfo.moreResourcesNeeded() == true)
                    {
                        GoToNearestStorage();
                        Debug.Log("More resources needed - go to storage");
                        state = State.MovingToStorage;
                    }
                    if (buildingInfo.waitForRemainingResource() == true)
                    {
                        Debug.Log("Waiting for remaining Resources");
                    }
                    if (buildingInfo.moreResourcesNeeded() == false)
                    {
                        m_Animator.SetBool("IsLumbering", true);
                        state = State.ConstructionWork;
                    }
                }
                break;

            #endregion

            #region State MovingToFire

            case State.MovingToFire:
                Debug.Log("State: MovingToFire");
                if (currentJob == "LightWarden")
                {
                    m_NavMeshAgent.stoppingDistance = 3;
                }

                if (m_NavMeshAgent.pathPending)
                {
                    distanceToNode = Vector3.Distance(transform.position, Target.transform.position);
                }
                else
                {
                    distanceToNode = m_NavMeshAgent.remainingDistance;
                }
                Debug.Log("MovingToFire - Distance to FirePlace: " + distanceToNode);
                
                if (distanceToNode <= m_NavMeshAgent.stoppingDistance)
                {
                    Debug.Log("Fire reached");
                    m_Animator.SetBool("IsWalking", false);

                    if (currentJob == "Unemployed" && inventoryWood > 0 && 
                        ResourceBank.GetFireLife() < ResourceBank.GetFireLifeMax() || 
                        currentJob == "LightWarden" && inventoryWood > 0 && 
                        ResourceBank.GetFireLife() < ResourceBank.GetFireLifeMax())
                        {
                        state = State.LoadToStorage;
                        }

                    else
                    {
                        state = State.IdleAtFire;
                    }                   
                }
                break;

            #endregion

        }
    }
}


                