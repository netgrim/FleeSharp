
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

namespace Ciloci.Flee
{

	/// <summary>
	/// Encapsulates an argument list
	/// </summary>
	/// <remarks></remarks>
	internal class ArgumentList
	{


		private IList<ExpressionElement> MyElements;
		public ArgumentList(ICollection elements)
		{
			ExpressionElement[] arr = new ExpressionElement[elements.Count];
			elements.CopyTo(arr, 0);
			MyElements = arr;
		}

		private string[] GetArgumentTypeNames()
		{
			List<string> l = new List<string>();

			foreach (ExpressionElement e in MyElements) {
				l.Add(e.ResultType.Name);
			}

			return l.ToArray();
		}

		public Type[] GetArgumentTypes()
		{
			List<Type> l = new List<Type>();

			foreach (ExpressionElement e in MyElements) {
				l.Add(e.ResultType);
			}

			return l.ToArray();
		}

		public override string ToString()
		{
			string[] typeNames = this.GetArgumentTypeNames();
			return Utility.FormatList(typeNames);
		}

		public ExpressionElement[] ToArray()
		{
			ExpressionElement[] arr = new ExpressionElement[MyElements.Count];
			MyElements.CopyTo(arr, 0);
			return arr;
		}

		public ExpressionElement this[int index] {
			get { return MyElements[index]; }
		}

		public int Count {
			get { return MyElements.Count; }
		}
	}
}
