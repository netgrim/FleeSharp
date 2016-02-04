using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
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

// Bitwise and logical And/Or
internal class AndOrElement : BinaryExpressionElement
{

    private AndOrOperation MyOperation;
    private static object OurTrueTerminalKey = new object();
    private static object OurFalseTerminalKey = new object();

    private static object OurEndLabelKey = new object();

    public AndOrElement()
    {
    }

    protected override void GetOperation(object operation)
    {
        MyOperation = (AndOrOperation)operation;
    }

    protected override System.Type GetResultType(System.Type leftType, System.Type rightType)
    {
        Type bitwiseOpType = Utility.GetBitwiseOpType(leftType, rightType);
        if ((bitwiseOpType != null))
        {
            return bitwiseOpType;
        }
        else if (this.AreBothChildrenOfType(typeof(bool)))
        {
            return typeof(bool);
        }
        else
        {
            return null;
        }
    }

    public override void Emit(FleeILGenerator ilg, IServiceProvider services)
    {
        Type resultType = this.ResultType;

        if (object.ReferenceEquals(resultType, typeof(bool)))
        {
            this.DoEmitLogical(ilg, services);
        }
        else
        {
            MyLeftChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, resultType, ilg);
            MyRightChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, resultType, ilg);
            EmitBitwiseOperation(ilg, MyOperation);
        }
    }

    private static void EmitBitwiseOperation(FleeILGenerator ilg, AndOrOperation op)
    {
        switch (op)
        {
            case AndOrOperation.And:
            	ilg.Emit(OpCodes.And);
                break;
            case AndOrOperation.Or:
                ilg.Emit(OpCodes.Or);
                break;
            default:
                Debug.Fail("Unknown op type");
                break;
        }
    }

    private void DoEmitLogical(FleeILGenerator ilg, IServiceProvider services)
    {
        // We have to do a 'fake' emit so we can get the positions of the labels
        ShortCircuitInfo info = new ShortCircuitInfo();
        // Create a temporary IL generator
        FleeILGenerator ilgTemp = this.CreateTempFleeILGenerator(ilg);

        // We have to make sure that the label count for the temp FleeILGenerator matches our real FleeILGenerator
        Utility.SyncFleeILGeneratorLabels(ilg, ilgTemp);
        // Do the fake emit
        this.EmitLogical(ilgTemp, info, services);

        // Clear everything except the label positions
        info.ClearTempState();

        info.Branches.ComputeBranches();

        Utility.SyncFleeILGeneratorLabels(ilgTemp, ilg);

        // Do the real emit
        this.EmitLogical(ilg, info, services);
    }

    // Emit a short-circuited logical operation sequence
    // The idea: Store all the leaf operands in a stack with the leftmost at the top and rightmost at the bottom.
    // For each operand, emit it and try to find an end point for when it short-circuits.  This means we go up through
    // the stack of operators (ignoring siblings) until we find a different operation (then emit a branch to its right operand)
    // or we reach the root (emit a branch to a true/false).
    // Repeat the process for all operands and then emit the true/false/last operand end cases.
    private void EmitLogical(FleeILGenerator ilg, ShortCircuitInfo info, IServiceProvider services)
    {
        // We always have an end label
        info.Branches.GetLabel(OurEndLabelKey, ilg);

        // Populate our data structures
        this.PopulateData(info);

        // Emit the sequence
        EmitLogicalShortCircuit(ilg, info, services);

        // Get the last operand
        ExpressionElement terminalOperand = (ExpressionElement)info.Operands.Pop();
        // Emit it
        EmitOperand(terminalOperand, info, ilg, services);
        // And jump to the end
        Label endLabel = info.Branches.FindLabel(OurEndLabelKey);
        ilg.Emit(OpCodes.Br_S, endLabel);

        // Emit our true/false terminals
        EmitTerminals(info, ilg, endLabel);

        // Mark the end
        ilg.MarkLabel(endLabel);
    }

    // Emit a sequence of and/or expressions with short-circuiting
    private static void EmitLogicalShortCircuit(FleeILGenerator ilg, ShortCircuitInfo info, IServiceProvider services)
    {
        while (info.Operators.Count != 0)
        {
            // Get the operator
            AndOrElement op = (AndOrElement)info.Operators.Pop();
            // Get the left operand
            ExpressionElement leftOperand = (ExpressionElement)info.Operands.Pop();

            // Emit the left
            EmitOperand(leftOperand, info, ilg, services);

            // Get the label for the short-circuit case
            Label l = GetShortCircuitLabel(op, info, ilg);
            // Emit the branch
            EmitBranch(op, ilg, l, info);
        }
    }


    private static void EmitBranch(AndOrElement op, FleeILGenerator ilg, Label target, ShortCircuitInfo info)
    {
        if (ilg.IsTemp == true)
        {
            info.Branches.AddBranch(ilg, target);

            // Temp mode; just emit a short branch and return
            OpCode shortBranch = GetBranchOpcode(op, false);
            ilg.Emit(shortBranch, target);

            return;
        }

        // Emit the proper branch opcode

        // Determine if it is a long branch
        bool longBranch = info.Branches.IsLongBranch(ilg, target);

        // Get the branch opcode
        OpCode brOpcode = GetBranchOpcode(op, longBranch);

        // Emit the branch
        ilg.Emit(brOpcode, target);
    }

    // Emit a short/long branch for an And/Or element
    private static OpCode GetBranchOpcode(AndOrElement op, bool longBranch)
    {
        if (op.MyOperation == AndOrOperation.And)
        {
            if (longBranch == true)
            {
                return OpCodes.Brfalse;
            }
            else
            {
                return OpCodes.Brfalse_S;
            }
        }
        else
        {
            if (longBranch == true)
            {
                return OpCodes.Brtrue;
            }
            else
            {
                return OpCodes.Brtrue_S;
            }
        }
    }

    // Get the label for a short-circuit
    private static Label GetShortCircuitLabel(AndOrElement current, ShortCircuitInfo info, FleeILGenerator ilg)
    {
        // We modify the given stacks so we need to clone them
        Stack cloneOperands = (Stack)info.Operands.Clone();
        Stack cloneOperators = (Stack)info.Operators.Clone();

        // Pop all siblings
        current.PopRightChild(cloneOperands, cloneOperators);

        // Go until we run out of operators
        while (cloneOperators.Count > 0)
        {
            // Get the top operator
            AndOrElement top = (AndOrElement)cloneOperators.Pop();

            // Is is a different operation?
            if (top.MyOperation != current.MyOperation)
            {
                // Yes, so return a label to its right operand
                object nextOperand = cloneOperands.Pop();
                return GetLabel(nextOperand, ilg, info);
            }
            else
            {
                // No, so keep going up the stack
                top.PopRightChild(cloneOperands, cloneOperators);
            }
        }

        // We've reached the end of the stack so return the label for the appropriate true/false terminal
        if (current.MyOperation == AndOrOperation.And)
        {
            return GetLabel(OurFalseTerminalKey, ilg, info);
        }
        else
        {
            return GetLabel(OurTrueTerminalKey, ilg, info);
        }
    }

    private void PopRightChild(Stack operands, Stack operators)
    {
        AndOrElement andOrChild = MyRightChild as AndOrElement;

        // What kind of child do we have?
        if ((andOrChild != null))
        {
            // Another and/or expression so recurse
            andOrChild.Pop(operands, operators);
        }
        else
        {
            // A terminal so pop it off the operands stack
            operands.Pop();
        }
    }

    // Recursively pop operators and operands
    private void Pop(Stack operands, Stack operators)
    {
        operators.Pop();

        AndOrElement andOrChild = MyLeftChild as AndOrElement;
        if (andOrChild == null)
        {
            operands.Pop();
        }
        else
        {
            andOrChild.Pop(operands, operators);
        }

        andOrChild = MyRightChild as AndOrElement;

        if (andOrChild == null)
        {
            operands.Pop();
        }
        else
        {
            andOrChild.Pop(operands, operators);
        }
    }

    private static void EmitOperand(ExpressionElement operand, ShortCircuitInfo info, FleeILGenerator ilg, IServiceProvider services)
    {
        // Is this operand the target of a label?
        if (info.Branches.HasLabel(operand) == true)
        {
            // Yes, so mark it
            Label leftLabel = info.Branches.FindLabel(operand);
            ilg.MarkLabel(leftLabel);

            // Note the label's position
            MarkBranchTarget(info, leftLabel, ilg);
        }

        // Emit the operand
        operand.Emit(ilg, services);
    }

    // Emit the end cases for a short-circuit
    private static void EmitTerminals(ShortCircuitInfo info, FleeILGenerator ilg, Label endLabel)
    {
        // Emit the false case if it was used
        if (info.Branches.HasLabel(OurFalseTerminalKey) == true)
        {
            Label falseLabel = info.Branches.FindLabel(OurFalseTerminalKey);

            // Mark the label and note its position
            ilg.MarkLabel(falseLabel);
            MarkBranchTarget(info, falseLabel, ilg);

            ilg.Emit(OpCodes.Ldc_I4_0);

            // If we also have a true terminal, then skip over it
            if (info.Branches.HasLabel(OurTrueTerminalKey) == true)
            {
                ilg.Emit(OpCodes.Br_S, endLabel);
            }
        }

        // Emit the true case if it was used
        if (info.Branches.HasLabel(OurTrueTerminalKey) == true)
        {
            Label trueLabel = info.Branches.FindLabel(OurTrueTerminalKey);

            // Mark the label and note its position
            ilg.MarkLabel(trueLabel);
            MarkBranchTarget(info, trueLabel, ilg);

            ilg.Emit(OpCodes.Ldc_I4_1);
        }
    }

    // Note a label's position if we are in mark mode
    private static void MarkBranchTarget(ShortCircuitInfo info, Label target, FleeILGenerator ilg)
    {
        if (ilg.IsTemp == true)
        {
            info.Branches.MarkLabel(ilg, target);
        }
    }

    private static Label GetLabel(object key, FleeILGenerator ilg, ShortCircuitInfo info)
    {
        return info.Branches.GetLabel(key, ilg);
    }

    // Visit the nodes of the tree (right then left) and populate some data structures
    private void PopulateData(ShortCircuitInfo info)
    {
        // Is our right child a leaf or another And/Or expression?
        AndOrElement andOrChild = MyRightChild as AndOrElement;
        if (andOrChild == null)
        {
            // Leaf so push it on the stack
            info.Operands.Push(MyRightChild);
        }
        else
        {
            // Another And/Or expression so recurse
            andOrChild.PopulateData(info);
        }

        // Add ourselves as an operator
        info.Operators.Push(this);

        // Do the same thing for the left child
        andOrChild = MyLeftChild as AndOrElement;

        if (andOrChild == null)
        {
            info.Operands.Push(MyLeftChild);
        }
        else
        {
            andOrChild.PopulateData(info);
        }
    }
}
