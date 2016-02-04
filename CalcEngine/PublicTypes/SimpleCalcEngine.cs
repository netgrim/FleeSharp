
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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

namespace Ciloci.Flee.CalcEngine
{

	public class SimpleCalcEngine
	{

		#region "Fields"

		private IDictionary<string, IExpression> MyExpressions;

		private ExpressionContext MyContext;
		#endregion

		#region "Constructor"

		public SimpleCalcEngine()
		{
			MyExpressions = new Dictionary<string, IExpression>(StringComparer.OrdinalIgnoreCase);
			MyContext = new ExpressionContext();
		}

		#endregion

		#region "Methods - Private"

		private void AddCompiledExpression(string expressionName, IExpression expression)
		{
			if (MyExpressions.ContainsKey(expressionName) == true) {
				throw new InvalidOperationException(string.Format("The calc engine already contains an expression named '{0}'", expressionName));
			} else {
				MyExpressions.Add(expressionName, expression);
			}
		}

		private ExpressionContext ParseAndLink(string expressionName, string expression)
		{
			IdentifierAnalyzer analyzer = Context.ParseIdentifiers(expression);

			ExpressionContext context2 = MyContext.CloneInternal(true);
			this.LinkExpression(expressionName, context2, analyzer);

			// Tell the expression not to clone the context since it's already been cloned
			context2.NoClone = true;

			// Clear our context's variables
			MyContext.Variables.Clear();

			return context2;
		}

		private void LinkExpression(string expressionName, ExpressionContext context, IdentifierAnalyzer analyzer)
		{
			foreach (string identifier in analyzer.GetIdentifiers(context)) {
				this.LinkIdentifier(identifier, expressionName, context);
			}
		}

		private void LinkIdentifier(string identifier, string expressionName, ExpressionContext context)
		{
			IExpression child = null;

			if (MyExpressions.TryGetValue(identifier, out child) == false) {
				string msg = string.Format("Expression '{0}' references unknown name '{1}'", expressionName, identifier);
				throw new InvalidOperationException(msg);
			}

			context.Variables.Add(identifier, child);
		}

		#endregion

		#region "Methods - Public"

		public void AddDynamic(string expressionName, string expression)
		{
			ExpressionContext linkedContext = this.ParseAndLink(expressionName, expression);
			IExpression e = linkedContext.CompileDynamic(expression);
			this.AddCompiledExpression(expressionName, e);
		}

		public void AddGeneric<T>(string expressionName, string expression)
		{
			ExpressionContext linkedContext = this.ParseAndLink(expressionName, expression);
			IExpression e = linkedContext.CompileGeneric<T>(expression);
			this.AddCompiledExpression(expressionName, e);
		}

		public void Clear()
		{
			MyExpressions.Clear();
		}

		#endregion

		#region "Properties - Public"

		public IExpression this[string name] {
			get {
				IExpression e = null;
				MyExpressions.TryGetValue(name, out e);
				return e;
			}
		}

		public ExpressionContext Context {
			get { return MyContext; }
			set { MyContext = value; }
		}

		#endregion
	}

}
