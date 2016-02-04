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

    // Keeps track of our dependencies
	internal class DependencyManager<T>
	{

		// Map of a node and the nodes that depend on it
		private Dictionary<T, IDictionary<T, object>> MyDependentsMap;
		private IEqualityComparer<T> MyEqualityComparer;
		// Map of a node and the number of nodes that point to it

		private Dictionary<T, int> MyPrecedentsMap;
		public DependencyManager(IEqualityComparer<T> comparer)
		{
			MyEqualityComparer = comparer;
            MyDependentsMap = new Dictionary<T, IDictionary<T, object>>(MyEqualityComparer);
			MyPrecedentsMap = new Dictionary<T, int>(MyEqualityComparer);
		}

		private IDictionary<T, object> CreateInnerDictionary()
		{
			return new Dictionary<T, object>(MyEqualityComparer);
		}

		private IDictionary<T, object> GetInnerDictionary(T tail)
		{
			IDictionary<T, object> value = null;

			if (MyDependentsMap.TryGetValue(tail, out value) == true) {
				return value;
			} else {
				return null;
			}
		}

		// Create a dependency list with only the dependents of the given tails
		public DependencyManager<T> CloneDependents(T[] tails)
		{
			IDictionary<T, object> seenNodes = this.CreateInnerDictionary();
			DependencyManager<T> copy = new DependencyManager<T>(MyEqualityComparer);

			foreach (T tail in tails) {
				this.CloneDependentsInternal(tail, copy, seenNodes);
			}

			return copy;
		}

		private void CloneDependentsInternal(T tail, DependencyManager<T> target, IDictionary<T, object> seenNodes)
		{
			if (seenNodes.ContainsKey(tail) == true) {
				// We've already added this node so just return
				return;
			} else {
				// Haven't seen this node yet; mark it as visited
				seenNodes.Add(tail, null);
				target.AddTail(tail);
			}

			IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);

			// Do the recursive add
			foreach (T head in innerDict.Keys) {
				target.AddDepedency(tail, head);
				this.CloneDependentsInternal(head, target, seenNodes);
			}
		}

		public T[] GetTails()
		{
			T[] arr = new T[MyDependentsMap.Keys.Count];
			MyDependentsMap.Keys.CopyTo(arr, 0);
			return arr;
		}

		public void Clear()
		{
			MyDependentsMap.Clear();
			MyPrecedentsMap.Clear();
		}

		public void ReplaceDependency(T old, T replaceWith)
		{
			IDictionary<T, object> value = MyDependentsMap[old];

			MyDependentsMap.Remove(old);
			MyDependentsMap.Add(replaceWith, value);

			foreach (var innerDict in MyDependentsMap.Values) {
				if (innerDict.ContainsKey(old) == true) {
					innerDict.Remove(old);
					innerDict.Add(replaceWith, null);
				}
			}
		}

		public void AddTail(T tail)
		{
			if (MyDependentsMap.ContainsKey(tail) == false) {
				MyDependentsMap.Add(tail, this.CreateInnerDictionary());
			}
		}

		public void AddDepedency(T tail, T head)
		{
			IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);

			if (innerDict.ContainsKey(head) == false) {
				innerDict.Add(head, head);
				this.AddPrecedent(head);
			}
		}

		public void RemoveDependency(T tail, T head)
		{
			IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);
			this.RemoveHead(head, innerDict);
		}

		private void RemoveHead(T head, IDictionary<T, object> dict)
		{
			if (dict.Remove(head) == true) {
				this.RemovePrecedent(head);
			}
		}

		public void Remove(T[] tails)
		{
			foreach (Dictionary<T, object> innerDict in MyDependentsMap.Values) {
				foreach (T tail in tails) {
					this.RemoveHead(tail, innerDict);
				}
			}

			foreach (T tail in tails) {
				MyDependentsMap.Remove(tail);
			}
		}

		public void GetDirectDependents(T tail, List<T> dest)
		{
			IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);
			dest.AddRange(innerDict.Keys);
		}

		public T[] GetDependents(T tail)
		{
            IDictionary<T, object> dependents = this.CreateInnerDictionary();
			this.GetDependentsRecursive(tail, dependents);

			T[] arr = new T[dependents.Count];
			dependents.Keys.CopyTo(arr, 0);
			return arr;
		}

        private void GetDependentsRecursive(T tail, IDictionary<T, object> dependents)
		{
			dependents[tail] = null;
            IDictionary<T, object> directDependents = this.GetInnerDictionary(tail);

			foreach (T pair in directDependents.Keys) {
				this.GetDependentsRecursive(pair, dependents);
			}
		}

		public void GetDirectPrecedents(T head, IList<T> dest)
		{
			foreach (T tail in MyDependentsMap.Keys) {
                IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);
				if (innerDict.ContainsKey(head) == true) {
					dest.Add(tail);
				}
			}
		}

		private void AddPrecedent(T head)
		{
			int count = 0;
			MyPrecedentsMap.TryGetValue(head, out count);
			MyPrecedentsMap[head] = count + 1;
		}

		private void RemovePrecedent(T head)
		{
			int count = MyPrecedentsMap[head] - 1;

			if (count == 0) {
				MyPrecedentsMap.Remove(head);
			} else {
				MyPrecedentsMap[head] = count;
			}
		}

		public bool HasPrecedents(T head)
		{
			return MyPrecedentsMap.ContainsKey(head);
		}

		public bool HasDependents(T tail)
		{
            IDictionary<T, object> innerDict = this.GetInnerDictionary(tail);
			return innerDict.Count > 0;
		}

		private string FormatValues(ICollection<T> values)
		{
			string[] strings = new string[values.Count];
			T[] keys = new T[values.Count];
			values.CopyTo(keys, 0);

			for (int i = 0; i <= keys.Length - 1; i++) {
				strings[i] = keys[i].ToString();
			}

			if (strings.Length == 0) {
				return "<empty>";
			} else {
				return string.Join(",", strings);
			}
		}

		// Add all nodes that don't have any incoming edges into a queue
		public Queue<T> GetSources(T[] rootTails)
		{
			Queue<T> q = new Queue<T>();

			foreach (T rootTail in rootTails) {
				if (this.HasPrecedents(rootTail) == false) {
					q.Enqueue(rootTail);
				}
			}

			return q;
		}

		public IList<T> TopologicalSort(Queue<T> sources)
		{
			List<T> output = new List<T>();
			List<T> directDependents = new List<T>();

			while (sources.Count > 0) {
				T n = sources.Dequeue();
				output.Add(n);

				directDependents.Clear();
				this.GetDirectDependents(n, directDependents);

				foreach (T m in directDependents) {
					this.RemoveDependency(n, m);

					if (this.HasPrecedents(m) == false) {
						sources.Enqueue(m);
					}
				}
			}

			if (output.Count != this.Count) {
				throw new CircularReferenceException();
			}

			return output;
		}

		#if DEBUG
		public string Precedents {
			get {
				List<string> list = new List<string>();

				foreach (KeyValuePair<T, int> pair in MyPrecedentsMap) {
					list.Add(pair.ToString());
				}

				return string.Join(System.Environment.NewLine, list.ToArray());
			}
		}
		#endif

		public string DependencyGraph {
			get {
				string[] lines = new string[MyDependentsMap.Count];
				int index = 0;

				foreach (var pair in MyDependentsMap) {
					T key = pair.Key;
					string s = this.FormatValues(pair.Value.Keys);
					lines[index] = string.Format("{0} -> {1}", key, s);
					index += 1;
				}

				return string.Join(System.Environment.NewLine, lines);
			}
		}

		public int Count {
			get { return MyDependentsMap.Count; }
		}
	}

}
