
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using System.Text.RegularExpressions;

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

	/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine"]/*' />	
	public class CalculationEngine
	{

		#region "Fields"

		private DependencyManager<ExpressionResultPair> MyDependencies;
		// Map of name to node

		private Dictionary<string, ExpressionResultPair> MyNameNodeMap;
		#endregion

		#region "Events"

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.NodeRecalculated"]/*' />	
		public event EventHandler<NodeEventArgs> NodeRecalculated;

		#endregion

		#region "Constructor"

		public CalculationEngine()
		{
			MyDependencies = new DependencyManager<ExpressionResultPair>(new PairEqualityComparer());
			MyNameNodeMap = new Dictionary<string, ExpressionResultPair>(StringComparer.OrdinalIgnoreCase);
		}

		#endregion

		#region "Methods - Private"

		private void AddTemporaryHead(string headName)
		{
			GenericExpressionResultPair<int> pair = new GenericExpressionResultPair<int>();
			pair.SetName(headName);

			if (MyNameNodeMap.ContainsKey(headName) == false) {
				MyDependencies.AddTail(pair);
				MyNameNodeMap.Add(headName, pair);
			} else {
				throw new ArgumentException(string.Format("An expression already exists at '{0}'", headName));
			}
		}

		private void DoBatchLoadAdd(BatchLoadInfo info)
		{
			try {
				this.Add(info.Name, info.ExpressionText, info.Context);
			} catch (ExpressionCompileException ex) {
				this.Clear();
				throw new BatchLoadCompileException(info.Name, info.ExpressionText, ex);
			}
		}

		private ExpressionResultPair GetTail(string tailName)
		{
			Utility.AssertNotNull(tailName, "name");
			ExpressionResultPair pair = null;
			MyNameNodeMap.TryGetValue(tailName, out pair);
			return pair;
		}

		private ExpressionResultPair GetTailWithValidate(string tailName)
		{
			Utility.AssertNotNull(tailName, "name");
			ExpressionResultPair pair = this.GetTail(tailName);

			if (pair == null) {
				throw new ArgumentException(string.Format("No expression is associated with the name '{0}'", tailName));
			} else {
				return pair;
			}
		}

		private string[] GetNames(IList<ExpressionResultPair> pairs)
		{
			string[] names = new string[pairs.Count];

			for (int i = 0; i <= names.Length - 1; i++) {
				names[i] = pairs[i].Name;
			}

			return names;
		}

		private ExpressionResultPair[] GetRootTails(string[] roots)
		{
			// No roots supplied so get everything
			if (roots.Length == 0) {
				return MyDependencies.GetTails();
			}

			// Get the tail for each name
			ExpressionResultPair[] arr = new ExpressionResultPair[roots.Length];

			for (int i = 0; i <= arr.Length - 1; i++) {
				arr[i] = this.GetTailWithValidate(roots[i]);
			}

			return arr;
		}

		#endregion

		#region "Methods - Internal"

		internal void FixTemporaryHead(IDynamicExpression expression, ExpressionContext context, Type resultType)
		{
			Type pairType = typeof(GenericExpressionResultPair<>);
			pairType = pairType.MakeGenericType(resultType);

            ExpressionResultPair pair = (ExpressionResultPair)Activator.CreateInstance(pairType);
			string headName = context.CalcEngineExpressionName;
			pair.SetName(headName);
			pair.SetExpression(expression);

			ExpressionResultPair oldPair = MyNameNodeMap[headName];
			MyDependencies.ReplaceDependency(oldPair, pair);
			MyNameNodeMap[headName] = pair;

			// Let the pair store the result of its expression
			pair.Recalculate();
		}

		// Called by an expression when it references another expression in the engine
		internal void AddDependency(string tailName, ExpressionContext context)
		{
			ExpressionResultPair actualTail = this.GetTail(tailName);
			string headName = context.CalcEngineExpressionName;
			ExpressionResultPair actualHead = this.GetTail(headName);

			// An expression could depend on the same reference more than once (ie: "a + a * a")
			MyDependencies.AddDepedency(actualTail, actualHead);
		}

		internal Type ResolveTailType(string tailName)
		{
			ExpressionResultPair actualTail = this.GetTail(tailName);
			return actualTail.ResultType;
		}

		internal bool HasTail(string tailName)
		{
			return MyNameNodeMap.ContainsKey(tailName);
		}

		internal void EmitLoad(string tailName, FleeILGenerator ilg)
		{
			PropertyInfo pi = typeof(ExpressionContext).GetProperty("CalculationEngine");
			ilg.Emit(OpCodes.Callvirt, pi.GetGetMethod());

			// Load the tail
			MemberInfo[] methods = typeof(CalculationEngine).FindMembers(MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public, Type.FilterNameIgnoreCase, "GetResult");
			MethodInfo mi = null;

			foreach (MethodInfo method in methods) {
				if (method.IsGenericMethod == true) {
					mi = method;
					break; // TODO: might not be correct. Was : Exit For
				}
			}

			Type resultType = this.ResolveTailType(tailName);

			mi = mi.MakeGenericMethod(resultType);

			ilg.Emit(OpCodes.Ldstr, tailName);
			ilg.Emit(OpCodes.Call, mi);
		}

		#endregion

		#region "Methods - Public"

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.Add"]/*' />	
		public void Add(string atomName, string expression, ExpressionContext context)
		{
			Utility.AssertNotNull(atomName, "atomName");
			Utility.AssertNotNull(expression, "expression");
			Utility.AssertNotNull(context, "context");

			this.AddTemporaryHead(atomName);

			context.SetCalcEngine(this, atomName);

			context.CompileDynamic(expression);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.Remove"]/*' />	
		public bool Remove(string name)
		{
			ExpressionResultPair tail = this.GetTail(name);

			if (tail == null) {
				return false;
			}

			ExpressionResultPair[] dependents = MyDependencies.GetDependents(tail);
			MyDependencies.Remove(dependents);

			foreach (ExpressionResultPair pair in dependents) {
				MyNameNodeMap.Remove(pair.Name);
			}

			return true;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.CreateBatchLoader"]/*' />
		public BatchLoader CreateBatchLoader()
		{
			BatchLoader loader = new BatchLoader();
			return loader;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.BatchLoad"]/*' />
		public void BatchLoad(BatchLoader loader)
		{
			Utility.AssertNotNull(loader, "loader");
			this.Clear();

			BatchLoadInfo[] infos = loader.GetBachInfos();

			foreach (BatchLoadInfo info in infos) {
				this.DoBatchLoadAdd(info);
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.GetResult"]/*' />	
		public T GetResult<T>(string name)
		{
			ExpressionResultPair tail = this.GetTailWithValidate(name);

			if ((!object.ReferenceEquals(typeof(T), tail.ResultType))) {
				string msg = string.Format("The result type of '{0}' ('{1}') does not match the supplied type argument ('{2}')", name, tail.ResultType.Name, typeof(T).Name);
				throw new ArgumentException(msg);
			}

            GenericExpressionResultPair<T> actualTail = (GenericExpressionResultPair<T>)tail;
			return actualTail.Result;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.GetResult2"]/*' />	
		public object GetResult(string name)
		{
			ExpressionResultPair tail = this.GetTailWithValidate(name);
			return tail.ResultAsObject;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.GetExpression"]/*' />	
		public IExpression GetExpression(string name)
		{
			ExpressionResultPair tail = this.GetTailWithValidate(name);
			return tail.Expression;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.GetDependents"]/*' />	
		public string[] GetDependents(string name)
		{
			ExpressionResultPair pair = this.GetTail(name);
			List<ExpressionResultPair> dependents = new List<ExpressionResultPair>();

			if ((pair != null)) {
				MyDependencies.GetDirectDependents(pair, dependents);
			}

			return this.GetNames(dependents);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.GetPrecedents"]/*' />	
		public string[] GetPrecedents(string name)
		{
			ExpressionResultPair pair = this.GetTail(name);
			List<ExpressionResultPair> dependents = new List<ExpressionResultPair>();

			if ((pair != null)) {
				MyDependencies.GetDirectPrecedents(pair, dependents);
			}

			return this.GetNames(dependents);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.HasDependents"]/*' />	
		public bool HasDependents(string name)
		{
			ExpressionResultPair pair = this.GetTail(name);
			return (pair != null) && MyDependencies.HasDependents(pair);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.HasPrecedents"]/*' />	
		public bool HasPrecedents(string name)
		{
			ExpressionResultPair pair = this.GetTail(name);
			return (pair != null) && MyDependencies.HasPrecedents(pair);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.Contains"]/*' />	
		public bool Contains(string name)
		{
			Utility.AssertNotNull(name, "name");
			return MyNameNodeMap.ContainsKey(name);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.Recalculate"]/*' />	
		public void Recalculate(params string[] roots)
		{
			// Get the tails corresponding to the names
			ExpressionResultPair[] rootTails = this.GetRootTails(roots);
			// Create a dependency list based on the tails
			DependencyManager<ExpressionResultPair> tempDependents = MyDependencies.CloneDependents(rootTails);
			// Get the sources (ie: nodes with no incoming edges) since that's what the sort requires
			Queue<ExpressionResultPair> sources = tempDependents.GetSources(rootTails);
			// Do the topological sort
			IList<ExpressionResultPair> calcList = tempDependents.TopologicalSort(sources);

			NodeEventArgs args = new NodeEventArgs();

			// Recalculate the sorted expressions
			foreach (ExpressionResultPair pair in calcList) {
				pair.Recalculate();
				args.SetData(pair.Name, pair.ResultAsObject);
				if (NodeRecalculated != null) {
					NodeRecalculated(this, args);
				}
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.Clear"]/*' />	
		public void Clear()
		{
			MyDependencies.Clear();
			MyNameNodeMap.Clear();
		}

		#endregion

		#region "Properties - Public"

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.Count"]/*' />	
		public int Count {
			get { return MyDependencies.Count; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="CalculationEngine.DependencyGraph"]/*' />	
		public string DependencyGraph {
			get { return MyDependencies.DependencyGraph; }
		}

		#endregion
	}
}
