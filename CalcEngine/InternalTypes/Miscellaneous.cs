
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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

using System.Reflection.Emit;

namespace Ciloci.Flee.CalcEngine
{

	internal class PairEqualityComparer : EqualityComparer<ExpressionResultPair>
	{

		public override bool Equals(ExpressionResultPair x, ExpressionResultPair y)
		{
			return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode(ExpressionResultPair obj)
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
		}
	}

	internal abstract class ExpressionResultPair
	{

		private string MyName;

		protected IDynamicExpression MyExpression;

		protected ExpressionResultPair()
		{
		}

		public abstract void Recalculate();

		public void SetExpression(IDynamicExpression e)
		{
			MyExpression = e;
		}

		public void SetName(string name)
		{
			MyName = name;
		}

		public override string ToString()
		{
			return MyName;
		}

		public string Name {
			get { return MyName; }
		}

		public abstract Type ResultType { get; }
		public abstract object ResultAsObject { get; }

		public IDynamicExpression Expression {
			get { return MyExpression; }
		}
	}

	internal class GenericExpressionResultPair<T> : ExpressionResultPair
	{


		public T MyResult;

		public GenericExpressionResultPair()
		{
		}

		public override void Recalculate()
		{
			MyResult = (T)MyExpression.Evaluate();
		}

		public T Result {
			get { return MyResult; }
		}

		public override System.Type ResultType {
			get { return typeof(T); }
		}

		public override object ResultAsObject {
			get { return MyResult; }
		}
	}

	internal class BatchLoadInfo
	{
		public string Name;
		public string ExpressionText;

		public ExpressionContext Context;
		public BatchLoadInfo(string name, string text, ExpressionContext context)
		{
			this.Name = name;
			this.ExpressionText = text;
			this.Context = context;
		}
	}

	/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="NodeEventArgs"]/*' />	
	public sealed class NodeEventArgs : EventArgs
	{

		private string MyName;

		private object MyResult;

		internal NodeEventArgs()
		{
		}

		internal void SetData(string name, object result)
		{
			MyName = name;
			MyResult = result;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="NodeEventArgs.Name"]/*' />	
		public string Name {
			get { return MyName; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="NodeEventArgs.Result"]/*' />	
		public object Result {
			get { return MyResult; }
		}
	}

}
