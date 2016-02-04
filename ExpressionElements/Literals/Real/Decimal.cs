
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

	internal class DecimalLiteralElement : RealLiteralElement
	{


		private static ConstructorInfo OurConstructorInfo = GetConstructor();

		private decimal MyValue;

		private DecimalLiteralElement()
		{
		}

		public DecimalLiteralElement(decimal value)
		{
			MyValue = value;
		}

		private static ConstructorInfo GetConstructor()
		{
			Type[] types = {
				typeof(Int32),
				typeof(Int32),
				typeof(Int32),
				typeof(bool),
				typeof(byte)
			};
			return typeof(decimal).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.Any, types, null);
		}

		public static DecimalLiteralElement Parse(string image, IServiceProvider services)
		{
            ExpressionParserOptions options = (ExpressionParserOptions)services.GetService(typeof(ExpressionParserOptions));
			DecimalLiteralElement element = new DecimalLiteralElement();

			try {
				decimal value = options.ParseDecimal(image);
				return new DecimalLiteralElement(value);
			} catch (OverflowException) {
				element.OnParseOverflow(image);
				return null;
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			int index = ilg.GetTempLocalIndex(typeof(decimal));
			Utility.EmitLoadLocalAddress(ilg, index);

			int[] bits = decimal.GetBits(MyValue);
			EmitLoad(bits[0], ilg);
			EmitLoad(bits[1], ilg);
			EmitLoad(bits[2], ilg);

			int flags = bits[3];

			EmitLoad((flags >> 31) == -1, ilg);

			EmitLoad(flags >> 16, ilg);

			ilg.Emit(OpCodes.Call, OurConstructorInfo);

			Utility.EmitLoadLocal(ilg, index);
		}

		public override System.Type ResultType {
			get { return typeof(decimal); }
		}
	}
}
