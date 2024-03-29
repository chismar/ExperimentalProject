﻿using UnityEngine;
using System.Collections;
using InternalDSL;
using System.IO;
using System.CodeDom;
using System.Text;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System;
using Microsoft.CSharp;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection.Emit;
using System.Linq;
using System.Threading;

public delegate void VoidDelegate ();
public class BasicLoader : MonoBehaviour
{
    public static bool IsInEditor;
	// Use this for initialization
	ScriptEngine engine = new ScriptEngine ();

	public ScriptEngine Engine { get { return engine; } }

	List<ProgressBar> pluginBars = new List<ProgressBar> ();
	System.Random random = new System.Random ();

	public class ExternalFunctions
	{
		public object Provider;
		public string[] Functions;

		public ExternalFunctions (object provider, params string[] functions)
		{
			Provider = provider;
			Functions = functions;
		}
	}

	public List<ExternalFunctions> EFunctions = new List<ExternalFunctions> ();

    Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    public float Clamp(float min, float max, float value)
    {
        return Mathf.Clamp(value, min, max);
    }

    public delegate bool BoolDelegate(GameObject go);
    public bool Any(List<GameObject> gos, BoolDelegate check)
    {
        for ( int i = 0; i < gos.Count; i++)
        {
            if (check(gos[i]))
                return true;
        }
        return false;
    }
    public GameObject SpawnPrefab(string name)
    {
        GameObject prefab = null;
        if(!prefabs.TryGetValue(name, out prefab))
        {
            var csharpname = NameTranslator.CSharpNameFromScript(name);
            prefab = Resources.Load("UIPrefabs/" + csharpname) as GameObject;
            prefabs.Add(name, prefab);
        }
        return GameObject.Instantiate(prefab);

    }

    public void Regenerate(GameObject go)
    {
        FindObjectOfType<Generators>().Generate(go, 0.1f);
    }
	public void Log (object log)
	{
		//Debug.Log (log);
	}
    public GameObject Nothing()
    {
        return null;
    }
	void Awake ()
	{
        IsInEditor = Application.isEditor;
        var loader = FindObjectOfType<SavingLoading> ();
		Loaded += () => loader.IsLoading = false;
	}

    public GameObject Gameobject(MonoBehaviour cmp)
    {
        return cmp.gameObject;
    }



	public delegate bool SelectionDelegate (GameObject go);

	public GameObject SelectFrom (List<GameObject> list, SelectionDelegate checker)
	{
		for (int i = 0; i < list.Count; i++)
			if (checker (list [i]))
				return list [i];
		return null;
	}

    List<GameObject> randomFromCache = new List<GameObject>();
    public GameObject RandomFrom(List<GameObject> list, SelectionDelegate checker)
    {
        randomFromCache.Clear();
        for (int i = 0; i < list.Count; i++)
            if (checker(list[i]))
                randomFromCache.Add(list[i]);
        if (randomFromCache.Count == 0)
            return null;
        return randomFromCache[random.Next(0, randomFromCache.Count)];
    }

    public void Destroy (UnityEngine.Object obj)
	{
		if (obj is GameObject)
		{
			((GameObject)obj).GetComponent<Entity> ().Destroy ();
		} else if (obj is MonoBehaviour)
		{
			((MonoBehaviour)obj).GetComponent<Entity> ().Destroy ();
		}
	}

	public Vector3 Vec (float x, float y)
	{
		return new Vector3 (x, 0, y);
	}

	public float Random (float min, float max)
	{
		return Mathf.Lerp (min, max, (float)random.NextDouble ());
	}

	public GameObject NoOne ()
	{
		return null;
	}

    public void SetParent(GameObject go, GameObject parent)
    {
        go.transform.SetParent(parent.transform, false);
    }
	public bool Has (object obj)
	{
		return !(ReferenceEquals (obj, null) || obj.Equals (null));
	}
  
    public bool Contains(IList collection, object obj)
    {
        return collection.Contains(obj);
    }
	public int Dsix { get { return random.Next (0, 10); } internal set { } }

	public event VoidDelegate Loaded;

	void Start ()
	{
		StartCoroutine (LoadCoroutine ());
		StartCoroutine (UpdateCoroutine ());
	}

	Thread compileThread;
    
	void OnDestroy ()
	{
		Engine.Working = false;
	}

	IEnumerator LoadCoroutine ()
	{

		List<Assembly> addons = new List<Assembly> ();
		Engine.Init (addons);
		var dirInfo = new DirectoryInfo ((Application.isEditor? "Assets/" : "ExperimentalProject_Data/") + "StreamingAssets/Mods");
		DateTime lastWriteTime = DateTime.MinValue;
		var ExternalFunctions = Engine.GetPlugin<ExternalFunctionsPlugin> ();
		var bars = FindObjectOfType<ProgressBarSet> ();
		genBar = bars.CreateBar (Color.yellow, "gen");
		for (int i = 0; i < Engine.PluginsCount; i++)
			pluginBars.Add (bars.CreateBar (Color.blue, i.ToString()));
		ExternalFunctions.Load ();
		foreach (var eFunctions in EFunctions)
			ExternalFunctions.AddProvider (eFunctions.Provider, eFunctions.Functions);
		ExternalFunctions.AddProvider (this, "Random", "Regenerate", "SetParent", "RandomFrom", "Any", "AbstractCamp", "Clamp", "Nothing", "Gameobject", "Contains" ,"SpawnPrefab","Has", "Vec", "GetWorld", "GetEventsController", "SelectFrom", "Log", "String", "GetPlayer", "Destroy", "NoOne");
		foreach (var fileInfo in dirInfo.GetFiles ("*", SearchOption.AllDirectories))
		{
			if (fileInfo.LastWriteTimeUtc > lastWriteTime)
				lastWriteTime = fileInfo.LastWriteTimeUtc;
		}
		var lastBuildString = PlayerPrefs.GetString ("last_build");
		Debug.Log (lastBuildString);
		Debug.Log (lastWriteTime);
		if (String.IsNullOrEmpty (lastBuildString) || (!String.IsNullOrEmpty (lastBuildString) && DateTime.Parse (lastBuildString) < lastWriteTime))
		{
			Debug.Log ("Loading scripts");
			yield return null;
			//loads scripts and set a date

			//		AbstractClassChildren actionsList = new AbstractClassChildren ("Generators", engine){ BaseType = typeof(EventAction) };
			AppDomain.CurrentDomain.AssemblyResolve += Resolver;
			loadedAsms.Add ("ExternalCode");
			loadedAsms.Add ("BlackboardsData");
			bbloader = new BlackboardsLoader (Engine);
			bbloader.Init ();
			eaBar = FindObjectOfType<ProgressBarSet> ().CreateBar (Color.green, "eaBar");
			compileThread = new Thread (() => {
				try
				{
					ExternalFunctions.Setup (OnExternalsCompiled);
				} catch (Exception e)
				{
					Debug.LogException (e);
				}});
			compileThread.Start ();
		} else
		{
			Debug.Log ("Loading dlls");
			yield return null;
			//Load dlls
			var asm = Assembly.LoadFile ((Application.isEditor ? "Assets/" : "ExperimentalProject_Data/") + "StreamingAssets/DLLs/ExternalCode.dll");
			ExternalFunctions.OnCompiled (asm);
			asm = Assembly.LoadFile ((Application.isEditor ? "Assets/" : "ExperimentalProject_Data/") + "StreamingAssets/DLLs/BlackboardsData.dll");
			Engine.AddAssembly (asm);
			asm = Assembly.LoadFile ((Application.isEditor ? "Assets/" : "ExperimentalProject_Data/") + "StreamingAssets/DLLs/Content.dll");
			Engine.AddAssembly (asm);

			yield return null;

			foreach (var bar in this.pluginBars)
				bar.Expire ();
			if (Loaded != null)
				Loaded ();
			//AppDomain.CurrentDomain.Load()

		}




	}

	BlackboardsLoader bbloader;
	ProgressBar eaBar;
	ProgressBar genBar;
    
	void OnExternalsCompiled ()
	{
		var loader = new EventActionsLoader ("generators", Engine);
		loader.CurProgressUpdated += x => eaBar.CurValue = x;
		loader.CurProgressUpdated += x => {
			if (x == eaBar.MaxValue)
			{
				eaBar.Expire ();
			}
		};
		loader.MaxProgressResolved += x => eaBar.MaxValue = x;
        Script genScript = new Script ("generators", loader, Engine);
        var scriptFiles = Directory.GetFiles((BasicLoader.IsInEditor ? "Assets/" : "ExperimentalProject_Data/") + "StreamingAssets/Mods/Scripts", "*.def");
        genScript.Progress.CurProgressUpdated += x => genBar.CurValue = x;
        int filesLoaded = 0;
		genScript.Progress.CurProgressUpdated += x => {
			if (x == genBar.MaxValue && filesLoaded == scriptFiles.Length - 1)
			{
				genBar.Expire ();
			}
		};
		genScript.Progress.MaxProgressResolved += x => genBar.MaxValue = x;
        foreach (var file in scriptFiles)
        {
                genScript.LoadFile(file);
                filesLoaded++;
        }




        bbloader.AddType (typeof(GameObject), "gameobject");
		bbloader.AddType (typeof(int), "int");
		bbloader.AddType (typeof(float), "float");
		bbloader.AddType (typeof(string), "string");
		bbloader.AddType (typeof(bool), "bool");
		Script blackboardsScript = new Script ("blackboards", bbloader, Engine);
        scriptFiles = Directory.GetFiles((BasicLoader.IsInEditor ? "Assets/" : "ExperimentalProject_Data/") + "StreamingAssets/Mods/Blackboards", "*.def");
        foreach (var file in scriptFiles)
            blackboardsScript.LoadFile(file);
        if (ScriptEngine.AnalyzeDebug)
            foreach (var entry in genScript.Entries)
			Debug.Log (entry);

		blackboardsScript.Interpret ();
		int pluginId = 0;
		foreach (var plugin in Engine.Plugins)
		{
			var bar = pluginBars [pluginId++];
			plugin.CurProgressUpdated += x => bar.CurValue = x;
			plugin.CurProgressUpdated += x => {
				if (x == bar.MaxValue)
				{
					bar.Expire ();
				}
			};
			plugin.MaxProgressResolved += x => bar.MaxValue = x;
			plugin.MaxProgressResolved += x => {
				if (x == -1)
				{
					bar.Expire ();
				}
			};
		}
		Engine.InitPlugins ();

		genScript.Interpret ();
		var compiler = Engine.GetPlugin<ScriptCompiler> ();
		compiler.Compile (OnAssemblyCompiled);
	}



	HashSet<string> loadedAsms = new HashSet<string> ();

	StringBuilder builder = new StringBuilder ();

	Assembly Resolver (object sender, ResolveEventArgs args)
	{
        if(ScriptEngine.AnalyzeDebug)
		Debug.LogWarning (string.Format ("RESOLVE {0} = {1}", args.Name, loadedAsms.Contains (args.Name)));

		var asms = AppDomain.CurrentDomain.GetAssemblies ();
		builder.Length = 0;
		foreach (var a in asms)
			builder.AppendLine (a.GetName ().Name);
        if (ScriptEngine.AnalyzeDebug)
            Debug.LogWarning (builder.ToString ());
		if (loadedAsms.Contains (args.Name))
			return asms.First (x => x.GetName ().Name == args.Name);
		else
			return Assembly.Load (args.Name);
	}

	object asmLock = new object ();
	Assembly loadedAsm;

	void OnAssemblyCompiled (Assembly asm)
	{
		lock (asmLock)
		{

			loadedAsm = asm;
		}
	}

	void OnRetrievedAsm ()
	{
		lock (asmLock)
		{
			Debug.Log ("Success");
			Engine.AddAssembly (loadedAsm);
			var types = Engine.FindTypesCastableTo<EventAction> ();
			foreach (var type in types)
			{
				//Debug.Log (type);
			}
			if (Loaded != null)
				Loaded ();

			PlayerPrefs.SetString ("last_build", DateTime.UtcNow.ToString ());
			//AppDomain.CurrentDomain.Load (asm.Location);
		}

	}

	IEnumerator UpdateCoroutine ()
	{

		while (true)
		{
			
			yield return null;
			if (loadedAsm == null)
				continue;
			compileThread.Join ();
			OnRetrievedAsm ();
			break;
		}
	}
}

