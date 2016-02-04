using System.IO;
using PerCederberg.Grammatica.Runtime;
using Ciloci.Flee;

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

/**
 * <remarks>A character stream tokenizer.</remarks>
 */
internal class ExpressionTokenizer : Tokenizer {

    private ExpressionContext _expressionContext;

    /**
     * <summary>Creates a new tokenizer for the specified input
     * stream.</summary>
     *
     * <param name='input'>the input stream to read</param>
     * <param name='expressionContext'>the expression context to work on</param>
     *
     * <exception cref='ParserCreationException'>if the tokenizer
     * couldn't be initialized correctly</exception>
     */
    public ExpressionTokenizer(TextReader input, ExpressionContext expressionContext)
        : base(input, true)
    {

        _expressionContext = expressionContext;
        CreatePatterns();
    }

    /**
     * <summary>Initializes the tokenizer by creating all the token
     * patterns.</summary>
     *
     * <exception cref='ParserCreationException'>if the tokenizer
     * couldn't be initialized correctly</exception>
     */
    private void CreatePatterns() {
        TokenPattern  pattern;

        pattern = new TokenPattern((int) ExpressionConstants.ADD,
                                   "ADD",
                                   TokenPattern.PatternType.STRING,
                                   "+");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.SUB,
                                   "SUB",
                                   TokenPattern.PatternType.STRING,
                                   "-");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.MUL,
                                   "MUL",
                                   TokenPattern.PatternType.STRING,
                                   "*");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.DIV,
                                   "DIV",
                                   TokenPattern.PatternType.STRING,
                                   "/");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.POWER,
                                   "POWER",
                                   TokenPattern.PatternType.STRING,
                                   "^");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.MOD,
                                   "MOD",
                                   TokenPattern.PatternType.STRING,
                                   "%");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.LEFT_PAREN,
                                   "LEFT_PAREN",
                                   TokenPattern.PatternType.STRING,
                                   "(");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.RIGHT_PAREN,
                                   "RIGHT_PAREN",
                                   TokenPattern.PatternType.STRING,
                                   ")");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.LEFT_BRACE,
                                   "LEFT_BRACE",
                                   TokenPattern.PatternType.STRING,
                                   "[");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.RIGHT_BRACE,
                                   "RIGHT_BRACE",
                                   TokenPattern.PatternType.STRING,
                                   "]");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.EQ,
                                   "EQ",
                                   TokenPattern.PatternType.STRING,
                                   "=");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.LT,
                                   "LT",
                                   TokenPattern.PatternType.STRING,
                                   "<");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.GT,
                                   "GT",
                                   TokenPattern.PatternType.STRING,
                                   ">");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.LTE,
                                   "LTE",
                                   TokenPattern.PatternType.STRING,
                                   "<=");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.GTE,
                                   "GTE",
                                   TokenPattern.PatternType.STRING,
                                   ">=");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.NE,
                                   "NE",
                                   TokenPattern.PatternType.STRING,
                                   "<>");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.AND,
                                   "AND",
                                   TokenPattern.PatternType.STRING,
                                   "AND");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.OR,
                                   "OR",
                                   TokenPattern.PatternType.STRING,
                                   "OR");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.XOR,
                                   "XOR",
                                   TokenPattern.PatternType.STRING,
                                   "XOR");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.NOT,
                                   "NOT",
                                   TokenPattern.PatternType.STRING,
                                   "NOT");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.IN,
                                   "IN",
                                   TokenPattern.PatternType.STRING,
                                   "in");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.DOT,
                                   "DOT",
                                   TokenPattern.PatternType.STRING,
                                   ".");
        AddPattern(pattern);

        var seperatorpattern = new ArgumentSeparatorPattern();
        seperatorpattern.Initialize((int)ExpressionConstants.ARGUMENT_SEPARATOR,
                                   "ARGUMENT_SEPARATOR",
                                   TokenPattern.PatternType.STRING, 
                                   ",", 
                                   _expressionContext);
        AddPattern(seperatorpattern);


        pattern = new TokenPattern((int) ExpressionConstants.ARRAY_BRACES,
                                   "ARRAY_BRACES",
                                   TokenPattern.PatternType.STRING,
                                   "[]");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.LEFT_SHIFT,
                                   "LEFT_SHIFT",
                                   TokenPattern.PatternType.STRING,
                                   "<<");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.RIGHT_SHIFT,
                                   "RIGHT_SHIFT",
                                   TokenPattern.PatternType.STRING,
                                   ">>");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.WHITESPACE,
                                   "WHITESPACE",
                                   TokenPattern.PatternType.REGEXP,
                                   "\\s+");
        pattern.Ignore = true;
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.INTEGER,
                                   "INTEGER",
                                   TokenPattern.PatternType.REGEXP,
                                   "\\d+(u|l|ul|lu|f|m)?");
        AddPattern(pattern);


        var customRealPattern = new RealPattern();
        customRealPattern.Initialize((int)ExpressionConstants.REAL,
                                    "REAL",
                                    TokenPattern.PatternType.REGEXP,
                                    "\\d{0}\\{1}\\d+([eE][+-]\\d+)?(d|f|m)?",
                                    _expressionContext);
        AddPattern(customRealPattern);


        pattern = new TokenPattern((int)ExpressionConstants.STRING_LITERAL,
                                   "STRING_LITERAL",
                                   TokenPattern.PatternType.REGEXP,
                                   "\"([^\"\\r\\n\\\\]|\\\\u[0-9a-f][0-9a-f][0-9a-f][0-9a-f]|\\\\[\\\\\"'trn])*\"");
        AddPattern(pattern);

        pattern = new TokenPattern((int)ExpressionConstants.CHAR_LITERAL,
                                   "CHAR_LITERAL",
                                   TokenPattern.PatternType.REGEXP,
                                   "'([^'\\r\\n\\\\]|\\\\u[0-9a-f][0-9a-f][0-9a-f][0-9a-f]|\\\\[\\\\\"'trn])*'");

        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.TRUE,
                                   "TRUE",
                                   TokenPattern.PatternType.STRING,
                                   "True");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.FALSE,
                                   "FALSE",
                                   TokenPattern.PatternType.STRING,
                                   "False");
        AddPattern(pattern);

        pattern = new TokenPattern((int)ExpressionConstants.IDENTIFIER,
                                   "IDENTIFIER",
                                   TokenPattern.PatternType.REGEXP,
                                   "[a-z_]\\w*");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.HEX_LITERAL,
                                   "HEX_LITERAL",
                                   TokenPattern.PatternType.REGEXP,
                                   "0x[0-9a-f]+(u|l|ul|lu)?");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.NULL_LITERAL,
                                   "NULL_LITERAL",
                                   TokenPattern.PatternType.STRING,
                                   "null");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.TIMESPAN,
                                   "TIMESPAN",
                                   TokenPattern.PatternType.REGEXP,
                                   "##(\\d+\\.)?\\d\\d:\\d\\d(:\\d\\d(\\.\\d*)?)?#");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.DATETIME,
                                   "DATETIME",
                                   TokenPattern.PatternType.REGEXP,
                                   "#[^#]+#");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.IF,
                                   "IF",
                                   TokenPattern.PatternType.STRING,
                                   "if");
        AddPattern(pattern);

        pattern = new TokenPattern((int) ExpressionConstants.CAST,
                                   "CAST",
                                   TokenPattern.PatternType.STRING,
                                   "cast");
        AddPattern(pattern);
    }
}
