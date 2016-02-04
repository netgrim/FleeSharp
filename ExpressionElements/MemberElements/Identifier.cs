using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;
using Ciloci.Flee.CalcEngine;

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

	// Represents an identifier
	internal class IdentifierElement : MemberElement
	{

		private FieldInfo MyField;
		private PropertyInfo MyProperty;
		private PropertyDescriptor MyPropertyDescriptor;
		private Type MyVariableType;

		private Type MyCalcEngineReferenceType;
		public IdentifierElement(string name)
		{
			this.MyName = name;
		}

		protected override void ResolveInternal()
		{
			// Try to bind to a field or property
			if (this.ResolveFieldProperty(MyPrevious) == true) {
				this.AddReferencedVariable(MyPrevious);
				return;
			}

			// Try to find a variable with our name
			MyVariableType = MyContext.Variables.GetVariableTypeInternal(MyName);

			// Variables are only usable as the first element
			if (MyPrevious == null && (MyVariableType != null)) {
				this.AddReferencedVariable(MyPrevious);
				return;
			}

			CalculationEngine ce = MyContext.CalculationEngine;

			if ((ce != null)) {
				ce.AddDependency(MyName, MyContext);
				MyCalcEngineReferenceType = ce.ResolveTailType(MyName);
				return;
			}

			if (MyPrevious == null) {
				base.ThrowCompileException(CompileErrorResourceKeys.NoIdentifierWithName, CompileExceptionReason.UndefinedName, MyName);
			} else {
				base.ThrowCompileException(CompileErrorResourceKeys.NoIdentifierWithNameOnType, CompileExceptionReason.UndefinedName, MyName, MyPrevious.TargetType.Name);
			}
		}

		private bool ResolveFieldProperty(MemberElement previous)
		{
			var members = this.GetMembers(MemberTypes.Field | MemberTypes.Property);

			// Keep only the ones which are accessible
			members = this.GetAccessibleMembers(members);

			if (members.Length == 0) {
				// No accessible members; try to resolve a virtual property
				return this.ResolveVirtualProperty(previous);
			} else if (members.Length > 1) {
				// More than one accessible member
				if (previous == null) {
					base.ThrowCompileException(CompileErrorResourceKeys.IdentifierIsAmbiguous, CompileExceptionReason.AmbiguousMatch, MyName);
				} else {
					base.ThrowCompileException(CompileErrorResourceKeys.IdentifierIsAmbiguousOnType, CompileExceptionReason.AmbiguousMatch, MyName, previous.TargetType.Name);
				}
			} else {
				// Only one member; bind to it
				MyField = members[0] as FieldInfo;
				if ((MyField != null)) {
					return true;
				}

				// Assume it must be a property
				MyProperty = (PropertyInfo)members[0];
				return true;
			}

            return false;
		}

		private bool ResolveVirtualProperty(MemberElement previous)
		{
			if (previous == null) {
				// We can't use virtual properties if we are the first element
				return false;
			}

			var coll = TypeDescriptor.GetProperties(previous.ResultType);
			MyPropertyDescriptor = coll.Find(MyName, true);
			return (MyPropertyDescriptor != null);
		}

		private void AddReferencedVariable(MemberElement previous)
		{
			if ((previous != null)) {
				return;
			}

			if ((MyVariableType != null) || MyOptions.IsOwnerType(this.MemberOwnerType) == true) {
                ExpressionInfo info = (ExpressionInfo)MyServices.GetService(typeof(ExpressionInfo));
				info.AddReferencedVariable(MyName);
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			base.Emit(ilg, services);

			this.EmitFirst(ilg);

			if ((MyCalcEngineReferenceType != null)) {
				this.EmitReferenceLoad(ilg);
			} else if ((MyVariableType != null)) {
				this.EmitVariableLoad(ilg);
			} else if ((MyField != null)) {
				this.EmitFieldLoad(MyField, ilg, services);
			} else if ((MyPropertyDescriptor != null)) {
				this.EmitVirtualPropertyLoad(ilg);
			} else {
				this.EmitPropertyLoad(MyProperty, ilg);
			}
		}

		private void EmitReferenceLoad(FleeILGenerator ilg)
		{
			ilg.Emit(OpCodes.Ldarg_1);
			MyContext.CalculationEngine.EmitLoad(MyName, ilg);
		}

		private void EmitFirst(FleeILGenerator ilg)
		{
			if ((MyPrevious != null)) {
				return;
			}

			bool isVariable = (MyVariableType != null);

			if (isVariable == true) {
				// Load variables
				EmitLoadVariables(ilg);
			} else if (MyOptions.IsOwnerType(this.MemberOwnerType) == true & this.IsStatic == false) {
				this.EmitLoadOwner(ilg);
			}
		}

		private void EmitVariableLoad(FleeILGenerator ilg)
		{
			var mi = VariableCollection.GetVariableLoadMethod(MyVariableType);
			ilg.Emit(OpCodes.Ldstr, MyName);
			this.EmitMethodCall(mi, ilg);
		}

		private void EmitFieldLoad(System.Reflection.FieldInfo fi, FleeILGenerator ilg, IServiceProvider services)
		{
			if (fi.IsLiteral == true) {
				EmitLiteral(fi, ilg, services);
			} else if (this.ResultType.IsValueType == true & this.NextRequiresAddress == true) {
				EmitLdfld(fi, true, ilg);
			} else {
				EmitLdfld(fi, false, ilg);
			}
		}

		private static void EmitLdfld(System.Reflection.FieldInfo fi, bool indirect, FleeILGenerator ilg)
		{
			if (fi.IsStatic == true) {
				if (indirect == true) {
					ilg.Emit(OpCodes.Ldsflda, fi);
				} else {
					ilg.Emit(OpCodes.Ldsfld, fi);
				}
			} else {
				if (indirect == true) {
					ilg.Emit(OpCodes.Ldflda, fi);
				} else {
					ilg.Emit(OpCodes.Ldfld, fi);
				}
			}
		}

        // Emit the load of a constant field.  We can't emit a ldsfld/ldfld of a constant so we have to get its value
        // and then emit a ldc.
		private static void EmitLiteral(System.Reflection.FieldInfo fi, FleeILGenerator ilg, IServiceProvider services)
		{
			object value = fi.GetValue(null);
			Type t = value.GetType();
			TypeCode code = Type.GetTypeCode(t);
			LiteralElement elem = null;

			switch (code) {
				case TypeCode.Char:
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
					elem = new Int32LiteralElement(System.Convert.ToInt32(value));
					break;
				case TypeCode.UInt32:
					elem = new UInt32LiteralElement((UInt32)value);
					break;
				case TypeCode.Int64:
					elem = new Int64LiteralElement((Int64)value);
					break;
				case TypeCode.UInt64:
					elem = new UInt64LiteralElement((UInt64)value);
					break;
				case TypeCode.Double:
					elem = new DoubleLiteralElement((double)value);
					break;
				case TypeCode.Single:
					elem = new SingleLiteralElement((float)value);
					break;
				case TypeCode.Boolean:
					elem = new BooleanLiteralElement((bool)value);
					break;
				case TypeCode.String:
					elem = new StringLiteralElement((string)value);
					break;
				default:
					elem = null;
					Debug.Fail("Unsupported constant type");
					break;
			}

			elem.Emit(ilg, services);
		}

		private void EmitPropertyLoad(System.Reflection.PropertyInfo pi, FleeILGenerator ilg)
		{
			System.Reflection.MethodInfo getter = pi.GetGetMethod(true);
			base.EmitMethodCall(getter, ilg);
		}

        // Load a PropertyDescriptor based property
		private void EmitVirtualPropertyLoad(FleeILGenerator ilg)
		{
			// The previous value is already on the top of the stack but we need it at the bottom

			// Get a temporary local index
			int index = ilg.GetTempLocalIndex(MyPrevious.ResultType);

			// Store the previous value there
			Utility.EmitStoreLocal(ilg, index);

			// Load the variable collection
			EmitLoadVariables(ilg);
			// Load the property name
			ilg.Emit(OpCodes.Ldstr, MyName);

			// Load the previous value and convert it to object
			Utility.EmitLoadLocal(ilg, index);
			ImplicitConverter.EmitImplicitConvert(MyPrevious.ResultType, typeof(object), ilg);

			// Call the method to get the actual value
			MethodInfo mi = VariableCollection.GetVirtualPropertyLoadMethod(this.ResultType);
			this.EmitMethodCall(mi, ilg);
		}

		private Type MemberOwnerType {
			get {
				if ((MyField != null)) {
					return MyField.ReflectedType;
				} else if ((MyPropertyDescriptor != null)) {
					return MyPropertyDescriptor.ComponentType;
				} else if ((MyProperty != null)) {
					return MyProperty.ReflectedType;
				} else {
					return null;
				}
			}
		}

		public override System.Type ResultType {
			get {
				if ((MyCalcEngineReferenceType != null)) {
					return MyCalcEngineReferenceType;
				} else if ((MyVariableType != null)) {
					return MyVariableType;
				} else if ((MyPropertyDescriptor != null)) {
					return MyPropertyDescriptor.PropertyType;
				} else if ((MyField != null)) {
					return MyField.FieldType;
				} else {
					MethodInfo mi = MyProperty.GetGetMethod(true);
					return mi.ReturnType;
				}
			}
		}

		protected override bool RequiresAddress {
			get { return MyPropertyDescriptor == null; }
		}

		protected override bool IsPublic {
			get {
				if ((MyVariableType != null) | (MyCalcEngineReferenceType != null)) {
					return true;
				} else if ((MyVariableType != null)) {
					return true;
				} else if ((MyPropertyDescriptor != null)) {
					return true;
				} else if ((MyField != null)) {
					return MyField.IsPublic;
				} else {
					MethodInfo mi = MyProperty.GetGetMethod(true);
					return mi.IsPublic;
				}
			}
		}

		protected override bool SupportsStatic {
			get {
				if ((MyVariableType != null)) {
					// Variables never support static
					return false;
				} else if ((MyPropertyDescriptor != null)) {
					// Neither do virtual properties
					return false;
				} else if (MyOptions.IsOwnerType(this.MemberOwnerType) == true && MyPrevious == null) {
					// Owner members support static if we are the first element
					return true;
				} else {
					// Support static if we are the first (ie: we are a static import)
					return MyPrevious == null;
				}
			}
		}

		protected override bool SupportsInstance {
			get {
				if ((MyVariableType != null)) {
					// Variables always support instance
					return true;
				} else if ((MyPropertyDescriptor != null)) {
					// So do virtual properties
					return true;
				} else if (MyOptions.IsOwnerType(this.MemberOwnerType) == true && MyPrevious == null) {
					// Owner members support instance if we are the first element
					return true;
				} else {
					// We always support instance if we are not the first element
					return (MyPrevious != null);
				}
			}
		}

		public override bool IsStatic {
			get {
				if ((MyVariableType != null) | (MyCalcEngineReferenceType != null)) {
					return false;
				} else if ((MyVariableType != null)) {
					return false;
				} else if ((MyField != null)) {
					return MyField.IsStatic;
				} else if ((MyPropertyDescriptor != null)) {
					return false;
				} else {
					MethodInfo mi = MyProperty.GetGetMethod(true);
					return mi.IsStatic;
				}
			}
		}
	}
}
