using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;

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

	// Element that compares values and generates a boolean result
	internal class CompareElement : BinaryExpressionElement
	{


		private LogicalCompareOperation MyOperation;

		public CompareElement()
		{
		}

		public void Initialize(ExpressionElement leftChild, ExpressionElement rightChild, LogicalCompareOperation op)
		{
			MyLeftChild = leftChild;
			MyRightChild = rightChild;
			MyOperation = op;
		}

		public void Validate()
		{
			this.ValidateInternal(MyOperation);
		}

		protected override void GetOperation(object operation)
		{
			MyOperation = (LogicalCompareOperation)operation;
		}

		protected override System.Type GetResultType(System.Type leftType, System.Type rightType)
		{
			var binaryResultType = ImplicitConverter.GetBinaryResultType(leftType, rightType);
			var overloadedOperator = this.GetOverloadedCompareOperator();
			var isEqualityOp = IsOpTypeEqualOrNotEqual(MyOperation);

			// Use our string equality instead of overloaded operator
			if (object.ReferenceEquals(leftType, typeof(string)) & object.ReferenceEquals(rightType, typeof(string)) & isEqualityOp == true) {
				// String equality
				return typeof(bool);
			} else if ((overloadedOperator != null)) {
				return overloadedOperator.ReturnType;
			} else if ((binaryResultType != null)) {
				// Comparison of numeric operands
				return typeof(bool);
			} else if (object.ReferenceEquals(leftType, typeof(bool)) & object.ReferenceEquals(rightType, typeof(bool)) & isEqualityOp == true) {
				// Boolean equality
				return typeof(bool);
			} else if (this.AreBothChildrenReferenceTypes() == true & isEqualityOp == true) {
				// Comparison of reference types
				return typeof(bool);
			} else if (this.AreBothChildrenSameEnum() == true) {
				return typeof(bool);
			} else {
				// Invalid operands
				return null;
			}
		}

		private MethodInfo GetOverloadedCompareOperator()
		{
			string name = GetCompareOperatorName(MyOperation);
			return base.GetOverloadedBinaryOperator(name, MyOperation);
		}

		private static string GetCompareOperatorName(LogicalCompareOperation op)
		{
			switch (op) {
				case LogicalCompareOperation.Equal:
					return "Equality";
				case LogicalCompareOperation.NotEqual:
					return "Inequality";
				case LogicalCompareOperation.GreaterThan:
					return "GreaterThan";
				case LogicalCompareOperation.LessThan:
					return "LessThan";
				case LogicalCompareOperation.GreaterThanOrEqual:
					return "GreaterThanOrEqual";
				case LogicalCompareOperation.LessThanOrEqual:
					return "LessThanOrEqual";
				default:
					Debug.Assert(false, "unknown compare type");
					return null;
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			var binaryResultType = ImplicitConverter.GetBinaryResultType(MyLeftChild.ResultType, MyRightChild.ResultType);
			var overloadedOperator = this.GetOverloadedCompareOperator();

			if (this.AreBothChildrenOfType(typeof(string))) {
				// String equality
				MyLeftChild.Emit(ilg, services);
				MyRightChild.Emit(ilg, services);
				EmitStringEquality(ilg, MyOperation, services);
			} else if ((overloadedOperator != null)) {
				base.EmitOverloadedOperatorCall(overloadedOperator, ilg, services);
			} else if ((binaryResultType != null)) {
				// Emit a compare of numeric operands
				EmitChildWithConvert(MyLeftChild, binaryResultType, ilg, services);
				EmitChildWithConvert(MyRightChild, binaryResultType, ilg, services);
				EmitCompareOperation(ilg, MyOperation);
			} else if (this.AreBothChildrenOfType(typeof(bool))) {
				// Boolean equality
				this.EmitRegular(ilg, services);
			} else if (this.AreBothChildrenReferenceTypes() == true) {
				// Reference equality
				this.EmitRegular(ilg, services);
			} else if (MyLeftChild.ResultType.IsEnum == true & MyRightChild.ResultType.IsEnum == true) {
				this.EmitRegular(ilg, services);
			} else {
				Debug.Fail("unknown operand types");
			}
		}

		private void EmitRegular(FleeILGenerator ilg, IServiceProvider services)
		{
			MyLeftChild.Emit(ilg, services);
			MyRightChild.Emit(ilg, services);
			this.EmitCompareOperation(ilg, MyOperation);
		}

		private static void EmitStringEquality(FleeILGenerator ilg, LogicalCompareOperation op, IServiceProvider services)
		{
			// Get the StringComparison from the options
            var options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
			var ic = new Int32LiteralElement((int)options.StringComparison);

			ic.Emit(ilg, services);

			// and emit the method call
			var mi = typeof(string).GetMethod("Equals", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new Type[] {
				typeof(string),
				typeof(string),
				typeof(StringComparison)
			}, null);
			ilg.Emit(OpCodes.Call, mi);

			if (op == LogicalCompareOperation.NotEqual) {
				ilg.Emit(OpCodes.Ldc_I4_0);
				ilg.Emit(OpCodes.Ceq);
			}
		}

		private static bool IsOpTypeEqualOrNotEqual(LogicalCompareOperation op)
		{
			return op == LogicalCompareOperation.Equal | op == LogicalCompareOperation.NotEqual;
		}

		private bool AreBothChildrenReferenceTypes()
		{
			return MyLeftChild.ResultType.IsValueType == false & MyRightChild.ResultType.IsValueType == false;
		}

		private bool AreBothChildrenSameEnum()
		{
			return MyLeftChild.ResultType.IsEnum == true && object.ReferenceEquals(MyLeftChild.ResultType, MyRightChild.ResultType);
		}

        // Emit the actual compare
		private void EmitCompareOperation(FleeILGenerator ilg, LogicalCompareOperation op)
		{
			var ltOpcode = this.GetCompareGTLTOpcode(false);
			var gtOpcode = this.GetCompareGTLTOpcode(true);

			switch (op) {
				case LogicalCompareOperation.Equal:
					ilg.Emit(OpCodes.Ceq);
					break;
				case LogicalCompareOperation.LessThan:
					ilg.Emit(ltOpcode);
					break;
				case LogicalCompareOperation.GreaterThan:
					ilg.Emit(gtOpcode);
					break;
				case LogicalCompareOperation.NotEqual:
					ilg.Emit(OpCodes.Ceq);
					ilg.Emit(OpCodes.Ldc_I4_0);
					ilg.Emit(OpCodes.Ceq);
					break;
				case LogicalCompareOperation.LessThanOrEqual:
					ilg.Emit(gtOpcode);
					ilg.Emit(OpCodes.Ldc_I4_0);
					ilg.Emit(OpCodes.Ceq);
					break;
				case LogicalCompareOperation.GreaterThanOrEqual:
					ilg.Emit(ltOpcode);
					ilg.Emit(OpCodes.Ldc_I4_0);
					ilg.Emit(OpCodes.Ceq);
					break;
				default:
					Debug.Fail("Unknown op type");
					break;
			}
		}

        // Get the correct greater/less than opcode
		private OpCode GetCompareGTLTOpcode(bool greaterThan)
		{
			var leftType = MyLeftChild.ResultType;

			if (object.ReferenceEquals(leftType, MyRightChild.ResultType)) {
				if (object.ReferenceEquals(leftType, typeof(UInt32)) | object.ReferenceEquals(leftType, typeof(UInt64))) {
					if (greaterThan == true) {
						return OpCodes.Cgt_Un;
					} else {
						return OpCodes.Clt_Un;
					}
				} else {
					return GetCompareOpcode(greaterThan);
				}
			} else {
				return GetCompareOpcode(greaterThan);
			}
		}

		private static OpCode GetCompareOpcode(bool greaterThan)
		{
			if (greaterThan == true) {
				return OpCodes.Cgt;
			} else {
				return OpCodes.Clt;
			}
		}
	}
}
