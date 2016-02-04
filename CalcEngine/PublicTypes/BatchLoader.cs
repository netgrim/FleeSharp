
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

namespace Ciloci.Flee.CalcEngine
{

	/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="BatchLoader"]/*' />
	public sealed class BatchLoader
	{

		private IDictionary<string, BatchLoadInfo> MyNameInfoMap;

		private DependencyManager<string> MyDependencies;
		internal BatchLoader()
		{
			MyNameInfoMap = new Dictionary<string, BatchLoadInfo>(StringComparer.OrdinalIgnoreCase);
			MyDependencies = new DependencyManager<string>(StringComparer.OrdinalIgnoreCase);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="BatchLoader.Add"]/*' />
		public void Add(string atomName, string expression, ExpressionContext context)
		{
			Utility.AssertNotNull(atomName, "atomName");
			Utility.AssertNotNull(expression, "expression");
			Utility.AssertNotNull(context, "context");

			BatchLoadInfo info = new BatchLoadInfo(atomName, expression, context);
			MyNameInfoMap.Add(atomName, info);
			MyDependencies.AddTail(atomName);

			ICollection<string> references = this.GetReferences(expression, context);

			foreach (string reference in references) {
				MyDependencies.AddTail(reference);
				MyDependencies.AddDepedency(reference, atomName);
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@atomName="BatchLoader.Contains"]/*' />
		public bool Contains(string atomName)
		{
			return MyNameInfoMap.ContainsKey(atomName);
		}

		internal BatchLoadInfo[] GetBachInfos()
		{
			string[] tails = MyDependencies.GetTails();
			Queue<string> sources = MyDependencies.GetSources(tails);

			IList<string> result = MyDependencies.TopologicalSort(sources);

			BatchLoadInfo[] infos = new BatchLoadInfo[result.Count];

			for (int i = 0; i <= result.Count - 1; i++) {
				infos[i] = MyNameInfoMap[result[i]];
			}

			return infos;
		}

		private ICollection<string> GetReferences(string expression, ExpressionContext context)
		{
			IdentifierAnalyzer analyzer = context.ParseIdentifiers(expression);

			return analyzer.GetIdentifiers(context);
		}
	}

}
