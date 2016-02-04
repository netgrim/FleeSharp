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

namespace Ciloci.Flee
{
	
	internal interface IVariable
	{
		IVariable Clone();
		Type VariableType { get; }
		object ValueAsObject { get; set; }
	}

    internal interface IGenericVariable<T> : IVariable
    {
        T GetValue();
    }

    internal class DynamicExpressionVariable<T> : IGenericVariable<T>
    {


        private IDynamicExpression MyExpression;
        public IVariable Clone()
        {
            DynamicExpressionVariable<T> copy = new DynamicExpressionVariable<T>();
            copy.MyExpression = MyExpression;
            return copy;
        }

        public T GetValue()
        {
            return (T)MyExpression.Evaluate();
        }

        public object ValueAsObject
        {
            get { return MyExpression; }
            set { MyExpression = (IDynamicExpression)value; }
        }

        public System.Type VariableType
        {
            get { return MyExpression.Context.Options.ResultType; }
        }
    }

    internal class GenericExpressionVariable<T> : IGenericVariable<T>
    {


        private IGenericExpression<T> MyExpression;
        public IVariable Clone()
        {
            GenericExpressionVariable<T> copy = new GenericExpressionVariable<T>();
            copy.MyExpression = MyExpression;
            return copy;
        }

        public T GetValue()
        {
            return MyExpression.Evaluate();
        }

        public object ValueAsObject
        {
            get { return MyExpression; }
            set { MyExpression = (IGenericExpression<T>)value; }
        }

        public System.Type VariableType
        {
            get { return MyExpression.Context.Options.ResultType; }
        }
    }

    internal class GenericVariable<T> : IGenericVariable<T>
    {
        public T MyValue;
        public IVariable Clone()
        {
            GenericVariable<T> copy = new GenericVariable<T>();
            copy.MyValue = MyValue;
            return copy;
        }

        public T GetValue()
        {
            return MyValue;
        }

        public System.Type VariableType
        {
            get { return typeof(T); }
        }

        public object ValueAsObject
        {
            get { return MyValue; }
            set
            {
                if (value == null)
                {
                    MyValue = default(T);
                }
                else
                {
                    MyValue = (T)value;
                }
            }
        }
    }
}
