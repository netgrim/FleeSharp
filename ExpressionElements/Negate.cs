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

	// Unary negate
	internal class NegateElement : UnaryElement
	{


		public NegateElement()
		{
		}

		protected override System.Type GetResultType(System.Type childType)
		{
			TypeCode tc = Type.GetTypeCode(childType);

			MethodInfo mi = Utility.GetSimpleOverloadedOperator("UnaryNegation", childType, childType);
			if ((mi != null)) {
				return mi.ReturnType;
			}

			switch (tc) {
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Int32:
				case TypeCode.Int64:
					return childType;
				case TypeCode.UInt32:
					return typeof(Int64);
				default:
					return null;
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			Type resultType = this.ResultType;
			MyChild.Emit(ilg, services);
			ImplicitConverter.EmitImplicitConvert(MyChild.ResultType, resultType, ilg);

			MethodInfo mi = Utility.GetSimpleOverloadedOperator("UnaryNegation", resultType, resultType);

			if (mi == null) {
				ilg.Emit(OpCodes.Neg);
			} else {
				ilg.Emit(OpCodes.Call, mi);
			}
		}
	}
}
