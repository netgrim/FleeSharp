
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

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

	// Element representing an array index
	internal class IndexerElement : MemberElement
	{

		private ExpressionElement MyIndexerElement;

		private ArgumentList MyIndexerElements;
		public IndexerElement(ArgumentList indexer)
		{
			MyIndexerElements = indexer;
		}

		protected override void ResolveInternal()
		{
			// Are we are indexing on an array?
			Type target = MyPrevious.TargetType;

			// Yes, so setup for an array index
			if (target.IsArray == true) {
				this.SetupArrayIndexer();
				return;
			}

			// Not an array, so try to find an indexer on the type
			if (this.FindIndexer(target) == false) {
				base.ThrowCompileException(CompileErrorResourceKeys.TypeNotArrayAndHasNoIndexerOfType, CompileExceptionReason.TypeMismatch, target.Name, MyIndexerElements);
			}
		}

		private void SetupArrayIndexer()
		{
			MyIndexerElement = MyIndexerElements[0];

			if (MyIndexerElements.Count > 1) {
				base.ThrowCompileException(CompileErrorResourceKeys.MultiArrayIndexNotSupported, CompileExceptionReason.TypeMismatch);
			} else if (ImplicitConverter.EmitImplicitConvert(MyIndexerElement.ResultType, typeof(Int32), null) == false) {
				base.ThrowCompileException(CompileErrorResourceKeys.ArrayIndexersMustBeOfType, CompileExceptionReason.TypeMismatch, typeof(Int32).Name);
			}
		}

		private bool FindIndexer(Type targetType)
		{
			// Get the default members
			MemberInfo[] members = targetType.GetDefaultMembers();

			List<MethodInfo> methods = new List<MethodInfo>();

			// Use the first one that's valid for our indexer type
			foreach (MemberInfo mi in members) {
				PropertyInfo pi = mi as PropertyInfo;
				if ((pi != null)) {
					methods.Add(pi.GetGetMethod(true));
				}
			}

			FunctionCallElement func = new FunctionCallElement("Indexer", methods.ToArray(), MyIndexerElements);
			func.Resolve(MyServices);
			MyIndexerElement = func;

			return true;
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			base.Emit(ilg, services);

			if (this.IsArray == true) {
				this.EmitArrayLoad(ilg, services);
			} else {
				this.EmitIndexer(ilg, services);
			}
		}

		private void EmitArrayLoad(FleeILGenerator ilg, IServiceProvider services)
		{
			MyIndexerElement.Emit(ilg, services);
			ImplicitConverter.EmitImplicitConvert(MyIndexerElement.ResultType, typeof(Int32), ilg);

			Type elementType = this.ResultType;

			if (elementType.IsValueType == false) {
				// Simple reference load
				ilg.Emit(OpCodes.Ldelem_Ref);
			} else {
				this.EmitValueTypeArrayLoad(ilg, elementType);
			}
		}

		private void EmitValueTypeArrayLoad(FleeILGenerator ilg, Type elementType)
		{
			if (this.NextRequiresAddress == true) {
				ilg.Emit(OpCodes.Ldelema, elementType);
			} else {
				Utility.EmitArrayLoad(ilg, elementType);
			}
		}

		private void EmitIndexer(FleeILGenerator ilg, IServiceProvider services)
		{
            FunctionCallElement func = (FunctionCallElement)MyIndexerElement;
			func.EmitFunctionCall(this.NextRequiresAddress, ilg, services);
		}

		private Type ArrayType {
			get {
				if (this.IsArray == true) {
					return MyPrevious.TargetType;
				} else {
					return null;
				}
			}
		}

		private bool IsArray {
			get { return MyPrevious.TargetType.IsArray; }
		}

		protected override bool RequiresAddress {
			get { return this.IsArray == false; }
		}

		public override System.Type ResultType {
			get {
				if (this.IsArray == true) {
					return this.ArrayType.GetElementType();
				} else {
					return MyIndexerElement.ResultType;
				}
			}
		}

		protected override bool IsPublic {
			get {
				if (this.IsArray == true) {
					return true;
				} else {
                    return MemberElement.IsElementPublic((MemberElement)MyIndexerElement);
				}
			}
		}

		public override bool IsStatic {
			get { return false; }
		}
	}
}
