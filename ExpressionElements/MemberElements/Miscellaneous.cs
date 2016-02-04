using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;

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

    // Elements for field, property, array, and function access

	internal class ExpressionMemberElement : MemberElement
	{


		private ExpressionElement MyElement;
		public ExpressionMemberElement(ExpressionElement element)
		{
			MyElement = element;
		}


		protected override void ResolveInternal()
		{
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			base.Emit(ilg, services);
			MyElement.Emit(ilg, services);
			if (MyElement.ResultType.IsValueType == true) {
				EmitValueTypeLoadAddress(ilg, this.ResultType);
			}
		}

		protected override bool SupportsInstance {
			get { return true; }
		}

		protected override bool IsPublic {
			get { return true; }
		}

		public override bool IsStatic {
			get { return false; }
		}

		public override System.Type ResultType {
			get { return MyElement.ResultType; }
		}
	}
}
