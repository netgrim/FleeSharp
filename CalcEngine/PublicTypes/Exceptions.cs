
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

namespace Ciloci.Flee.CalcEngine
{

	/// <include file='Resources/DocComments.xml' path='DocComments/CircularReferenceException/Class/*' />	
	[Serializable()]
	public class CircularReferenceException : System.Exception
	{


		private string MyCircularReferenceSource;

		internal CircularReferenceException()
		{
		}

		internal CircularReferenceException(string circularReferenceSource)
		{
			MyCircularReferenceSource = circularReferenceSource;
		}

		private CircularReferenceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{
		}

		public override string Message {
			get {
				if (MyCircularReferenceSource == null) {
					return "Circular reference detected in calculation engine";
				} else {
					return string.Format("Circular reference detected in calculation engine at '{0}'", MyCircularReferenceSource);
				}
			}
		}
	}

	/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="BatchLoadCompileException"]/*' />	
	[Serializable()]
	public class BatchLoadCompileException : Exception
	{

		private string MyAtomName;

		private string MyExpressionText;
		internal BatchLoadCompileException(string atomName, string expressionText, ExpressionCompileException innerException) : base(string.Format("Batch Load: The expression for atom '${0}' could not be compiled", atomName), innerException)
		{
			MyAtomName = atomName;
			MyExpressionText = expressionText;
		}

		private BatchLoadCompileException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{
			MyAtomName = info.GetString("AtomName");
			MyExpressionText = info.GetString("ExpressionText");
		}

		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("AtomName", MyAtomName);
			info.AddValue("ExpressionText", MyExpressionText);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="BatchLoadCompileException.AtomName"]/*' />	
		public string AtomName {
			get { return MyAtomName; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/Member[@name="BatchLoadCompileException.ExpressionText"]/*' />	
		public string ExpressionText {
			get { return MyExpressionText; }
		}
	}

}
