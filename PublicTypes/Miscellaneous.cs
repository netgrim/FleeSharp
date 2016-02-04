using System;
using System.Collections.Generic;

// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public License
// as published by the Free Software Foundation; either version 2.1
// of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free
// Software Foundation, Inc., 59 Temple Place, Suite 330, Boston,
// MA 02111-1307, USA.
// 
// Flee# - A port of Eugene Ciloci's Flee to C#
// Copyright © 2012 Yoni Gozman
//

namespace Ciloci.Flee
{
	

	/// <include file='Resources/DocComments.xml' path='DocComments/IExpression/Class/*' />	
	public interface IExpression
	{

		/// <include file='Resources/DocComments.xml' path='DocComments/IExpression/Clone/*' />	
		IExpression Clone();
		/// <include file='Resources/DocComments.xml' path='DocComments/IExpression/Text/*' />	
		string Text { get; }
		/// <include file='Resources/DocComments.xml' path='DocComments/IExpression/Info/*' />	
		ExpressionInfo Info { get; }
		/// <include file='Resources/DocComments.xml' path='DocComments/IExpression/Context/*' />	
		ExpressionContext Context { get; }
		/// <include file='Resources/DocComments.xml' path='DocComments/IExpression/Owner/*' />	
		object Owner { get; set; }
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/IDynamicExpression/Class/*' />	
	public interface IDynamicExpression : IExpression
	{
		/// <include file='Resources/DocComments.xml' path='DocComments/IDynamicExpression/Evaluate/*' />	
		object Evaluate();
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/IGenericExpression/Class/*' />	
	public interface IGenericExpression<T> : IExpression
	{
		/// <include file='Resources/DocComments.xml' path='DocComments/IGenericExpression/Evaluate/*' />	
		T Evaluate();
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionInfo/Class/*' />	
	public sealed class ExpressionInfo
	{


		private IDictionary<string, object> MyData;
		internal ExpressionInfo()
		{
			MyData = new Dictionary<string, object>();
			MyData.Add("ReferencedVariables", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
		}

		internal void AddReferencedVariable(string name)
		{
            IDictionary<string, string> dict = MyData["ReferencedVariables"] as IDictionary<string, string>;
			dict[name] = name;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionInfo/GetReferencedVariables/*' />	
		public string[] GetReferencedVariables()
		{
            IDictionary<string, string> dict = MyData["ReferencedVariables"] as IDictionary<string, string>;
			string[] arr = new string[dict.Count];
			dict.Keys.CopyTo(arr, 0);
			return arr;
		}
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOwnerMemberAccessAttribute/Class/*' />	
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class ExpressionOwnerMemberAccessAttribute : Attribute
	{


		private bool MyAllowAccess;
		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOwnerMemberAccessAttribute/New/*' />	
		public ExpressionOwnerMemberAccessAttribute(bool allowAccess)
		{
			MyAllowAccess = allowAccess;
		}

		internal bool AllowAccess {
			get { return MyAllowAccess; }
		}
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ResolveVariableTypeEventArgs/Class/*' />
	public class ResolveVariableTypeEventArgs : EventArgs
	{

		private string MyName;

		private Type MyType;
		internal ResolveVariableTypeEventArgs(string name)
		{
			this.MyName = name;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ResolveVariableTypeEventArgs/VariableName/*' />
		public string VariableName {
			get { return MyName; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ResolveVariableTypeEventArgs/VariableType/*' />
		public Type VariableType {
			get { return MyType; }
			set { MyType = value; }
		}
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ResolveVariableValueEventArgs/Class/*' />
	public class ResolveVariableValueEventArgs : EventArgs
	{

		private string MyName;
		private Type MyType;

		private object MyValue;
		internal ResolveVariableValueEventArgs(string name, Type t)
		{
			MyName = name;
			MyType = t;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ResolveVariableValueEventArgs/VariableName/*' />
		public string VariableName {
			get { return MyName; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ResolveVariableValueEventArgs/VariableType/*' />
		public Type VariableType {
			get { return MyType; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ResolveVariableValueEventArgs/VariableValue/*' />
		public object VariableValue {
			get { return MyValue; }
			set { MyValue = value; }
		}
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ResolveFunctionEventArgs/Class/*' />
	public class ResolveFunctionEventArgs : EventArgs
	{

		private string MyName;
		private Type[] MyArgumentTypes;

		private Type MyReturnType;
		internal ResolveFunctionEventArgs(string name, Type[] argumentTypes)
		{
			MyName = name;
			MyArgumentTypes = argumentTypes;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ResolveFunctionEventArgs/FunctionName/*' />
		public string FunctionName {
			get { return MyName; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ResolveFunctionEventArgs/ArgumentTypes/*' />
		public Type[] ArgumentTypes {
			get { return MyArgumentTypes; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ResolveFunctionEventArgs/ReturnType/*' />
		public Type ReturnType {
			get { return MyReturnType; }
			set { MyReturnType = value; }
		}
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/InvokeFunctionEventArgs/Class/*' />
	public class InvokeFunctionEventArgs : EventArgs
	{

		private string MyName;
		private object[] MyArguments;

		private object MyFunctionResult;
		internal InvokeFunctionEventArgs(string name, object[] arguments)
		{
			MyName = name;
			MyArguments = arguments;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/InvokeFunctionEventArgs/FunctionName/*' />
		public string FunctionName {
			get { return MyName; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/InvokeFunctionEventArgs/Arguments/*' />
		public object[] Arguments {
			get { return MyArguments; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/InvokeFunctionEventArgs/Result/*' />
		public object Result {
			get { return MyFunctionResult; }
			set { MyFunctionResult = value; }
		}
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/RealLiteralDataType/Class/*' />	
	public enum RealLiteralDataType
	{
		/// <include file='Resources/DocComments.xml' path='DocComments/RealLiteralDataType/Single/*' />	
		Single,
		/// <include file='Resources/DocComments.xml' path='DocComments/RealLiteralDataType/Double/*' />	
		Double,
		/// <include file='Resources/DocComments.xml' path='DocComments/RealLiteralDataType/Decimal/*' />	
		Decimal
	}
}
