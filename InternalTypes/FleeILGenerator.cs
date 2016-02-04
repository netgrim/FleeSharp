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

	/// <summary>
	/// Wraps a regular IL generator and provides additional functionality we need
	/// </summary>
	/// <remarks></remarks>
	internal class FleeILGenerator
	{

		private ILGenerator MyILGenerator;
		private int MyLength;
		private int MyLabelCount;
		private Dictionary<Type, LocalBuilder> MyTempLocals;

		private bool MyIsTemp;
		public FleeILGenerator(ILGenerator ilg, int startLength = 0, bool isTemp = false)
		{
			MyILGenerator = ilg;
			MyTempLocals = new Dictionary<Type, LocalBuilder>();
			MyIsTemp = isTemp;
			MyLength = startLength;
		}

		public int GetTempLocalIndex(Type localType)
		{
			LocalBuilder local = null;

			if (MyTempLocals.TryGetValue(localType, out local) == false) {
				local = MyILGenerator.DeclareLocal(localType);
				MyTempLocals.Add(localType, local);
			}

			return local.LocalIndex;
		}

		public void Emit(OpCode op)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op);
		}

		public void Emit(OpCode op, Type arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, ConstructorInfo arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, MethodInfo arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, FieldInfo arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, byte arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, sbyte arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, short arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, int arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, long arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, float arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, double arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, string arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void Emit(OpCode op, Label arg)
		{
			this.RecordOpcode(op);
			MyILGenerator.Emit(op, arg);
		}

		public void MarkLabel(Label lbl)
		{
			MyILGenerator.MarkLabel(lbl);
		}

		public Label DefineLabel()
		{
			MyLabelCount += 1;
			return MyILGenerator.DefineLabel();
		}

		public LocalBuilder DeclareLocal(Type localType)
		{
			return MyILGenerator.DeclareLocal(localType);
		}

		private void RecordOpcode(OpCode op)
		{
			//Trace.WriteLine(String.Format("{0:x}: {1}", MyLength, op.Name))
			int operandLength = GetOpcodeOperandSize(op.OperandType);
			MyLength += op.Size + operandLength;
		}

		private static int GetOpcodeOperandSize(OperandType operand)
		{
			switch (operand) {
				case OperandType.InlineNone:
					return 0;
				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineVar:
					return 1;
				case OperandType.InlineVar:
					return 2;
				case OperandType.InlineBrTarget:
				case OperandType.InlineField:
				case OperandType.InlineI:
				case OperandType.InlineMethod:
				case OperandType.InlineSig:
				case OperandType.InlineString:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.ShortInlineR:
					return 4;
				case OperandType.InlineI8:
				case OperandType.InlineR:
					return 8;
				default:
					Debug.Fail("Unknown operand type");
					break;
			}
            return 0;
		}

		[Conditional("DEBUG")]
		public void ValidateLength()
		{
			Debug.Assert(this.Length == this.ILGeneratorLength, "ILGenerator length mismatch");
		}

		public int Length {
			get { return MyLength; }
		}

		public int LabelCount {
			get { return MyLabelCount; }
		}

		private int ILGeneratorLength {
			get { return Utility.GetILGeneratorLength(MyILGenerator); }
		}

		public bool IsTemp {
			get { return MyIsTemp; }
		}
	}
}
