using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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

	/// <summary>
	/// Represents a function call
	/// </summary>
	/// <remarks></remarks>
	internal class FunctionCallElement : MemberElement
	{

		private ArgumentList MyArguments;
		private ICollection<MethodInfo> MyMethods;
		private CustomMethodInfo MyTargetMethodInfo;

		private Type MyOnDemandFunctionReturnType;
		public FunctionCallElement(string name, ArgumentList arguments)
		{
			this.MyName = name;
			MyArguments = arguments;
		}

		internal FunctionCallElement(string name, ICollection<MethodInfo> methods, ArgumentList arguments)
		{
			MyName = name;
			MyArguments = arguments;
			MyMethods = methods;
		}

		protected override void ResolveInternal()
		{
			// Get the types of our arguments
			var argTypes = MyArguments.GetArgumentTypes();
			// Find all methods with our name on the type
			var methods = MyMethods;

			if (methods == null) {
				// Convert member info to method info
				var arr = this.GetMembers(MemberTypes.Method);
				var arr2 = new MethodInfo[arr.Length];
				Array.Copy(arr, arr2, arr.Length);
				methods = arr2;
			}

			if (methods.Count > 0) {
				// More than one method exists with this name			
				this.BindToMethod(methods, MyPrevious, argTypes);
				return;
			}

			// No methods with this name exist; try to bind to an on-demand function
			MyOnDemandFunctionReturnType = MyContext.Variables.ResolveOnDemandFunction(MyName, argTypes);

			if (MyOnDemandFunctionReturnType == null) {
				// Failed to bind to a function
				this.ThrowFunctionNotFoundException(MyPrevious);
			}
		}

		private void ThrowFunctionNotFoundException(MemberElement previous)
		{
			if (previous == null) {
				base.ThrowCompileException(CompileErrorResourceKeys.UndefinedFunction, CompileExceptionReason.UndefinedName, MyName, MyArguments);
			} else {
				base.ThrowCompileException(CompileErrorResourceKeys.UndefinedFunctionOnType, CompileExceptionReason.UndefinedName, MyName, MyArguments, previous.TargetType.Name);
			}
		}

		private void ThrowNoAccessibleMethodsException(MemberElement previous)
		{
			if (previous == null) {
				base.ThrowCompileException(CompileErrorResourceKeys.NoAccessibleMatches, CompileExceptionReason.AccessDenied, MyName, MyArguments);
			} else {
				base.ThrowCompileException(CompileErrorResourceKeys.NoAccessibleMatchesOnType, CompileExceptionReason.AccessDenied, MyName, MyArguments, previous.TargetType.Name);
			}
		}

		private void ThrowAmbiguousMethodCallException()
		{
			base.ThrowCompileException(CompileErrorResourceKeys.AmbiguousCallOfFunction, CompileExceptionReason.AmbiguousMatch, MyName, MyArguments);
		}

        // Try to find a match from a set of methods
		private void BindToMethod(ICollection<MethodInfo> methods, MemberElement previous, Type[] argTypes)
		{
			List<CustomMethodInfo> customInfos = new List<CustomMethodInfo>();

			// Wrap the MethodInfos in our custom class
			foreach (MethodInfo mi in methods) {
				CustomMethodInfo cmi = new CustomMethodInfo(mi);
				customInfos.Add(cmi);
			}

			// Discard any methods that cannot qualify as overloads
			CustomMethodInfo[] arr = customInfos.ToArray();
			customInfos.Clear();

			foreach (CustomMethodInfo cmi in arr) {
				if (cmi.IsMatch(argTypes) == true) {
					customInfos.Add(cmi);
				}
			}

			if (customInfos.Count == 0) {
				// We have no methods that can qualify as overloads; throw exception
				this.ThrowFunctionNotFoundException(previous);
			} else {
				// At least one method matches our criteria; do our custom overload resolution
				this.ResolveOverloads(customInfos.ToArray(), previous, argTypes);
			}
		}

        // Find the best match from a set of overloaded methods
		private void ResolveOverloads(CustomMethodInfo[] infos, MemberElement previous, Type[] argTypes)
		{
			// Compute a score for each candidate
			foreach (CustomMethodInfo cmi in infos) {
				cmi.ComputeScore(argTypes);
			}

			// Sort array from best to worst matches
			Array.Sort<CustomMethodInfo>(infos);

			// Discard any matches that aren't accessible
			infos = this.GetAccessibleInfos(infos);

			// No accessible methods left
			if (infos.Length == 0) {
				this.ThrowNoAccessibleMethodsException(previous);
			}

			// Handle case where we have more than one match with the same score
			this.DetectAmbiguousMatches(infos);

			// If we get here, then there is only one best match
			MyTargetMethodInfo = infos[0];
		}

		private CustomMethodInfo[] GetAccessibleInfos(CustomMethodInfo[] infos)
		{
			List<CustomMethodInfo> accessible = new List<CustomMethodInfo>();

			foreach (CustomMethodInfo cmi in infos) {
				if (cmi.IsAccessible(this) == true) {
					accessible.Add(cmi);
				}
			}

			return accessible.ToArray();
		}

        // Handle case where we have overloads with the same score
		private void DetectAmbiguousMatches(CustomMethodInfo[] infos)
		{
			List<CustomMethodInfo> sameScores = new List<CustomMethodInfo>();
			CustomMethodInfo first = infos[0];

			// Find all matches with the same score as the best match
			foreach (CustomMethodInfo cmi in infos) {
				if (((IEquatable<CustomMethodInfo>)cmi).Equals(first) == true) {
					sameScores.Add(cmi);
				}
			}

			// More than one accessible match with the same score exists
			if (sameScores.Count > 1) {
				this.ThrowAmbiguousMethodCallException();
			}
		}

		protected override void Validate()
		{
			base.Validate();

			if ((MyOnDemandFunctionReturnType != null)) {
				return;
			}

			// Any function reference in an expression must return a value
			if (object.ReferenceEquals(this.Method.ReturnType, typeof(void))) {
				base.ThrowCompileException(CompileErrorResourceKeys.FunctionHasNoReturnValue, CompileExceptionReason.FunctionHasNoReturnValue, MyName);
			}
		}

		public override void Emit(FleeILGenerator ilg, IServiceProvider services)
		{
			base.Emit(ilg, services);

			ExpressionElement[] elements = MyArguments.ToArray();

			// If we are an on-demand function, then emit that and exit
			if ((MyOnDemandFunctionReturnType != null)) {
				this.EmitOnDemandFunction(elements, ilg, services);
				return;
			}

			bool isOwnerMember = MyOptions.IsOwnerType(this.Method.ReflectedType);

			// Load the owner if required
			if (MyPrevious == null && isOwnerMember == true && this.IsStatic == false) {
				this.EmitLoadOwner(ilg);
			}

			this.EmitFunctionCall(this.NextRequiresAddress, ilg, services);
		}

		private void EmitOnDemandFunction(ExpressionElement[] elements, FleeILGenerator ilg, IServiceProvider services)
		{
			// Load the variable collection
			EmitLoadVariables(ilg);
			// Load the function name
			ilg.Emit(OpCodes.Ldstr, MyName);
			// Load the arguments array
			EmitElementArrayLoad(elements, typeof(object), ilg, services);

			// Call the function to get the result
			MethodInfo mi = VariableCollection.GetFunctionInvokeMethod(MyOnDemandFunctionReturnType);

			this.EmitMethodCall(mi, ilg);
		}

        // Emit the arguments to a paramArray method call
		private void EmitParamArrayArguments(ParameterInfo[] parameters, ExpressionElement[] elements, FleeILGenerator ilg, IServiceProvider services)
		{
			// Get the fixed parameters
			ParameterInfo[] fixedParameters = new ParameterInfo[MyTargetMethodInfo.MyFixedArgTypes.Length];
			Array.Copy(parameters, fixedParameters, fixedParameters.Length);

			// Get the corresponding fixed parameters
			ExpressionElement[] fixedElements = new ExpressionElement[MyTargetMethodInfo.MyFixedArgTypes.Length];
			Array.Copy(elements, fixedElements, fixedElements.Length);

			// Emit the fixed arguments
			this.EmitRegularFunctionInternal(fixedParameters, fixedElements, ilg, services);

			// Get the paramArray arguments
			ExpressionElement[] paramArrayElements = new ExpressionElement[elements.Length - fixedElements.Length];
			Array.Copy(elements, fixedElements.Length, paramArrayElements, 0, paramArrayElements.Length);

			// Emit them into an array
			EmitElementArrayLoad(paramArrayElements, MyTargetMethodInfo.ParamArrayElementType, ilg, services);
		}

        // Emit elements into an array
		private static void EmitElementArrayLoad(ExpressionElement[] elements, Type arrayElementType, FleeILGenerator ilg, IServiceProvider services)
		{
			// Load the array length
			LiteralElement.EmitLoad(elements.Length, ilg);

			// Create the array
			ilg.Emit(OpCodes.Newarr, arrayElementType);

			// Store the new array in a unique local and remember the index
			var local = ilg.DeclareLocal(arrayElementType.MakeArrayType());
			int arrayLocalIndex = local.LocalIndex;
			Utility.EmitStoreLocal(ilg, arrayLocalIndex);

			for (int i = 0; i <= elements.Length - 1; i++) {
				// Load the array
				Utility.EmitLoadLocal(ilg, arrayLocalIndex);
				// Load the index
				LiteralElement.EmitLoad(i, ilg);
				// Emit the element (with any required conversions)
				ExpressionElement element = elements[i];
				element.Emit(ilg, services);
				ImplicitConverter.EmitImplicitConvert(element.ResultType, arrayElementType, ilg);
				// Store it into the array
				Utility.EmitArrayStore(ilg, arrayElementType);
			}

			// Load the array
			Utility.EmitLoadLocal(ilg, arrayLocalIndex);
		}

		public void EmitFunctionCall(bool nextRequiresAddress, FleeILGenerator ilg, IServiceProvider services)
		{
			var parameters = this.Method.GetParameters();
			var elements = MyArguments.ToArray();

			// Emit either a regular or paramArray call
			if (MyTargetMethodInfo.IsParamArray == false) {
				this.EmitRegularFunctionInternal(parameters, elements, ilg, services);
			} else {
				this.EmitParamArrayArguments(parameters, elements, ilg, services);
			}

			MemberElement.EmitMethodCall(this.ResultType, nextRequiresAddress, this.Method, ilg);
		}

        // Emit the arguments to a regular method call
		private void EmitRegularFunctionInternal(ParameterInfo[] parameters, ExpressionElement[] elements, FleeILGenerator ilg, IServiceProvider services)
		{
			Debug.Assert(parameters.Length == elements.Length, "argument count mismatch");

			// Emit each element and any required conversions to the actual parameter type
			for (int i = 0; i <= parameters.Length - 1; i++) {
				var element = elements[i];
				var pi = parameters[i];
				element.Emit(ilg, services);
				bool success = ImplicitConverter.EmitImplicitConvert(element.ResultType, pi.ParameterType, ilg);
				Debug.Assert(success, "conversion failed");
			}
		}

		/// <summary>
		/// The method info we will be calling
		/// </summary>	
		private MethodInfo Method {
			get { return MyTargetMethodInfo.Target; }
		}

		public override Type ResultType {
			get {
				if ((MyOnDemandFunctionReturnType != null)) {
					return MyOnDemandFunctionReturnType;
				} else {
					return this.Method.ReturnType;
				}
			}
		}

		protected override bool RequiresAddress {
			get { return !IsGetTypeMethod(this.Method); }
		}

		protected override bool IsPublic {
			get { return this.Method.IsPublic; }
		}

		public override bool IsStatic {
			get { return this.Method.IsStatic; }
		}
	}
}
