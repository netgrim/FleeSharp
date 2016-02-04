
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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

using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
namespace Ciloci.Flee
{

	internal class DateTimeLiteralElement : LiteralElement
	{


		private DateTime MyValue;
		public DateTimeLiteralElement(string image, ExpressionContext context)
		{
			ExpressionParserOptions options = context.ParserOptions;

			if (DateTime.TryParseExact(image, options.DateTimeFormat, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out MyValue) == false) {
				base.ThrowCompileException(CompileErrorResourceKeys.CannotParseType, CompileExceptionReason.InvalidFormat, typeof(DateTime).Name);
			}
		}

		public override void Emit(FleeILGenerator ilg, System.IServiceProvider services)
		{
			int index = ilg.GetTempLocalIndex(typeof(DateTime));

			Utility.EmitLoadLocalAddress(ilg, index);

			LiteralElement.EmitLoad(MyValue.Ticks, ilg);

			ConstructorInfo ci = typeof(DateTime).GetConstructor(new Type[] { typeof(long) });

			ilg.Emit(OpCodes.Call, ci);

			Utility.EmitLoadLocal(ilg, index);
		}

		public override System.Type ResultType {
			get { return typeof(DateTime); }
		}
	}
}
