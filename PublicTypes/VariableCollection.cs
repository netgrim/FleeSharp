using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

	/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/Class/*' />
    /// Class that manages an expression's variables
	public sealed class VariableCollection : IDictionary<string, object>
	{
		private IDictionary<string, IVariable> MyVariables;
		private ExpressionContext MyContext;

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/ResolveVariableType/*' />
		public event EventHandler<ResolveVariableTypeEventArgs> ResolveVariableType;
		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/ResolveVariableValue/*' />
		public event EventHandler<ResolveVariableValueEventArgs> ResolveVariableValue;

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/ResolveFunction/*' />
		public event EventHandler<ResolveFunctionEventArgs> ResolveFunction;
		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/InvokeFunction/*' />
		public event EventHandler<InvokeFunctionEventArgs> InvokeFunction;

		internal VariableCollection(ExpressionContext context)
		{
			MyContext = context;
			this.CreateDictionary();
			this.HookOptions();
		}

		#region "Methods - Non Public"
		private void HookOptions()
		{
			MyContext.Options.CaseSensitiveChanged += OnOptionsCaseSensitiveChanged;
		}

		private void CreateDictionary()
		{
			MyVariables = new Dictionary<string, IVariable>(MyContext.Options.StringComparer);
		}

		private void OnOptionsCaseSensitiveChanged(object sender, EventArgs e)
		{
			this.CreateDictionary();
		}

		internal void Copy(VariableCollection dest)
		{
			dest.CreateDictionary();
			dest.HookOptions();

			foreach (KeyValuePair<string, IVariable> pair in MyVariables) {
				IVariable copyVariable = pair.Value.Clone();
				dest.MyVariables.Add(pair.Key, copyVariable);
			}
		}

		internal void DefineVariableInternal(string name, Type variableType, object variableValue)
		{
			Utility.AssertNotNull(variableType, "variableType");

			if (MyVariables.ContainsKey(name)) {
				string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.VariableWithNameAlreadyDefined, name);
				throw new ArgumentException(msg);
			}

			var v = CreateVariable(variableType, variableValue);
			MyVariables.Add(name, v);
		}

		internal Type GetVariableTypeInternal(string name)
		{
			IVariable value = null;
			bool success = MyVariables.TryGetValue(name, out value);

			if (success == true) {
				return value.VariableType;
			}

			ResolveVariableTypeEventArgs args = new ResolveVariableTypeEventArgs(name);
			if (ResolveVariableType != null) {
				ResolveVariableType(this, args);
			}

			return args.VariableType;
		}

		private IVariable GetVariable(string name, bool throwOnNotFound)
		{
			IVariable value = null;
			bool success = MyVariables.TryGetValue(name, out value);

			if (!success && throwOnNotFound) {
				string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.UndefinedVariable, name);
				throw new ArgumentException(msg);
			} else {
				return value;
			}
		}

		/// <summary>
		/// Create a variable
		/// </summary>
		/// <param name="variableValueType">The variable's type</param>
		/// <param name="variableValue">The actual value; may be null</param>
		/// <returns>A new variable for the value</returns>
		/// <remarks></remarks>
		private IVariable CreateVariable(Type variableValueType, object variableValue)
		{
			Type variableType = null;

			// Is the variable value an expression?
			IExpression expression = variableValue as IExpression;
			ExpressionOptions options = null;

			if (expression != null) {
				options = expression.Context.Options;
				// Get its result type
				variableValueType = options.ResultType;

                // Create a variable that wraps the expression
                if (!options.IsGeneric)
                {
                    variableType = typeof(DynamicExpressionVariable<>);
                }
                else
                {
                    variableType = typeof(GenericExpressionVariable<>);
                }
            }
            else
            {
                // Create a variable for a regular value
                MyContext.AssertTypeIsAccessible(variableValueType);
                variableType = typeof(GenericVariable<>);
            }

			// Create the generic variable instance
			variableType = variableType.MakeGenericType(variableValueType);
            return Activator.CreateInstance(variableType) as IVariable;
		}

		internal Type ResolveOnDemandFunction(string name, Type[] argumentTypes)
		{
			var args = new ResolveFunctionEventArgs(name, argumentTypes);
			if (ResolveFunction != null) {
				ResolveFunction(this, args);
			}
			return args.ReturnType;
		}

		private static T ReturnGenericValue<T>(object value)
		{
			if (value == null) {
				return default(T);
			} else {
				return (T)value;
			}
		}

		private static void ValidateSetValueType(Type requiredType, object value)
		{
			if (value == null) {
				// Can always assign null value
				return;
			}

			Type valueType = value.GetType();

			if (!requiredType.IsAssignableFrom(valueType)) {
				string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.VariableValueNotAssignableToType, valueType.Name, requiredType.Name);
				throw new ArgumentException(msg);
			}
		}

		static internal MethodInfo GetVariableLoadMethod(Type variableType)
		{
			var mi = typeof(VariableCollection).GetMethod("GetVariableValueInternal", BindingFlags.Public | BindingFlags.Instance);
			mi = mi.MakeGenericMethod(variableType);
			return mi;
		}

		static internal MethodInfo GetFunctionInvokeMethod(Type returnType)
		{
			var mi = typeof(VariableCollection).GetMethod("GetFunctionResultInternal", BindingFlags.Public | BindingFlags.Instance);
			mi = mi.MakeGenericMethod(returnType);
			return mi;
		}

		static internal MethodInfo GetVirtualPropertyLoadMethod(Type returnType)
		{
			var mi = typeof(VariableCollection).GetMethod("GetVirtualPropertyValueInternal", BindingFlags.Public | BindingFlags.Instance);
			mi = mi.MakeGenericMethod(returnType);
			return mi;
		}

		private Dictionary<string, object> GetNameValueDictionary()
		{
			var dict = new Dictionary<string, object>();

			foreach (KeyValuePair<string, IVariable> pair in MyVariables) {
				dict.Add(pair.Key, pair.Value.ValueAsObject);
			}

			return dict;
		}

		#endregion

		#region "Methods - Public"
		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/GetVariableType/*' />
		public Type GetVariableType(string name)
		{
            return this.GetVariable(name, true).VariableType;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/DefineVariable/*' />
		public void DefineVariable(string name, Type variableType)
		{
			this.DefineVariableInternal(name, variableType, null);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/GetVariableValueInternal/*' />
		public T GetVariableValueInternal<T>(string name)
		{
            IVariable vout = null;

            if (MyVariables.TryGetValue(name, out vout) == true)
            {
                return ((IGenericVariable<T>)vout).GetValue();
			}

			var vTemp = new GenericVariable<T>();
			var args = new ResolveVariableValueEventArgs(name, typeof(T));
			if (ResolveVariableValue != null) {
				ResolveVariableValue(this, args);
			}

			ValidateSetValueType(typeof(T), args.VariableValue);
			vTemp.ValueAsObject = args.VariableValue;
            return vTemp.GetValue();
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/GetVirtualPropertyValueInternal/*' />
		public T GetVirtualPropertyValueInternal<T>(string name, object component)
		{
			var coll = TypeDescriptor.GetProperties(component);
			var pd = coll.Find(name, true);
			var value = pd.GetValue(component);

			ValidateSetValueType(typeof(T), value);
			return ReturnGenericValue<T>(value);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/GetFunctionResultInternal/*' />
		public T GetFunctionResultInternal<T>(string name, object[] arguments)
		{
			var args = new InvokeFunctionEventArgs(name, arguments);
			if (InvokeFunction != null) {
				InvokeFunction(this, args);
			}

			object result = args.Result;
			ValidateSetValueType(typeof(T), result);

			return ReturnGenericValue<T>(result);
		}
		#endregion

		#region "IDictionary Implementation"

		public void Add(System.Collections.Generic.KeyValuePair<string, object> item)
		{
			this.Add(item.Key, item.Value);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/Clear/*' />
		public void Clear()
		{
			MyVariables.Clear();
		}

        public bool Contains(System.Collections.Generic.KeyValuePair<string, object> item)
		{
			return ContainsKey(item.Key);
		}

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
		{
			return Contains(item);
		}

        public void CopyTo(System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex)
		{
			var dict = this.GetNameValueDictionary();
            ((ICollection<KeyValuePair<string, object>>)dict).CopyTo(array, arrayIndex);
		}

        public bool Remove(KeyValuePair<string, object> item)
		{
			return Remove(item.Key);
		}

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
		{
			return Remove(item);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/Add/*' />
		public void Add(string name, object value)
		{
			Utility.AssertNotNull(value, "value");
			this.DefineVariableInternal(name, value.GetType(), value);
			this[name] = value;
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/ContainsKey/*' />
		public bool ContainsKey(string name)
		{
			return MyVariables.ContainsKey(name);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/Remove/*' />
		public bool Remove(string name)
		{
			return MyVariables.Remove(name);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/TryGetValue/*' />
		public bool TryGetValue(string key, out object value)
		{
			var v = this.GetVariable(key, false);
            if (v != null)
                value = v.ValueAsObject;
            else
                value = null;

			return (v != null);
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			var dict = this.GetNameValueDictionary();
			return dict.GetEnumerator();
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/Count/*' />
		public int Count {
			get { return MyVariables.Count; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/Item/*' />
		public object this[string name] {
			get {
				var v = this.GetVariable(name, true);
				return v.ValueAsObject;
			}
			set {
				IVariable v = null;

				if (MyVariables.TryGetValue(name, out v) == true) {
					v.ValueAsObject = value;
				} else {
					this.Add(name, value);
				}
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/Keys/*' />
		public System.Collections.Generic.ICollection<string> Keys {
			get { return MyVariables.Keys; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/VariableCollection/Values/*' />
		public System.Collections.Generic.ICollection<object> Values {
			get {
				Dictionary<string, object> dict = this.GetNameValueDictionary();
				return dict.Values;
			}
		}

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            var dict = this.GetNameValueDictionary();
            ((ICollection<KeyValuePair<string, object>>)dict).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

		#endregion


    }
}
