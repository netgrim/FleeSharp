
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

	// Base class for expression elements that operate on two child elements
	internal abstract class BinaryExpressionElement : ExpressionElement
	{

		protected ExpressionElement MyLeftChild;
		protected ExpressionElement MyRightChild;

		private Type MyResultType;

		protected BinaryExpressionElement()
		{
		}

        // Converts a list of binary elements into a binary tree
		public static BinaryExpressionElement CreateElement(IList childValues, Type elementType)
		{
            var firstElement = (BinaryExpressionElement)Activator.CreateInstance(elementType);
            firstElement.Configure((ExpressionElement)childValues[0], (ExpressionElement)childValues[2], childValues[1]);

			var lastElement = firstElement;

			for (int i = 3; i <= childValues.Count - 1; i += 2) {
                var element = (BinaryExpressionElement)Activator.CreateInstance(elementType);
                element.Configure((ExpressionElement)lastElement, (ExpressionElement)childValues[i + 1], childValues[i]);
				lastElement = element;
			}

			return lastElement;
		}

		protected abstract void GetOperation(object operation);

		protected void ValidateInternal(object op)
		{
			MyResultType = this.GetResultType(MyLeftChild.ResultType, MyRightChild.ResultType);

			if (MyResultType == null) {
				this.ThrowOperandTypeMismatch(op, MyLeftChild.ResultType, MyRightChild.ResultType);
			}
		}

		protected MethodInfo GetOverloadedBinaryOperator(string name, object operation)
		{
			var leftType = MyLeftChild.ResultType;
			var rightType = MyRightChild.ResultType;
			var binder = new BinaryOperatorBinder(leftType, rightType);

			// If both arguments are of the same type, pick either as the owner type
			if (object.ReferenceEquals(leftType, rightType)) {
				return Utility.GetOverloadedOperator(name, leftType, binder, leftType, rightType);
			}

			// Get the operator for both types
			MethodInfo leftMethod = null;
			MethodInfo rightMethod = null;
			leftMethod = Utility.GetOverloadedOperator(name, leftType, binder, leftType, rightType);
			rightMethod = Utility.GetOverloadedOperator(name, rightType, binder, leftType, rightType);

			// Pick the right one
			if (leftMethod == null & rightMethod == null) {
				// No operator defined for either
				return null;
			} else if (leftMethod == null) {
				return rightMethod;
			} else if (rightMethod == null) {
				return leftMethod;
			} else {
				// Ambiguous call
				base.ThrowAmbiguousCallException(leftType, rightType, operation);
				return null;
			}
		}

		protected void EmitOverloadedOperatorCall(MethodInfo method, FleeILGenerator ilg, IServiceProvider services)
		{
			var @params = method.GetParameters();
			var pLeft = @params[0];
			var pRight = @params[1];

			EmitChildWithConvert(MyLeftChild, pLeft.ParameterType, ilg, services);
			EmitChildWithConvert(MyRightChild, pRight.ParameterType, ilg, services);
			ilg.Emit(OpCodes.Call, method);
		}

		protected void ThrowOperandTypeMismatch(object operation, Type leftType, Type rightType)
		{
			base.ThrowCompileException(CompileErrorResourceKeys.OperationNotDefinedForTypes, CompileExceptionReason.TypeMismatch, operation, leftType.Name, rightType.Name);
		}

		protected abstract Type GetResultType(Type leftType, Type rightType);

		protected static void EmitChildWithConvert(ExpressionElement child, Type resultType, FleeILGenerator ilg, IServiceProvider services)
		{
			child.Emit(ilg, services);
			var converted = ImplicitConverter.EmitImplicitConvert(child.ResultType, resultType, ilg);
			Debug.Assert(converted, "convert failed");
		}

		protected bool AreBothChildrenOfType(Type target)
		{
			return IsChildOfType(MyLeftChild, target) & IsChildOfType(MyRightChild, target);
		}

		protected bool IsEitherChildOfType(Type target)
		{
			return IsChildOfType(MyLeftChild, target) || IsChildOfType(MyRightChild, target);
		}

		protected static bool IsChildOfType(ExpressionElement child, Type t)
		{
			return object.ReferenceEquals(child.ResultType, t);
		}

        // Set the left and right operands, get the operation, and get the result type
		private void Configure(ExpressionElement leftChild, ExpressionElement rightChild, object op)
		{
			MyLeftChild = leftChild;
			MyRightChild = rightChild;
			this.GetOperation(op);

			this.ValidateInternal(op);
		}

		public override sealed System.Type ResultType {
			get { return MyResultType; }
		}
	}
}
