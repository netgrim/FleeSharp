
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

	internal abstract class IntegralLiteralElement : LiteralElement
	{


		protected IntegralLiteralElement()
		{
		}

        // Attempt to find the first type of integer that a number can fit into
		public static LiteralElement Create(string image, bool isHex, bool negated, IServiceProvider services)
		{
			var comparison = StringComparison.OrdinalIgnoreCase;

			if (isHex == false) {
				// Create a real element if required
				var realElement = RealLiteralElement.CreateFromInteger(image, services);

				if ((realElement != null)) {
					return realElement;
				}
			}

			bool hasUSuffix = image.EndsWith("u", comparison) & !image.EndsWith("lu", comparison);
			bool hasLSuffix = image.EndsWith("l", comparison) & !image.EndsWith("ul", comparison);
			bool hasULSuffix = image.EndsWith("ul", comparison) | image.EndsWith("lu", comparison);
			bool hasSuffix = hasUSuffix | hasLSuffix | hasULSuffix;

			LiteralElement constant = null;
			var numStyles = System.Globalization.NumberStyles.Integer;

			if (isHex == true) {
				numStyles = System.Globalization.NumberStyles.AllowHexSpecifier;
				image = image.Remove(0, 2);
			}

			if (hasSuffix == false) {
				// If the literal has no suffix, it has the first of these types in which its value can be represented: int, uint, long, ulong.
				constant = Int32LiteralElement.TryCreate(image, isHex, negated);

				if ((constant != null)) {
					return constant;
				}

				constant = UInt32LiteralElement.TryCreate(image, numStyles);

				if ((constant != null)) {
					return constant;
				}

				constant = Int64LiteralElement.TryCreate(image, isHex, negated);

				if ((constant != null)) {
					return constant;
				}

				return new UInt64LiteralElement(image, numStyles);
			} else if (hasUSuffix == true) {
				image = image.Remove(image.Length - 1);
				// If the literal is suffixed by U or u, it has the first of these types in which its value can be represented: uint, ulong.

				constant = UInt32LiteralElement.TryCreate(image, numStyles);

				if ((constant != null)) {
					return constant;
				} else {
					return new UInt64LiteralElement(image, numStyles);
				}
			} else if (hasLSuffix == true) {
				// If the literal is suffixed by L or l, it has the first of these types in which its value can be represented: long, ulong.
				image = image.Remove(image.Length - 1);

				constant = Int64LiteralElement.TryCreate(image, isHex, negated);

				if ((constant != null)) {
					return constant;
				} else {
					return new UInt64LiteralElement(image, numStyles);
				}
			} else {
				// If the literal is suffixed by UL, Ul, uL, ul, LU, Lu, lU, or lu, it is of type ulong.
				Debug.Assert(hasULSuffix == true, "expecting ul suffix");
				image = image.Remove(image.Length - 2);
				return new UInt64LiteralElement(image, numStyles);
			}
		}
	}
}
