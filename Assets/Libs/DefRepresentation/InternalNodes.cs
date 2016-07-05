﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using PerCederberg.Grammatica.Runtime;

namespace InternalDSL
{
	
	public class Root
	{
		public List<Operator> Operators { get; internal set; }

		public Root (Node n)
		{
			Operators = new List<Operator> ();
			int count = n.Count;
			for (int i = 0; i < count; i++)
				Operators.Add (new Operator (n.GetChildAt (i)));
		}

		public void Show ()
		{
			for (int i = 0; i < Operators.Count; i++)
				Debug.Log (Operators [i]);
		}
	}



	public class FunctionCall
	{
		public string Name;
		public Expression[] Args;

		public FunctionCall (Node n)
		{
			var idNode = n.GetChildAt (0);
			Name = (idNode as Token).Image;
			var callNode = n.GetChildAt (2);
			if (callNode.Id == (int)DefConstants.CALL_ARGS)
			{
				//It's a function call with some arguments
				int argsCount = (callNode.Count - 1) / 2 + 1;

				Args = new Expression[argsCount];
				for (int i = 0; i < argsCount; i++)
				{
					var argNode = callNode.GetChildAt (i * 2);
					//Debug.Log (argNode);
					Expression expr = new Expression (argNode);
					Args [i] = expr;
				}

			}
		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder (100);
			builder.Append (Name).Append ("(");
			for (int i = 0; i < Args.Length; i++)
				builder.Append (Args [i]).Append (", ");
			if (builder.Length > Name.Length)
				builder.Length -= 2;
			builder.Append (")");
			return builder.ToString ();
		}
	}

	public class ExprAtom
	{
		public enum UnaryOp
		{
			Not,
			Inverse,
			None
		}

		public UnaryOp Op = UnaryOp.None;
		public object Content;

		public ExprAtom (Node exprNode)
		{
			
			var firstChild = exprNode.GetChildAt (0);
			if (firstChild.Id == (int)DefConstants.NOT || firstChild.Id == (int)DefConstants.SUB)
			{
				firstChild = exprNode.GetChildAt (1);
				Op = firstChild.Id == (int)DefConstants.NOT ? UnaryOp.Not : UnaryOp.Inverse;
				if (firstChild.Id == (int)DefConstants.OPEN_PARENT)
					firstChild = exprNode.GetChildAt (2);
			} 

			if (firstChild.Id == (int)DefConstants.EXPRESSION)
			{
				Content = new Expression (firstChild);
			} else if (firstChild.Id == (int)DefConstants.SCOPE)
			{
				Content = new Scope (firstChild);
			} else if (firstChild.Id == (int)DefConstants.ATOM)
			{
				Content = Atom.FromNode (firstChild);
			} else
			{
				Content = new Expression (firstChild, false);
			}
		}

		public override string ToString ()
		{
			if (Op == UnaryOp.None)
				return string.Format ("({0})", Content);
			else if (Op == UnaryOp.Inverse)
				return string.Format ("(!{0})", Content);
			else
				return string.Format ("(-{0}", Content);
		}
	}

	public class Expression
	{
		public object[] Operands;

		Type type = null;

		public Type ExpressionType ()
		{
			if (type != null)
				return type;

			if (Operands.Length > 1)
			{
				var firstOperator = (BinaryOp)Operands [1];
				switch (firstOperator)
				{
				case BinaryOp.Add:
				case BinaryOp.Div:
				case BinaryOp.Mul:
				case BinaryOp.Sub:
					return typeof(int);
				}
			}
			return typeof(string);
		}

		public enum BinaryOp
		{
			Add,
			Sub,
			Div,
			Mul,
			Equals,
			More,
			Less,
			NotEquals,
			MoreOrEquals,
			LessOrEquals,
			And,
			Or,
			Undefined
		}

		public string OpToString (BinaryOp op)
		{
			switch (op)
			{
			case BinaryOp.Add:
				return " + ";
			case BinaryOp.Sub:
				return " - ";
			case BinaryOp.Div:
				return " / ";
			case BinaryOp.Mul:
				return " * ";
			case BinaryOp.Equals:
				return " == ";
			case BinaryOp.More:
				return " > ";
			case BinaryOp.Less:
				return " < ";
			case BinaryOp.NotEquals:
				return " != ";
			case BinaryOp.MoreOrEquals:
				return " >= ";
			case BinaryOp.LessOrEquals:
				return " <=+> ";
			case BinaryOp.And:
				return " && ";
			case BinaryOp.Or:
				return " || ";
			case BinaryOp.Undefined:
				return " undefined_operator ";
			}
			return null;
		}

		BinaryOp BinaryOpFromCode (Node n)
		{
			switch (n.Id)
			{
			case (int)DefConstants.ADD:
				return BinaryOp.Add;
			case (int)DefConstants.SUB:
				return BinaryOp.Sub;
			case (int)DefConstants.MUL:
				return BinaryOp.Mul;
			case (int)DefConstants.DIV:
				return BinaryOp.Div;
			case (int)DefConstants.EQUALS:
				return BinaryOp.Equals;
			case (int)DefConstants.NOTEQUALS:
				return BinaryOp.NotEquals;
			case (int)DefConstants.AND:
				return BinaryOp.And;
			case (int)DefConstants.OR:
				return BinaryOp.Or;
			case (int)DefConstants.MORE:
				return BinaryOp.More;
			case (int)DefConstants.LESS:
				return BinaryOp.Less;
			case (int)DefConstants.MOREOREQUALS:
				return BinaryOp.MoreOrEquals;
			case (int)DefConstants.LESSOREQUALS:
				return BinaryOp.LessOrEquals;
			}
			return BinaryOp.Undefined;
		}

		public Expression (Node n, bool isExprNode = true)
		{
			
			var exprNode = isExprNode ? n.GetChildAt (0) : n;
//			Debug.LogFormat ("Expr {0} {1}", n, exprNode);
			while (exprNode.Count == 1 && exprNode.Id != (int)DefConstants.FACTOR)
			{
//				if (exprNode.Id != (int)DefConstants.SUB)
				exprNode = exprNode.GetChildAt (0);
			}

//			Debug.LogFormat ("Found {0} {1}", n, exprNode);
			if (exprNode.Id == (int)DefConstants.FACTOR)
			{
				ExprAtom atom = new ExprAtom (exprNode);
				Operands = new object[]{ atom };
			} else
			{
				int childrenCount = exprNode.Count;
				Operands = new object[childrenCount];
				bool op = false;
			
				for (int i = 0; i < childrenCount; i++)
				{
					var childNode = exprNode.GetChildAt (i);
					if (op)
					{
						Operands [i] = BinaryOpFromCode (childNode);
						op = false;
					} else
					{
						Operands [i] = new ExprAtom (childNode);
						op = true;
					}
				}
					
			}
		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder (100);
			builder.Append ("EXPR:");
			for (int i = 0; i < Operands.Length; i++)
			{
				if (i % 2 == 0)
					builder.Append (Operands [i]).Append (" ");
				else
					builder.Append (OpToString ((BinaryOp)Operands [i])).Append (" ");
			}
			return builder.ToString ();
		}
	}

	public struct IdWrapper
	{
		public string Id;

		public override string ToString ()
		{
			return Id;
		}

		static public implicit operator string (IdWrapper wrapper)
		{
			return wrapper.Id;
		}
	}

	public static class Atom
	{
		public static object FromNode (Node n)
		{
			if (n.Id == (int)DefConstants.ATOM)
				n = n.GetChildAt (0);
			switch (n.Id)
			{
			case (int)DefConstants.IDENTIFIER_OR_CALL:
				{
					var possibleArgs = n.GetChildAt (2);
					if (possibleArgs != null)
					{
						return new FunctionCall (n);
					} else
					{
						return new IdWrapper (){ Id = (n.GetChildAt (0) as Token).Image };
					}
				}
			case (int)DefConstants.DECIMAL:
				{
					var child = n.GetChildCount ();
					if (child == 1)
						return float.Parse ((n.GetChildAt (0) as Token).Image);
					else
						return float.Parse (String.Concat ((n.GetChildAt (0) as Token).Image, '.', (n.GetChildAt (2) as Token).Image));
					//var data = (n as Token).Image;
					//return int.Parse (data);
				}
			case (int)DefConstants.STRING:
				{
					var data = (n as Token).Image;
					return data;
				}
			case (int)DefConstants.TRUE:
				{
					return true;
				}
			case (int)DefConstants.FALSE:
				{
					return false;
				}
			}
			Debug.Log (n);
			return n;
		}
	}

	public class Context
	{
		public List<object> Entries = new List<object> ();

		public List<Expression> Args = new List<Expression> ();

		public Context ()
		{
			
		}

		public Context (Node argsNode, Node n)
		{
			if (argsNode != null)
			{
				//It's a function call with some arguments
				int argsCount = (argsNode.Count - 1) / 2 + 1;
				for (int i = 0; i < argsCount; i++)
				{
					var argNode = argsNode.GetChildAt (i * 2);
					//Debug.LogFormat ("Context arg: {0} in {1}", argNode, argsNode);
					Expression expr = new Expression (argNode);
					Args.Add (expr);
				}
			}
			var firstChild = n.GetChildAt (0);
			if (firstChild.Id == (int)DefConstants.OPEN_TABLE)
			{
				//It's a table
				if (argsNode != null)
				{
					//Input context is a value, while others - args
					Operator op = new Operator ();
					op.Identifier = "value";
					op.Context = new Context (null, n);
					Entries.Add (op);
				} else
				{

					var listNode = n.GetChildAt (1);
					if (listNode.GetChildAt (0).Id == (int)DefConstants.OPEN_TABLE)
					{
						//it's a list of structs
					} else
					{
						int childrenCount = listNode.Count / 3;
						if (listNode.Count > 1 && listNode.GetChildAt (1).Id == (int)DefConstants.EQUALS)
						{
							//It's about operators
							for (int i = 0; i < childrenCount; i++)
							{
								var idNode = listNode.GetChildAt (i * 3);
								var contextNode = listNode.GetChildAt (i * 3 + 2);
								//Debug.LogFormat ("{0} {1} {2} {3}", idNode, contextNode, i * 3, i * 3 + 2);
								Operator op = new Operator (idNode, contextNode);
								Entries.Add (op);
							}
						} else
						{
							//It's about atoms
							//						Operator op = new Operator ();
							//						op.Identifier = "list";
							//						op.Context = new Context ();
							//						var context = op.Context as Context;
							childrenCount = listNode.Count;
							for (int i = 0; i < childrenCount; i++)
							{
								var atom = Atom.FromNode (listNode.GetChildAt (i));
								//Debug.Log (atom);
								Entries.Add (atom);
							}
							//						Entries.Add (op);
						}
					}


				}
			} else if (firstChild.Id == (int)DefConstants.EXPRESSION)
			{

//				if (argsNode == null)
//					Entries.Add (new Expression (firstChild));
//				else
//				{
				Entries.Add (new Expression (firstChild));
//				}
			}

		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder (100);
			if (Args.Count > 0)
			{
				builder.Append ("ARGS<");
				for (int i = 0; i < Args.Count; i++)
				{
					builder.Append (Args [i]);
					builder.Append (", ");
				}
				builder.Append (">");

			}
			if (Entries.Count > 0)
			{
				if (Entries.Count > 1 || Entries [0] is Operator)
					builder.Append ("{").Append (Environment.NewLine);
				for (int i = 0; i < Entries.Count - 1; i++)
				{
					builder.Append (Entries [i]);
					builder.Append (Environment.NewLine);
				}
				builder.Append (Entries [Entries.Count - 1]);
				if (Entries.Count > 1 || Entries [0] is Operator)
					builder.Append ("}");
				builder.Append (Environment.NewLine);
			}

			return builder.ToString ();
		}
	}

	public class Operator
	{
		public object Identifier;
		public object Context;
		public List<Expression> Args;


		public enum ContextType
		{
			Table,
			Expression
		}

		public ContextType ConType;

		public Operator (Node idNode, Node n)
		{
			var idOrCall = idNode.GetChildAt (0);
			//Debug.Log (idOrCall);
			if (idNode.Count == 1)
			{
				//It's a simple operator
				Identifier = (idOrCall as Token).Image;
				Context = new Context (null, n);
			} else
			{
				//It's a function-operator
				Identifier = (idOrCall as Token).Image;
				Context = new Context (idNode.GetChildAt (2), n);
			}

			var ctx = (Context as Context);
			Args = ctx.Args;
			if (ctx.Entries.Count == 1 && ctx.Entries [0] is Expression)
				Context = ctx.Entries [0];

		}

		public Operator (Node n)
		{
			var idNode = n.GetChildAt (0);
			n = n.GetChildAt (2);
			var idOrCall = idNode.GetChildAt (0);
			if (idNode.Count == 1)
			{
				//It's a simple operator
				Identifier = (idOrCall as Token).Image;
				Context = new Context (null, n);
			} else
			{
				//It's a function-operator
				Identifier = (idOrCall.GetChildAt (0) as Token).Image;
				Context = new Context (idOrCall.GetChildAt (2), n);
			}
			var ctx = (Context as Context);
			Args = ctx.Args;
			if (ctx.Entries.Count == 1 && ctx.Entries [0] is Expression)
				Context = ctx.Entries [0];

			//ctx.Entries = (valueOp.Context as Context).
		}

		public Operator ()
		{
			var ctx = (Context as Context);
			Args = ctx.Args;
			if (ctx.Entries.Count == 1 && ctx.Entries [0] is Expression)
				Context = ctx.Entries [0];
		}

		public override string ToString ()
		{
			return String.Format ("{0} = {1}", Identifier, Context);
		}
	}

	public class Scope
	{
		public object[] Parts;

		public enum ScopeType
		{
			FunctionScope,
			CommonScope
		}

		public ScopeType Type;

		public Scope (Node n)
		{
			int childCount = (n.Count - 1) / 2 + 1;
			Parts = new object[childCount];
			if (n.Id == (int)DefConstants.FUNC_SCOPE)
			{
				for (int i = 0; i < childCount; i++)
				{
					var argNode = n.GetChildAt (i * 2);
					Parts [i] = (argNode as Token).Image;
				}
				Type = ScopeType.FunctionScope;
			} else if (n.Id == (int)DefConstants.SCOPE)
			{
				for (int i = 0; i < childCount; i++)
				{
					var argNode = n.GetChildAt (i * 2);
					var idNode = argNode.GetChildAt (0);
					var callNode = argNode.GetChildAt (2);
					if (callNode != null)
					{
						//It's a function call
						FunctionCall call = new FunctionCall (argNode);
						Parts [i] = call;
					} else
						Parts [i] = (idNode as Token).Image;
				}
				Type = ScopeType.CommonScope;
			}
		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder (100);
			builder.Append ("SCOPE<");
			for (int i = 0; i < Parts.Length; i++)
				builder.Append (Parts [i]).Append (".");
			if (builder.Length > 0)
				builder.Length -= 1;
			builder.Append (">");
			return builder.ToString ();
		}
	}



}

