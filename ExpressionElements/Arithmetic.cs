using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using Ciloci.Flee;

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

// Elements for arithmetic operations

// Element that represents all arithmetic operations
internal class ArithmeticElement : BinaryExpressionElement
{

    private static MethodInfo OurPowerMethodInfo;
    private static MethodInfo OurStringConcatMethodInfo;
    private static MethodInfo OurObjectConcatMethodInfo;

    private BinaryArithmeticOperation MyOperation;
    static ArithmeticElement()
    {
        OurPowerMethodInfo = typeof(Math).GetMethod("Pow", BindingFlags.Public | BindingFlags.Static);
        OurStringConcatMethodInfo = typeof(string).GetMethod("Concat", BindingFlags.Public | BindingFlags.Static, null, new Type[] {
			typeof(string),
			typeof(string)
		}, null);
        OurObjectConcatMethodInfo = typeof(string).GetMethod("Concat", BindingFlags.Public | BindingFlags.Static, null, new Type[] {
			typeof(object),
			typeof(object)
		}, null);
    }


    public ArithmeticElement()
    {
    }

    protected override void GetOperation(object operation)
    {
        MyOperation = (BinaryArithmeticOperation)operation;
    }

    protected override System.Type GetResultType(System.Type leftType, System.Type rightType)
    {
        var binaryResultType = ImplicitConverter.GetBinaryResultType(leftType, rightType);
        var overloadedMethod = this.GetOverloadedArithmeticOperator();

        // Is an overloaded operator defined for our left and right children?
        if ((overloadedMethod != null))
        {
            // Yes, so use its return type
            return overloadedMethod.ReturnType;
        }
        else if ((binaryResultType != null))
        {
            // Operands are primitive types.  Return computed result type unless we are doing a power operation
            if (MyOperation == BinaryArithmeticOperation.Power)
            {
                return this.GetPowerResultType(leftType, rightType, binaryResultType);
            }
            else
            {
                return binaryResultType;
            }
        }
        else if (this.IsEitherChildOfType(typeof(string)) == true & (MyOperation == BinaryArithmeticOperation.Add))
        {
            // String concatenation
            return typeof(string);
        }
        else
        {
            // Invalid types
            return null;
        }
    }

    private Type GetPowerResultType(Type leftType, Type rightType, Type binaryResultType)
    {
        if (this.IsOptimizablePower == true)
        {
            return leftType;
        }
        else
        {
            return typeof(double);
        }
    }

    private MethodInfo GetOverloadedArithmeticOperator()
    {
        // Get the name of the operator
        string name = GetOverloadedOperatorFunctionName(MyOperation);
        return base.GetOverloadedBinaryOperator(name, MyOperation);
    }

    private static string GetOverloadedOperatorFunctionName(BinaryArithmeticOperation op)
    {
        switch (op)
        {
            case BinaryArithmeticOperation.Add:
                return "Addition";
            case BinaryArithmeticOperation.Subtract:
                return "Subtraction";
            case BinaryArithmeticOperation.Multiply:
                return "Multiply";
            case BinaryArithmeticOperation.Divide:
                return "Division";
            case BinaryArithmeticOperation.Mod:
                return "Modulus";
            case BinaryArithmeticOperation.Power:
                return "Exponent";
            default:
                Debug.Assert(false, "unknown operator type");
                return null;
        }
    }

    public override void Emit(FleeILGenerator ilg, IServiceProvider services)
    {
        var overloadedMethod = this.GetOverloadedArithmeticOperator();

        if ((overloadedMethod != null))
        {
            // Emit a call to an overloaded operator
            this.EmitOverloadedOperatorCall(overloadedMethod, ilg, services);
        }
        else if (this.IsEitherChildOfType(typeof(string)) == true)
        {
            // One of our operands is a string so emit a concatenation
            this.EmitStringConcat(ilg, services);
        }
        else
        {
            // Emit a regular arithmetic operation			
            EmitArithmeticOperation(MyOperation, ilg, services);
        }
    }

    private static bool IsUnsignedForArithmetic(Type t)
    {
        return object.ReferenceEquals(t, typeof(UInt32)) | object.ReferenceEquals(t, typeof(UInt64));
    }

    // Emit an arithmetic operation with handling for unsigned and checked contexts
    private void EmitArithmeticOperation(BinaryArithmeticOperation op, FleeILGenerator ilg, IServiceProvider services)
    {
        var options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
        var unsigned = IsUnsignedForArithmetic(MyLeftChild.ResultType) & IsUnsignedForArithmetic(MyRightChild.ResultType);
        var integral = Utility.IsIntegralType(MyLeftChild.ResultType) & Utility.IsIntegralType(MyRightChild.ResultType);
        var emitOverflow = integral & options.Checked;

        EmitChildWithConvert(MyLeftChild, this.ResultType, ilg, services);

        if (this.IsOptimizablePower == false)
        {
            EmitChildWithConvert(MyRightChild, this.ResultType, ilg, services);
        }

        switch (op)
        {
            case BinaryArithmeticOperation.Add:
                if (emitOverflow == true)
                {
                    if (unsigned == true)
                    {
                        ilg.Emit(OpCodes.Add_Ovf_Un);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Add_Ovf);
                    }
                }
                else
                {
                    ilg.Emit(OpCodes.Add);
                }
                break;
            case BinaryArithmeticOperation.Subtract:
                if (emitOverflow == true)
                {
                    if (unsigned == true)
                    {
                        ilg.Emit(OpCodes.Sub_Ovf_Un);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub_Ovf);
                    }
                }
                else
                {
                    ilg.Emit(OpCodes.Sub);
                }
                break;
            case BinaryArithmeticOperation.Multiply:
                this.EmitMultiply(ilg, emitOverflow, unsigned);
                break;
            case BinaryArithmeticOperation.Divide:
                if (unsigned == true)
                {
                    ilg.Emit(OpCodes.Div_Un);
                }
                else
                {
                    ilg.Emit(OpCodes.Div);
                }
                break;
            case BinaryArithmeticOperation.Mod:
            	if (unsigned == true )
                {
            		ilg.Emit(OpCodes.Rem_Un);
                }
            	else
                {
                    ilg.Emit(OpCodes.Rem);
                }
                break;
            case BinaryArithmeticOperation.Power:
                this.EmitPower(ilg, emitOverflow, unsigned);
                break;
            default:
                Debug.Fail("Unknown op type");
                break;
        }
    }

    private void EmitPower(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
    {
        if (this.IsOptimizablePower == true)
        {
            this.EmitOptimizedPower(ilg, emitOverflow, unsigned);
        }
        else
        {
            ilg.Emit(OpCodes.Call, OurPowerMethodInfo);
        }
    }

    private void EmitOptimizedPower(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
    {
        var right = MyRightChild as Int32LiteralElement;

        if (right.Value == 0)
        {
            ilg.Emit(OpCodes.Pop);
            IntegralLiteralElement.EmitLoad(1, ilg);
            ImplicitConverter.EmitImplicitNumericConvert(typeof(Int32), MyLeftChild.ResultType, ilg);
            return;
        }

        if (right.Value == 1)
        {
            return;
        }

        // Start at 1 since left operand has already been emited once
        for (int i = 1; i <= right.Value - 1; i++)
        {
            ilg.Emit(OpCodes.Dup);
        }

        for (int i = 1; i <= right.Value - 1; i++)
        {
            this.EmitMultiply(ilg, emitOverflow, unsigned);
        }
    }

    private void EmitMultiply(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
    {
        if (emitOverflow)
        {
            if (unsigned)
                ilg.Emit(OpCodes.Mul_Ovf_Un);
            else
                ilg.Emit(OpCodes.Mul_Ovf);
        }
        else
        {
            ilg.Emit(OpCodes.Mul);
        }
    }

    // Emit a string concatenation
    private void EmitStringConcat(FleeILGenerator ilg, IServiceProvider services)
    {
        Type argType = null;
        MethodInfo concatMethodInfo = null;

        // Pick the most specific concat method
        if (this.AreBothChildrenOfType(typeof(string)))
        {
            concatMethodInfo = OurStringConcatMethodInfo;
            argType = typeof(string);
        }
        else
        {
            Debug.Assert(this.IsEitherChildOfType(typeof(string)), "one child must be a string");
            concatMethodInfo = OurObjectConcatMethodInfo;
            argType = typeof(object);
        }

        // Emit the operands and call the function
        MyLeftChild.Emit(ilg, services);
        ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, argType, ilg);
        MyRightChild.Emit(ilg, services);
        ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, argType, ilg);
        ilg.Emit(OpCodes.Call, concatMethodInfo);
    }

    private bool IsOptimizablePower
    {
        get
        {
            if (MyOperation != BinaryArithmeticOperation.Power)
            {
                return false;
            }

            var right = MyRightChild as Int32LiteralElement;

            if (right == null)
            {
                return false;
            }

            return right.Value >= 0;
        }
    }
}
