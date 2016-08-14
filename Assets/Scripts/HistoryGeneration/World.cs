﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class World : MonoBehaviour
{
	public List<GameObject> Objects { get; internal set; }

	public string Name { get; set; }

	void Awake ()
	{
		Objects = new List<GameObject> ();
	}

	public void Add (GameObject go)
	{
		Objects.Add (go);
	}

	public void Remove (GameObject go)
	{
		Objects.Remove (go);
	}
}
