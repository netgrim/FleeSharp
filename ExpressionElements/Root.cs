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

	// The expression element at the top of the expression tree
	internal class RootExpressionElement : ExpressionElement
	{

		private ExpressionElement MyChild;

		private Type MyResultType;
		public RootExpressionElement(ExpressionElement child, Type resultType)
		{
			MyChild = child;
			MyResultType = resultType;
			this.Validate();
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			MyChild.Emit(ilg, services);
			ImplicitConverter.EmitImplicitConvert(MyChild.ResultType, MyResultType, ilg);

            var options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));

			if (options.IsGeneric == false) {
				ImplicitConverter.EmitImplicitConvert(MyResultType, typeof(object), ilg);
			}

			ilg.Emit(OpCodes.Ret);
		}

		private void Validate()
		{
			if (ImplicitConverter.EmitImplicitConvert(MyChild.ResultType, MyResultType, null) == false) {
				base.ThrowCompileException(CompileErrorResourceKeys.CannotConvertTypeToExpressionResult, CompileExceptionReason.TypeMismatch, MyChild.ResultType.Name, MyResultType.Name);
			}
		}

		public override System.Type ResultType {
			get { return typeof(object); }
		}
	}
}
