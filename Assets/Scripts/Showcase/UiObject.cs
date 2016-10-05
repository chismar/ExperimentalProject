﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class UiObject : MonoBehaviour
{
	[SerializeField]
	GameObject showedObject;
	static Generators gens;

	public GameObject ShowedObject {
		get { return showedObject; }
		set
		{
			showedObject = value;
			var ent = showedObject.GetComponent<Entity> ();
			if (ent != null)
			{
				ent.OnDestruction (OnGODestoryed);
				ent.ComponentAddedEvent += EntComponentAdded;
			}
		}
	}

	void EntComponentAdded ()
	{
		Debug.Log ("Regenerating ui for ", ShowedObject);
		if (gens == null)
			gens = FindObjectOfType<Generators> ();
		gens.Generate (gameObject);
	}

	void OnGODestoryed (GameObject go)
	{
		showedObject = null;
		Destroy (gameObject);
	}

	LayoutElement layout;

	public LayoutElement Layout {
		get
		{
			if (layout == null)
				layout = gameObject.AddComponent<LayoutElement> ();
			return layout;
		}
	}



	void Awake ()
	{
		if (GetComponent<Markers> () == null)
			gameObject.AddComponent<Markers> ();
	}

}

public delegate bool BoolDelegate ();
public class Updated : MonoBehaviour
{
	List<BoolDelegate> delegates = new List<BoolDelegate> ();

	void Update ()
	{
		for (int i = 0; i < delegates.Count; i++)
		{
			bool shouldStop = delegates [i] ();
			if (shouldStop)
			{

				delegates.RemoveAt (i);
				i--;
			}
		}
	}

	public void OnUpdate (BoolDelegate boolDelegate)
	{
		delegates.Add (boolDelegate);
	}
}

