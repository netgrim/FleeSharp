
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

// Elements that represent constants in an expression


namespace Ciloci.Flee
{

	internal class Int64LiteralElement : IntegralLiteralElement
	{

		private Int64 MyValue;
		private const string MinValue = "9223372036854775808";

		private bool MyIsMinValue;
		public Int64LiteralElement(Int64 value)
		{
			MyValue = value;
		}

		private Int64LiteralElement()
		{
			MyIsMinValue = true;
		}

		public static Int64LiteralElement TryCreate(string image, bool isHex, bool negated)
		{
			if (negated == true & image == MinValue) {
				return new Int64LiteralElement();
			} else if (isHex == true) {
				Int64 value = default(Int64);

				if (Int64.TryParse(image, System.Globalization.NumberStyles.AllowHexSpecifier, null, out value) == false) {
					return null;
				} else if (value >= 0 & value <= Int64.MaxValue) {
					return new Int64LiteralElement(value);
				} else {
					return null;
				}
			} else {
				Int64 value = default(Int64);

				if (Int64.TryParse(image, out value) == true) {
					return new Int64LiteralElement(value);
				} else {
					return null;
				}
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			EmitLoad(MyValue, ilg);
		}

		public void Negate()
		{
			if (MyIsMinValue == true) {
				MyValue = Int64.MinValue;
			} else {
				MyValue = -MyValue;
			}
		}

		public override System.Type ResultType {
			get { return typeof(Int64); }
		}
	}
}
