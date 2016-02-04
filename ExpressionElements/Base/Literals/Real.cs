
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
namespace Ciloci.Flee
{

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

	internal abstract class RealLiteralElement : LiteralElement
	{


		protected RealLiteralElement()
		{
		}

		public static LiteralElement CreateFromInteger(string image, IServiceProvider services)
		{
			LiteralElement element = null;

			element = CreateSingle(image, services);

			if ((element != null)) {
				return element;
			}

			element = CreateDecimal(image, services);

			if ((element != null)) {
				return element;
			}

            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));

			// Convert to a double if option is set
			if (options.IntegersAsDoubles == true) {
				return DoubleLiteralElement.Parse(image, services);
			}

			return null;
		}

		public static LiteralElement Create(string image, IServiceProvider services)
		{
			LiteralElement element = null;

			element = CreateSingle(image, services);

			if ((element != null)) {
				return element;
			}

			element = CreateDecimal(image, services);

			if ((element != null)) {
				return element;
			}

			element = CreateDouble(image, services);

			if ((element != null)) {
				return element;
			}

			element = CreateImplicitReal(image, services);

			return element;
		}

		private static LiteralElement CreateImplicitReal(string image, IServiceProvider services)
		{
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
			RealLiteralDataType realType = options.RealLiteralDataType;

			switch (realType) {
				case RealLiteralDataType.Double:
					return DoubleLiteralElement.Parse(image, services);
				case RealLiteralDataType.Single:
					return SingleLiteralElement.Parse(image, services);
				case RealLiteralDataType.Decimal:
					return DecimalLiteralElement.Parse(image, services);
				default:
					Debug.Fail("Unknown value");
					return null;
			}
		}

		private static DoubleLiteralElement CreateDouble(string image, IServiceProvider services)
		{
			if (image.EndsWith("d", StringComparison.OrdinalIgnoreCase) == true) {
				image = image.Remove(image.Length - 1);
				return DoubleLiteralElement.Parse(image, services);
			} else {
				return null;
			}
		}

		private static SingleLiteralElement CreateSingle(string image, IServiceProvider services)
		{
			if (image.EndsWith("f", StringComparison.OrdinalIgnoreCase) == true) {
				image = image.Remove(image.Length - 1);
				return SingleLiteralElement.Parse(image, services);
			} else {
				return null;
			}
		}

		private static DecimalLiteralElement CreateDecimal(string image, IServiceProvider services)
		{
			if (image.EndsWith("m", StringComparison.OrdinalIgnoreCase) == true) {
				image = image.Remove(image.Length - 1);
				return DecimalLiteralElement.Parse(image, services);
			} else {
				return null;
			}
		}
	}
}
