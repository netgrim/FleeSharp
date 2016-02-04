using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;

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

	internal enum BinaryArithmeticOperation
	{
		Add,
		Subtract,
		Multiply,
		Divide,
		Mod,
		Power
	}

    internal enum LogicalCompareOperation
    {
        LessThan,
        GreaterThan,
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual
    }

    internal enum AndOrOperation
    {
        And,
        Or
    }

    internal enum ShiftOperation
    {
        LeftShift,
        RightShift
    }

    internal delegate T ExpressionEvaluator<T>(object owner, ExpressionContext context, VariableCollection variables);

    internal abstract class CustomBinder : Binder
    {

        public override System.Reflection.FieldInfo BindToField(System.Reflection.BindingFlags bindingAttr, System.Reflection.FieldInfo[] match, object value, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
        {
            state = null;
            return null;
        }

        public override object ChangeType(object value, System.Type type, System.Globalization.CultureInfo culture)
        {
            return null;
        }


        public override void ReorderArgumentArray(ref object[] args, object state)
        {
        }

        public override System.Reflection.PropertyInfo SelectProperty(System.Reflection.BindingFlags bindingAttr, System.Reflection.PropertyInfo[] match, System.Type returnType, System.Type[] indexes, System.Reflection.ParameterModifier[] modifiers)
        {
            return null;
        }
    }

    internal class ExplicitOperatorMethodBinder : CustomBinder
    {

        private Type MyReturnType;

        private Type MyArgType;
        public ExplicitOperatorMethodBinder(Type returnType, Type argType)
        {
            MyReturnType = returnType;
            MyArgType = argType;
        }

        public override System.Reflection.MethodBase SelectMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, System.Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            foreach (MethodInfo mi in match)
            {
                ParameterInfo[] parameters = mi.GetParameters();
                ParameterInfo firstParameter = parameters[0];
                if (object.ReferenceEquals(firstParameter.ParameterType, MyArgType) & object.ReferenceEquals(mi.ReturnType, MyReturnType))
                {
                    return mi;
                }
            }
            return null;
        }
    }

    internal class BinaryOperatorBinder : CustomBinder
    {

        private Type MyLeftType;

        private Type MyRightType;
        public BinaryOperatorBinder(Type leftType, Type rightType)
        {
            MyLeftType = leftType;
            MyRightType = rightType;
        }

        public override MethodBase SelectMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, System.Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            foreach (MethodInfo mi in match)
            {
                ParameterInfo[] parameters = mi.GetParameters();
                bool leftValid = ImplicitConverter.EmitImplicitConvert(MyLeftType, parameters[0].ParameterType, null);
                bool rightValid = ImplicitConverter.EmitImplicitConvert(MyRightType, parameters[1].ParameterType, null);

                if (leftValid == true & rightValid == true)
                {
                    return mi;
                }
            }
            return null;
        }
    }

    internal class Null
    {

    }

    internal class DefaultExpressionOwner
    {


        private static DefaultExpressionOwner OurInstance = new DefaultExpressionOwner();

        private DefaultExpressionOwner()
        {
        }

        public static object Instance
        {
            get { return OurInstance; }
        }
    }

    // Helper class to resolve overloads
    internal class CustomMethodInfo : IComparable<CustomMethodInfo>, IEquatable<CustomMethodInfo>
    {

        // Method we are wrapping
        private MethodInfo MyTarget;
        // The rating of how close the method matches the given arguments (0 is best)
        private float MyScore;
        public bool IsParamArray;
        public Type[] MyFixedArgTypes;
        public Type[] MyParamArrayArgTypes;

        public Type ParamArrayElementType;
        public CustomMethodInfo(MethodInfo target)
        {
            MyTarget = target;
        }

        public void ComputeScore(Type[] argTypes)
        {
            ParameterInfo[] @params = MyTarget.GetParameters();

            if (@params.Length == 0)
            {
                MyScore = 0;
            }
            else if (IsParamArray == true)
            {
                MyScore = this.ComputeScoreForParamArray(@params, argTypes);
            }
            else
            {
                MyScore = this.ComputeScoreInternal(@params, argTypes);
            }
        }

        // Compute a score showing how close our method matches the given argument types
        private float ComputeScoreInternal(ParameterInfo[] parameters, Type[] argTypes)
        {
            // Our score is the average of the scores of each parameter.  The lower the score, the better the match.
            int sum = ComputeSum(parameters, argTypes);

            return sum / argTypes.Length;
        }

        private static int ComputeSum(ParameterInfo[] parameters, Type[] argTypes)
        {
            Debug.Assert(parameters.Length == argTypes.Length);
            int sum = 0;

            for (int i = 0; i <= parameters.Length - 1; i++)
            {
                sum += ImplicitConverter.GetImplicitConvertScore(argTypes[i], parameters[i].ParameterType);
            }

            return sum;
        }

        private float ComputeScoreForParamArray(ParameterInfo[] parameters, Type[] argTypes)
        {
            ParameterInfo paramArrayParameter = parameters[parameters.Length - 1];
            int fixedParameterCount = paramArrayParameter.Position;

            ParameterInfo[] fixedParameters = new ParameterInfo[fixedParameterCount];

            System.Array.Copy(parameters, fixedParameters, fixedParameterCount);

            int fixedSum = ComputeSum(fixedParameters, MyFixedArgTypes);

            Type paramArrayElementType = paramArrayParameter.ParameterType.GetElementType();

            int paramArraySum = 0;

            foreach (Type argType in MyParamArrayArgTypes)
            {
                paramArraySum += ImplicitConverter.GetImplicitConvertScore(argType, paramArrayElementType);
            }

            float score = 0;

            if (argTypes.Length > 0)
            {
                score = (fixedSum + paramArraySum) / argTypes.Length;
            }
            else
            {
                score = 0;
            }

            // The param array score gets a slight penalty so that it scores worse than direct matches
            return score + 1;
        }

        public bool IsAccessible(MemberElement owner)
        {
            return owner.IsMemberAccessible(MyTarget);
        }

        // Is the given MethodInfo usable as an overload?
        public bool IsMatch(Type[] argTypes)
        {
            ParameterInfo[] parameters = MyTarget.GetParameters();

            // If there are no parameters and no arguments were passed, then we are a match.
            if (parameters.Length == 0 & argTypes.Length == 0)
            {
                return true;
            }

            // If there are no parameters but there are arguments, we cannot be a match
            if (parameters.Length == 0 & argTypes.Length > 0)
            {
                return false;
            }

            // Is the last parameter a paramArray?
            ParameterInfo lastParam = parameters[parameters.Length - 1];

            if (lastParam.IsDefined(typeof(ParamArrayAttribute), false) == false)
            {
                if ((parameters.Length != argTypes.Length))
                {
                    // Not a paramArray and parameter and argument counts don't match
                    return false;
                }
                else
                {
                    // Regular method call, do the test
                    return AreValidArgumentsForParameters(argTypes, parameters);
                }
            }

            // At this point, we are dealing with a paramArray call

            // If the parameter and argument counts are equal and there is an implicit conversion from one to the other, we are a match.
            if (parameters.Length == argTypes.Length && AreValidArgumentsForParameters(argTypes, parameters) == true)
            {
                return true;
            }
            else if (this.IsParamArrayMatch(argTypes, parameters, lastParam) == true)
            {
                IsParamArray = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsParamArrayMatch(Type[] argTypes, ParameterInfo[] parameters, ParameterInfo paramArrayParameter)
        {
            // Get the count of arguments before the paramArray parameter
            int fixedParameterCount = paramArrayParameter.Position;
            Type[] fixedArgTypes = new Type[fixedParameterCount];
            ParameterInfo[] fixedParameters = new ParameterInfo[fixedParameterCount];

            // Get the argument types and parameters before the paramArray
            System.Array.Copy(argTypes, fixedArgTypes, fixedParameterCount);
            System.Array.Copy(parameters, fixedParameters, fixedParameterCount);

            // If the fixed arguments don't match, we are not a match
            if (AreValidArgumentsForParameters(fixedArgTypes, fixedParameters) == false)
            {
                return false;
            }

            // Get the type of the paramArray
            ParamArrayElementType = paramArrayParameter.ParameterType.GetElementType();

            // Get the types of the arguments passed to the paramArray
            Type[] paramArrayArgTypes = new Type[argTypes.Length - fixedParameterCount];
            System.Array.Copy(argTypes, fixedParameterCount, paramArrayArgTypes, 0, paramArrayArgTypes.Length);

            // Check each argument
            foreach (Type argType in paramArrayArgTypes)
            {
                if (ImplicitConverter.EmitImplicitConvert(argType, ParamArrayElementType, null) == false)
                {
                    return false;
                }
            }

            MyFixedArgTypes = fixedArgTypes;
            MyParamArrayArgTypes = paramArrayArgTypes;

            // They all match, so we are a match
            return true;
        }

        private static bool AreValidArgumentsForParameters(Type[] argTypes, ParameterInfo[] parameters)
        {
            Debug.Assert(argTypes.Length == parameters.Length);
            // Match if every given argument is implicitly convertible to the method's corresponding parameter
            for (int i = 0; i <= argTypes.Length - 1; i++)
            {
                if (ImplicitConverter.EmitImplicitConvert(argTypes[i], parameters[i].ParameterType, null) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public int CompareTo(CustomMethodInfo other)
        {
            return MyScore.CompareTo(other.MyScore);
        }

        bool IEquatable<CustomMethodInfo>.Equals(CustomMethodInfo other)
        {
            return MyScore == other.MyScore;
        }

        public MethodInfo Target
        {
            get { return MyTarget; }
        }
    }

    internal class ShortCircuitInfo
    {

        public Stack Operands;
        public Stack Operators;

        public BranchManager Branches;
        public ShortCircuitInfo()
        {
            this.Operands = new Stack();
            this.Operators = new Stack();
            this.Branches = new BranchManager();
        }

        public void ClearTempState()
        {
            this.Operands.Clear();
            this.Operators.Clear();
        }
    }

    // Wraps an expression element so that it is loaded from a local slot
    internal class LocalBasedElement : ExpressionElement
    {

        private int MyIndex;

        private ExpressionElement MyTarget;
        public LocalBasedElement(ExpressionElement target, int index)
        {
            MyTarget = target;
            MyIndex = index;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Utility.EmitLoadLocal(ilg, MyIndex);
        }

        public override System.Type ResultType
        {
            get { return MyTarget.ResultType; }
        }
    }

    /// <summary>
    /// Helper class for storing strongly-typed properties
    /// </summary>
    /// <remarks></remarks>
    internal class PropertyDictionary
    {


        private Dictionary<string, object> MyProperties;
        public PropertyDictionary()
        {
            MyProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public PropertyDictionary Clone()
        {
            PropertyDictionary copy = new PropertyDictionary();

            foreach (KeyValuePair<string, object> pair in MyProperties)
            {
                copy.SetValue(pair.Key, pair.Value);
            }

            return copy;
        }

        public T GetValue<T>(string name)
        {
            object value = null;
            if (MyProperties.TryGetValue(name, out value) == false)
            {
                Debug.Fail(string.Format("Unknown property '{0}'", name));
            }
            return (T)value;
        }

        public void SetToDefault<T>(string name)
        {
            this.SetValue(name, default(T));
        }

        public void SetValue(string name, object value)
        {
            MyProperties[name] = value;
        }

        public bool Contains(string name)
        {
            return MyProperties.ContainsKey(name);
        }
    }
}

