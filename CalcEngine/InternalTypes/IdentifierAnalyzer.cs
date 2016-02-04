
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PerCederberg.Grammatica.Runtime;

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

	internal class IdentifierAnalyzer : Analyzer
	{

		private IDictionary<int, string> MyIdentifiers;
		private int MyMemberExpressionCount;

		private bool MyInFieldPropertyExpression;
		public IdentifierAnalyzer()
		{
			MyIdentifiers = new Dictionary<int, string>();
		}

		public override Node Exit(Node node)
		{
            switch ((ExpressionConstants)node.Id)
            {
				case ExpressionConstants.IDENTIFIER:
					this.ExitIdentifier((Token)node);
					break;
				case ExpressionConstants.FIELD_PROPERTY_EXPRESSION:
					this.ExitFieldPropertyExpression();
					break;
			}

			return node;
		}

		public override void Enter(Node node)
		{
            switch ((ExpressionConstants)node.Id)
            {
				case ExpressionConstants.MEMBER_EXPRESSION:
					this.EnterMemberExpression();
					break;
				case ExpressionConstants.FIELD_PROPERTY_EXPRESSION:
					this.EnterFieldPropertyExpression();
					break;
			}
		}

		private void ExitIdentifier(Token node)
		{
			if (MyInFieldPropertyExpression == false) {
				return;
			}

			if (MyIdentifiers.ContainsKey(MyMemberExpressionCount) == false) {
				MyIdentifiers.Add(MyMemberExpressionCount, node.Image);
			}
		}

		private void EnterMemberExpression()
		{
			MyMemberExpressionCount += 1;
		}

		private void EnterFieldPropertyExpression()
		{
			MyInFieldPropertyExpression = true;
		}

		private void ExitFieldPropertyExpression()
		{
			MyInFieldPropertyExpression = false;
		}

		public override void Reset()
		{
			MyIdentifiers.Clear();
			MyMemberExpressionCount = -1;
		}

		public ICollection<string> GetIdentifiers(ExpressionContext context)
		{
			Dictionary<string, object> dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			ExpressionImports ei = context.Imports;

			foreach (string identifier in MyIdentifiers.Values) {
				// Skip names registered as namespaces
				if (ei.HasNamespace(identifier) == true) {
					continue;
				} else if (context.Variables.ContainsKey(identifier) == true) {
					// Identifier is a variable
					continue;
				}

				// Get only the unique values
				dict[identifier] = null;
			}

			return dict.Keys;
		}
	}
}
