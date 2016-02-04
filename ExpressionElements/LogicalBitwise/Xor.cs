
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

	internal class XorElement : BinaryExpressionElement
	{

		protected override System.Type GetResultType(System.Type leftType, System.Type rightType)
		{
			var bitwiseType = Utility.GetBitwiseOpType(leftType, rightType);

			if ((bitwiseType != null)) {
				return bitwiseType;
			} else if (this.AreBothChildrenOfType(typeof(bool)) == true) {
				return typeof(bool);
			} else {
				return null;
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			var resultType = this.ResultType;

			MyLeftChild.Emit(ilg, services);
			ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, resultType, ilg);
			MyRightChild.Emit(ilg, services);
			ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, resultType, ilg);
			ilg.Emit(OpCodes.Xor);
		}


		protected override void GetOperation(object operation)
		{
		}
	}
}
