﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PersonFitTest : MonoBehaviour
{
	public int Value { get; set; }

	public int SomeValue ()
	{
		return 0;
	}

}

public class Person : MonoBehaviour
{
	public int Age { get; set; }

	public string Name { get; set; }

	public Person Father { get; set; }

	public Person Mother { get; set; }
}



public class Entity : MonoBehaviour
{

	void Awake ()
	{
		gameObject.AddComponent<CircleCollider2D> ().radius = 1;
	}

	public bool Dead { get; internal set; }

	public void Destroy ()
	{
		Dead = true;
		Destroy (gameObject);
	}
}



