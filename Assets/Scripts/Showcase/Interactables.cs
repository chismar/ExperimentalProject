﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Interactables : MonoBehaviour
{
    public List<GameObject> Every { get; set; }
    public int Count { get { return Every.Count; } }
    public event GODelegate GOAttached;
    public event GODelegate GODetached;
    void Awake()
    {
        Every = new List<GameObject>();
    }

    public void Attach(GameObject go)
    {
        Every.Add(go);
        var marker = go.GetComponent<InteractablesMarker>();
        if (marker == null)
            marker = go.AddComponent<InteractablesMarker>();
        marker.AttachedTo = this;
        var e = go.GetComponent<Entity>();
        if(e != null)
        e.Destoryed += OnEntityDestroyed;
        if (GOAttached != null)
            GOAttached(go);
    }

    void OnEntityDestroyed(GameObject go)
    {
        Detach(go);
    }
    public void Detach(GameObject go)
    {
        var e = go.GetComponent<Entity>();
        if (e != null)
            e.Destoryed -= OnEntityDestroyed;
        Every.Remove(go);
        if (GODetached != null)
            GODetached(go);
    }

}

public class InteractablesMarker : MonoBehaviour
{
    public Interactables AttachedTo { get; set; }
    public GameObject Attached { get { return AttachedTo.gameObject; } }
    void Start()
    {
        if (AttachedTo != null && !AttachedTo.Every.Contains(gameObject))
            AttachedTo.Attach(gameObject);
    }
}