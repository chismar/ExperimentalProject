﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;
using System;
using System.CodeDom;
using InternalDSL;
using System.Linq;
using CSharpCompiler;
using UnityEngine.UI;

public abstract class ScriptEnginePlugin
{
	protected ScriptEngine Engine { get; private set; }

	public void SetEngine (ScriptEngine engine)
	{
		Engine = engine;
	}

	public abstract void Init ();
}

public static class NameTranslator
{
	

	static StringBuilder builder = new StringBuilder ();

	public static string CSharpNameFromScript (string name)
	{
		builder.Length = 0;
		builder.Append (Char.ToUpper (name [0]));
		for (int i = 1; i < name.Length; i++)
		{
			if (name [i] == '_')
			{
				builder.Append (Char.ToUpper (name [i + 1]));
				i++;
			} else
				builder.Append (name [i]);
		}
		return builder.ToString ();
	}

	public static string ScriptNameFromCSharp (string name)
	{
		builder.Length = 0;
		builder.Append (Char.ToLower (name [0]));
		for (int i = 1; i < name.Length; i++)
		{
			char lower = Char.ToLower (name [i]);
			char possibleUpper = name [i];
			if (lower == possibleUpper)
			{
				builder.Append (lower);
			} else
			{
				builder.Append ('_');
				builder.Append (lower);
			}
		}
		return builder.ToString ();
	}
}

public class ScriptCompiler : ScriptEnginePlugin
{
	List<string> csharpSources = new List<string> ();

	public override void Init ()
	{
	}

	public void AddSource (string source)
	{
		csharpSources.Add (source);
	}

	public void Compile (Action<Assembly> onAssemblyCompiled)
	{
		var loader = UnityEngine.Object.FindObjectOfType<ScriptsLoader> ();
		string sourceCode = string.Concat (csharpSources.ToArray ());

		UnityEngine.GameObject.Find ("SourceCode").GetComponent<Text> ().text = sourceCode;
		onAssemblyCompiled (loader.Load (csharpSources.ToArray (), "Content"));
	}
}

