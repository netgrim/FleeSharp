
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

	// The base class for all elements of an expression.
	internal abstract class ExpressionElement
	{


		internal ExpressionElement()
		{
		}

        // All expression elements must be able to emit their IL
		public abstract void Emit(FleeILGenerator ilg, IServiceProvider services);
        // All expression elements must expose the Type they evaluate to
		public abstract Type ResultType { get; }

		public override string ToString()
		{
			return this.Name;
		}

		protected void ThrowCompileException(string messageKey, CompileExceptionReason reason, params object[] arguments)
		{
			var messageTemplate = FleeResourceManager.Instance.GetCompileErrorString(messageKey);
			var message = string.Format(messageTemplate, arguments);
			message = string.Concat(this.Name, ": ", message);
			throw new ExpressionCompileException(message, reason);
		}

		protected void ThrowAmbiguousCallException(Type leftType, Type rightType, object operation)
		{
			this.ThrowCompileException(CompileErrorResourceKeys.AmbiguousOverloadedOperator, CompileExceptionReason.AmbiguousMatch, leftType.Name, rightType.Name, operation);
		}

		protected FleeILGenerator CreateTempFleeILGenerator(FleeILGenerator ilgCurrent)
		{
			var dm = new DynamicMethod("temp", typeof(Int32), null, this.GetType());
			return new FleeILGenerator(dm.GetILGenerator(), ilgCurrent.Length, true);
		}

		protected string Name {
			get {
				var key = this.GetType().Name;
				var value = FleeResourceManager.Instance.GetElementNameString(key);
				Debug.Assert(value != null, string.Format("Element name for '{0}' not in resource file", key));
				return value;
			}
		}
	}
}
