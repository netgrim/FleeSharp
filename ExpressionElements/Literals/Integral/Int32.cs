
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

	internal class Int32LiteralElement : IntegralLiteralElement
	{

		private Int32 MyValue;
		private const string MinValue = "2147483648";

		private bool MyIsMinValue;
		public Int32LiteralElement(Int32 value)
		{
			MyValue = value;
		}

		private Int32LiteralElement()
		{
			MyIsMinValue = true;
		}

		public static Int32LiteralElement TryCreate(string image, bool isHex, bool negated)
		{
			if (negated == true & image == MinValue) {
				return new Int32LiteralElement();
			} else if (isHex == true) {
				Int32 value = default(Int32);

				// Since Int32.TryParse will succeed for a string like 0xFFFFFFFF we have to do some special handling
				if (Int32.TryParse(image, System.Globalization.NumberStyles.AllowHexSpecifier, null, out value) == false) {
					return null;
				} else if (value >= 0 & value <= Int32.MaxValue) {
					return new Int32LiteralElement(value);
				} else {
					return null;
				}
			} else {
				Int32 value = default(Int32);

				if (Int32.TryParse(image, out value) == true) {
					return new Int32LiteralElement(value);
				} else {
					return null;
				}
			}
		}

		public void Negate()
		{
			if (MyIsMinValue == true) {
				MyValue = Int32.MinValue;
			} else {
				MyValue = -MyValue;
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			EmitLoad(MyValue, ilg);
		}

		public override System.Type ResultType {
			get { return typeof(Int32); }
		}

		public int Value {
			get { return MyValue; }
		}
	}
}
