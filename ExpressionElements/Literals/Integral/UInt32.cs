
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

	internal class UInt32LiteralElement : IntegralLiteralElement
	{


		private UInt32 MyValue;
		public UInt32LiteralElement(UInt32 value)
		{
			MyValue = value;
		}

		public static UInt32LiteralElement TryCreate(string image, System.Globalization.NumberStyles ns)
		{
			UInt32 value = default(UInt32);
			if (UInt32.TryParse(image, ns, null, out value) == true) {
				return new UInt32LiteralElement(value);
			} else {
				return null;
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			EmitLoad((int)MyValue, ilg);
		}

		public override System.Type ResultType {
			get { return typeof(UInt32); }
		}
	}
}
