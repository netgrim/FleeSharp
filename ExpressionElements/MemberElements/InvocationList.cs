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

	// Chain of member accesses
	internal class InvocationListElement : ExpressionElement
	{


		private MemberElement MyTail;
		public InvocationListElement(IList elements, IServiceProvider services)
		{
			this.HandleFirstElement(elements, services);
			LinkElements(elements);
			Resolve(elements, services);
            MyTail = (MemberElement)elements[elements.Count - 1];
		}

        // Arrange elements as a linked list
		private static void LinkElements(IList elements)
		{
			for (int i = 0; i <= elements.Count - 1; i++) {
                MemberElement current = (MemberElement)elements[i];
				MemberElement nextElement = null;
				if (i + 1 < elements.Count) {
                    nextElement = (MemberElement)elements[i + 1];
				}
				current.Link(nextElement);
			}
		}

		private void HandleFirstElement(IList elements, IServiceProvider services)
		{
            ExpressionElement first = (ExpressionElement)elements[0];

			// If the first element is not a member element, then we assume it is an expression and replace it with the correct member element
			if (!(first is MemberElement)) {
				ExpressionMemberElement actualFirst = new ExpressionMemberElement(first);
				elements[0] = actualFirst;
			} else {
				this.ResolveNamespaces(elements, services);
			}
		}

		private void ResolveNamespaces(IList elements, IServiceProvider services)
		{
            var context = (ExpressionContext)services.GetService(typeof(ExpressionContext));
			ImportBase currentImport = context.Imports.RootImport;

			while (true) {
				string name = GetName(elements);

				if (name == null) {
					break; // TODO: might not be correct. Was : Exit While
				}

				ImportBase import = currentImport.FindImport(name);

				if (import == null) {
					break; // TODO: might not be correct. Was : Exit While
				}

				currentImport = import;
				elements.RemoveAt(0);

				if (elements.Count > 0) {
					MemberElement newFirst = (MemberElement)elements[0];
					newFirst.SetImport(currentImport);
				}
			}

			if (elements.Count == 0) {
				base.ThrowCompileException(CompileErrorResourceKeys.NamespaceCannotBeUsedAsType, CompileExceptionReason.TypeMismatch, currentImport.Name);
			}
		}

		private static string GetName(IList elements)
		{
			if (elements.Count == 0) {
				return null;
			}

			// Is the first member a field/property element?
			var fpe = elements[0] as IdentifierElement;

			if (fpe == null) {
				return null;
			} else {
				return fpe.MemberName;
			}
		}

		private static void Resolve(IList elements, IServiceProvider services)
		{
			foreach (MemberElement element in elements) {
				element.Resolve(services);
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			MyTail.Emit(ilg, services);
		}

		public override System.Type ResultType {
			get { return MyTail.ResultType; }
		}
	}
}
