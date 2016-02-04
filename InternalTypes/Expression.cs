using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using System.ComponentModel.Design;

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

	internal class Expression<T> : IExpression, IDynamicExpression, IGenericExpression<T>
	{

		private string MyExpression;
		private ExpressionContext MyContext;
		private ExpressionOptions MyOptions;
		private ExpressionInfo MyInfo;
		private ExpressionEvaluator<T> MyEvaluator;

		private object MyOwner;
		private const string EmitAssemblyName = "FleeExpression";

		private const string DynamicMethodName = "Flee Expression";
		public Expression(string expression, ExpressionContext context, bool isGeneric)
		{
			Utility.AssertNotNull(expression, "expression");
			MyExpression = expression;
			MyOwner = context.ExpressionOwner;

			MyContext = context;

			if (context.NoClone == false) {
				MyContext = context.CloneInternal(false);
			}

			MyInfo = new ExpressionInfo();

			this.SetupOptions(MyContext.Options, isGeneric);

			MyContext.Imports.ImportOwner(MyOptions.OwnerType);

			this.ValidateOwner(MyOwner);

			this.Compile(expression, MyOptions);

			if ((MyContext.CalculationEngine != null)) {
				MyContext.CalculationEngine.FixTemporaryHead(this, MyContext, MyOptions.ResultType);
			}
		}

		private void SetupOptions(ExpressionOptions options, bool isGeneric)
		{
			// Make sure we clone the options
			MyOptions = options;
			MyOptions.IsGeneric = isGeneric;

			if (isGeneric) {
				MyOptions.ResultType = typeof(T);
			}

			MyOptions.SetOwnerType(MyOwner.GetType());
		}

		private void Compile(string expression, ExpressionOptions options)
		{
			// Add the services that will be used by elements during the compile
			var services = new ServiceContainer();
			this.AddServices(services);

			// Parse and get the root element of the parse tree
			var topElement = MyContext.Parse(expression, services);

			if (options.ResultType == null) {
				options.ResultType = topElement.ResultType;
			}

			var rootElement = new RootExpressionElement(topElement, options.ResultType);
			var dm = this.CreateDynamicMethod();
			var ilg = new FleeILGenerator(dm.GetILGenerator());

			// Emit the IL
			rootElement.Emit(ilg, services);

			ilg.ValidateLength();

			// Emit to an assembly if required
			if (options.EmitToAssembly == true) {
				EmitToAssembly(rootElement, services);
			}

			var delegateType = typeof(ExpressionEvaluator<>).MakeGenericType(typeof(T));
			MyEvaluator = dm.CreateDelegate(delegateType) as ExpressionEvaluator<T>;
		}

		private DynamicMethod CreateDynamicMethod()
		{
			// Create the dynamic method
			Type[] parameterTypes = {
				typeof(object),
				typeof(ExpressionContext),
				typeof(VariableCollection)
			};
            return new DynamicMethod(DynamicMethodName, typeof(T), parameterTypes, MyOptions.OwnerType);
		}

		private void AddServices(IServiceContainer dest)
		{
			dest.AddService(typeof(ExpressionOptions), MyOptions);
			dest.AddService(typeof(ExpressionParserOptions), MyContext.ParserOptions);
			dest.AddService(typeof(ExpressionContext), MyContext);
			dest.AddService(typeof(IExpression), this);
			dest.AddService(typeof(ExpressionInfo), MyInfo);
		}

		private static void EmitToAssembly(ExpressionElement rootElement, IServiceContainer services)
		{
			var assemblyName = new AssemblyName(EmitAssemblyName);

			var assemblyFileName = string.Format("{0}.dll", EmitAssemblyName);

			var assemblyBuilder = System.AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyFileName, assemblyFileName);

			var mb = moduleBuilder.DefineGlobalMethod("Evaluate", MethodAttributes.Public | MethodAttributes.Static, typeof(T), new Type[] {
				typeof(object),
				typeof(ExpressionContext),
				typeof(VariableCollection)
			});
			var ilg = new FleeILGenerator(mb.GetILGenerator());

			rootElement.Emit(ilg, services);

			moduleBuilder.CreateGlobalFunctions();
			assemblyBuilder.Save(assemblyFileName);
		}

		private void ValidateOwner(object owner)
		{
			Utility.AssertNotNull(owner, "owner");

			if (MyOptions.OwnerType.IsAssignableFrom(owner.GetType()) == false) {
				string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.NewOwnerTypeNotAssignableToCurrentOwner);
				throw new ArgumentException(msg);
			}
		}

		public object Evaluate()
		{
			return MyEvaluator(MyOwner, MyContext, MyContext.Variables);
		}

		public T EvaluateGeneric()
		{
			return MyEvaluator(MyOwner, MyContext, MyContext.Variables);
		}
		T IGenericExpression<T>.Evaluate()
		{
			return EvaluateGeneric();
		}

		public IExpression Clone()
		{
            var copy = this.MemberwiseClone() as Expression<T>;
			copy.MyContext = MyContext.CloneInternal(true);
			copy.MyOptions = copy.MyContext.Options;
			return copy;
		}

		public override string ToString()
		{
			return MyExpression;
		}

		internal Type ResultType {
			get { return MyOptions.ResultType; }
		}

		public string Text {
			get { return MyExpression; }
		}

		public ExpressionInfo Info1 {
			get { return MyInfo; }
		}
		ExpressionInfo IExpression.Info {
			get { return Info1; }
		}

		public object Owner {
			get { return MyOwner; }
			set {
				this.ValidateOwner(value);
				MyOwner = value;
			}
		}

		public ExpressionContext Context {
			get { return MyContext; }
		}
	}
}
