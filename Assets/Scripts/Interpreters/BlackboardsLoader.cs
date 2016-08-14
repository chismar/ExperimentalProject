﻿using UnityEngine;
using System.Collections;
using System.CodeDom;
using InternalDSL;
using System.Collections.Generic;
using System;
using CSharpCompiler;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;


public class BlackboardsLoader  : ScriptInterpreter
{

	Dictionary<string, Type> types = new Dictionary<string, Type> ();
	CodeNamespace cNamespace = new CodeNamespace ();
	Dictionary<string, CodeTypeDeclaration> codeTypes = new Dictionary<string, CodeTypeDeclaration> ();

	public BlackboardsLoader (ScriptEngine engine) : base (engine)
	{
		cNamespace.Name = "Blackboards";
	}

	//	public void AddTypes (params Type[] types)
	//	{
	//		foreach (var type in types)
	//			this.types.Add (NameTranslator.ScriptNameFromCSharp (type.Name), type);
	//	}
	//
	public void AddType (Type t, string name)
	{
		types.Add (name, t);
	}

	public override void Interpret (Script script)
	{
		foreach (var entry in script.Entries)
		{
			
			CodeTypeDeclaration bbType = null;
			if (!codeTypes.TryGetValue (entry.Identifier as string, out bbType))
			{
				bbType = new CodeTypeDeclaration (entry.Identifier as string);
				bbType.BaseTypes.Add (new CodeTypeReference (typeof(MonoBehaviour)));
				cNamespace.Types.Add (bbType);
				codeTypes.Add (entry.Identifier as string, bbType);
			}

			foreach (var fieldOp in (entry.Context as Context).Entries)
			{
				var op = fieldOp as Operator;
				var typeName = (((op.Context as Expression).Operands [0] as ExprAtom).Content as Scope).Parts [0] as string;
				CodeMemberField field = null;
				if (typeName == null)
				{
					var listFunc = (((op.Context as Expression).Operands [0] as ExprAtom).Content as Scope).Parts [0] as FunctionCall;
					typeName = ((listFunc.Args [0].Operands [0] as ExprAtom).Content as Scope).Parts [0] as string;

					Debug.Log ("LIST: " + typeName);
					var listType = String.Format ("System.Collections.Generic.List<{0}>", types [typeName]);
					field = new CodeMemberField (new CodeTypeReference (listType), op.Identifier as string);
					field.InitExpression = new CodeSnippetExpression (String.Format ("new {0}()", listType));
				} else
				{

					Debug.Log (typeName);
					field = new CodeMemberField (types [typeName], op.Identifier as string);
				}
				field.Attributes = MemberAttributes.Public;
				bbType.Members.Add (field);
			}
		}
		var loader = UnityEngine.Object.FindObjectOfType<ScriptsLoader> ();
		CSharpCodeProvider codeProvider = new CSharpCodeProvider ();
		CodeGeneratorOptions options = new CodeGeneratorOptions ();
		var writer = new StringWriter ();
		codeProvider.GenerateCodeFromNamespace (cNamespace, writer, options);
		Debug.Log (writer.ToString ());
		OnCompiled (loader.Load (new string[]{ writer.ToString () }, "BlackboardsData"));
		//onCompiled ();
	}

	public ContextSwitchInterpreter Ctx { get; internal set; }

	void OnCompiled (Assembly asm)
	{
		//AppDomain.CurrentDomain.Load (asm.GetName ());
		//asm.GetName ().SetPublicKeyToken (new byte[]{ 12, 13, 48, 2 });

		Debug.LogWarning (asm.FullName);
		Engine.AddAssembly (asm);
		foreach (var key in codeTypes.Keys)
			Debug.Log (Engine.GetType (key));
		

//		Ctx = new ContextSwitchInterpreter (type, Engine);
//
//		onCompiled ();
	}

}
