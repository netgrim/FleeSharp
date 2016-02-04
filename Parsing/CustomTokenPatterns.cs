
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PerCederberg.Grammatica.Runtime;

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

    internal abstract class CustomTokenPattern : TokenPattern
    {

        public void Initialize(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            this.ComputeToken(id, name, type, pattern, context);
        }

        protected abstract void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context);

        protected void SetData(int id, string name, PatternType type, string pattern)
        {
            m_id = id;
            m_name = name;
            m_type = type;
            m_pattern = pattern;
        }
    }

    internal class RealPattern : CustomTokenPattern
    {

        protected override void ComputeToken(int id, string name, PerCederberg.Grammatica.Runtime.TokenPattern.PatternType type, string pattern, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;

            char digitsBeforePattern = (options.RequireDigitsBeforeDecimalPoint ? '+' : '*');

            pattern = string.Format(pattern, digitsBeforePattern, options.DecimalSeparator);

            this.SetData(id, name, type, pattern);
        }
    }

    internal class ArgumentSeparatorPattern : CustomTokenPattern
    {

        protected override void ComputeToken(int id, string name, PerCederberg.Grammatica.Runtime.TokenPattern.PatternType type, string pattern, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;
            this.SetData(id, name, type, options.FunctionArgumentSeparator.ToString());
        }
    }
}

