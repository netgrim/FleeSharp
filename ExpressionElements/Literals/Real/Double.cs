
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

	internal class DoubleLiteralElement : RealLiteralElement
	{


		private double MyValue;

		private DoubleLiteralElement()
		{
		}

		public DoubleLiteralElement(double value)
		{
			MyValue = value;
		}

		public static DoubleLiteralElement Parse(string image, IServiceProvider services)
		{
            ExpressionParserOptions options = (ExpressionParserOptions)services.GetService(typeof(ExpressionParserOptions));
			DoubleLiteralElement element = new DoubleLiteralElement();

			try {
				double value = options.ParseDouble(image);
				return new DoubleLiteralElement(value);
			} catch (OverflowException) {
				element.OnParseOverflow(image);
				return null;
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			ilg.Emit(OpCodes.Ldc_R8, MyValue);
		}

		public override System.Type ResultType {
			get { return typeof(double); }
		}
	}
}
