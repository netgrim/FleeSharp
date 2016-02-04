
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

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

	internal class FleeExpressionAnalyzer : ExpressionAnalyzer
	{

		private IServiceProvider MyServices;
		private Regex MyUnicodeEscapeRegex;
		private Regex MyRegularEscapeRegex;

		private bool MyInUnaryNegate;

		internal FleeExpressionAnalyzer()
		{
			MyUnicodeEscapeRegex = new Regex("\\\\u[0-9a-f]{4}", RegexOptions.IgnoreCase);
			MyRegularEscapeRegex = new Regex("\\\\[\\\\\"'trn]", RegexOptions.IgnoreCase);
		}

		public void SetServices(IServiceProvider services)
		{
			MyServices = services;
		}

		public override void Reset()
		{
			MyServices = null;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddFirstChildValue(node);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitExpressionGroup(PerCederberg.Grammatica.Runtime.Production node)
		{
			node.AddValues(this.GetChildValues(node));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitXorExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddBinaryOp(node, typeof(XorElement));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitOrExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddBinaryOp(node, typeof(AndOrElement));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitAndExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddBinaryOp(node, typeof(AndOrElement));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitNotExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddUnaryOp(node, typeof(NotElement));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitCompareExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddBinaryOp(node, typeof(CompareElement));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitShiftExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddBinaryOp(node, typeof(ShiftElement));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitAdditiveExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddBinaryOp(node, typeof(ArithmeticElement));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitMultiplicativeExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddBinaryOp(node, typeof(ArithmeticElement));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitPowerExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddBinaryOp(node, typeof(ArithmeticElement));
			return node;
		}

        // Try to fold a negated constant int32.  We have to do this so that parsing int32.MinValue will work
		public override PerCederberg.Grammatica.Runtime.Node ExitNegateExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			IList childValues = this.GetChildValues(node);

			// Get last child
            ExpressionElement childElement = (ExpressionElement)childValues[childValues.Count - 1];

			// Is it an signed integer constant?
			if (object.ReferenceEquals(childElement.GetType(), typeof(Int32LiteralElement)) & childValues.Count == 2) {
				((Int32LiteralElement)childElement).Negate();
				// Add it directly instead of the negate element since it will already be negated
				node.AddValue(childElement);
			} else if (object.ReferenceEquals(childElement.GetType(), typeof(Int64LiteralElement)) & childValues.Count == 2) {
				((Int64LiteralElement)childElement).Negate();
				// Add it directly instead of the negate element since it will already be negated
				node.AddValue(childElement);
			} else {
				// No so just add a regular negate
				this.AddUnaryOp(node, typeof(NegateElement));
			}

			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitMemberExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			var childValues = this.GetChildValues(node);
			object first = childValues[0];

			if (childValues.Count == 1 && !(first is MemberElement)) {
				node.AddValue(first);
			} else {
				var list = new InvocationListElement(childValues, MyServices);
				node.AddValue(list);
			}

			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitIndexExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			var childValues = this.GetChildValues(node);
			var args = new ArgumentList(childValues);
			var e = new IndexerElement(args);
			node.AddValue(e);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitMemberAccessExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			node.AddValue(node.GetChildAt(1).GetValue(0));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitSpecialFunctionExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddFirstChildValue(node);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitIfExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			var childValues = this.GetChildValues(node);
            var op = new ConditionalElement((ExpressionElement)childValues[0], (ExpressionElement)childValues[1], (ExpressionElement)childValues[2]);
			node.AddValue(op);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitInExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			var childValues = this.GetChildValues(node);

			if (childValues.Count == 1) {
				this.AddFirstChildValue(node);
				return node;
			}

            var operand = (ExpressionElement)childValues[0];
			childValues.RemoveAt(0);

			object second = childValues[0];
			InElement op = null;

			if ((second) is IList) {
				op = new InElement(operand, (IList)second);
			} else {
				var il = new InvocationListElement(childValues, MyServices);
				op = new InElement(operand, il);
			}

			node.AddValue(op);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitInTargetExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddFirstChildValue(node);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitInListTargetExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			IList childValues = this.GetChildValues(node);
			node.AddValue(childValues);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitCastExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			var childValues = this.GetChildValues(node);
			string[] destTypeParts = (string[])childValues[1];
			bool isArray = (bool)childValues[2];
            var op = new CastElement((ExpressionElement)childValues[0], destTypeParts, isArray, MyServices);
			node.AddValue(op);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitCastTypeExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			var childValues = this.GetChildValues(node);
			var parts = new List<string>();

			foreach (string part in childValues) {
				parts.Add(part);
			}

			bool isArray = false;

			if (parts[parts.Count - 1] == "[]") {
				isArray = true;
				parts.RemoveAt(parts.Count - 1);
			}

			node.AddValue(parts.ToArray());
			node.AddValue(isArray);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitMemberFunctionExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddFirstChildValue(node);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitFieldPropertyExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			string name = (string)node.GetChildAt(0).GetValue(0);
			IdentifierElement elem = new IdentifierElement(name);
			node.AddValue(elem);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitFunctionCallExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			var childValues = this.GetChildValues(node);
			string name = (string)childValues[0];
			childValues.RemoveAt(0);
			var args = new ArgumentList(childValues);
			var funcCall = new FunctionCallElement(name, args);
			node.AddValue(funcCall);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitArgumentList(PerCederberg.Grammatica.Runtime.Production node)
		{
			var childValues = this.GetChildValues(node);
			node.AddValues(childValues);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitBasicExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddFirstChildValue(node);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitLiteralExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddFirstChildValue(node);
			return node;
		}

		private void AddFirstChildValue(PerCederberg.Grammatica.Runtime.Production node)
		{
			node.AddValue(this.GetChildAt(node, 0).Values[0]);
		}

		private void AddUnaryOp(PerCederberg.Grammatica.Runtime.Production node, Type elementType)
		{
			var childValues = this.GetChildValues(node);

			if (childValues.Count == 2) {
                var element = (UnaryElement)Activator.CreateInstance(elementType);
				element.SetChild((ExpressionElement)childValues[1]);
				node.AddValue(element);
			} else {
				node.AddValue(childValues[0]);
			}
		}

		private void AddBinaryOp(PerCederberg.Grammatica.Runtime.Production node, Type elementType)
		{
			IList childValues = this.GetChildValues(node);

			if (childValues.Count > 1) {
				BinaryExpressionElement e = BinaryExpressionElement.CreateElement(childValues, elementType);
				node.AddValue(e);
			} else if (childValues.Count == 1) {
				node.AddValue(childValues[0]);
			} else {
				Debug.Assert(false, "wrong number of chilren");
			}
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitReal(PerCederberg.Grammatica.Runtime.Token node)
		{
			string image = node.Image;
			var element = RealLiteralElement.Create(image, MyServices);

			node.AddValue(element);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitInteger(PerCederberg.Grammatica.Runtime.Token node)
		{
			var element = IntegralLiteralElement.Create(node.Image, false, MyInUnaryNegate, MyServices);
			node.AddValue(element);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitHexLiteral(PerCederberg.Grammatica.Runtime.Token node)
		{
			var element = IntegralLiteralElement.Create(node.Image, true, MyInUnaryNegate, MyServices);
			node.AddValue(element);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitBooleanLiteralExpression(PerCederberg.Grammatica.Runtime.Production node)
		{
			this.AddFirstChildValue(node);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitTrue(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(new BooleanLiteralElement(true));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitFalse(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(new BooleanLiteralElement(false));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitStringLiteral(PerCederberg.Grammatica.Runtime.Token node)
		{
			string s = this.DoEscapes(node.Image);
			var element = new StringLiteralElement(s);
			node.AddValue(element);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitCharLiteral(PerCederberg.Grammatica.Runtime.Token node)
		{
			string s = this.DoEscapes(node.Image);
			node.AddValue(new CharLiteralElement(s[0]));
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitDatetime(PerCederberg.Grammatica.Runtime.Token node)
		{
            var context = (ExpressionContext)MyServices.GetService(typeof(ExpressionContext));
			string image = node.Image.Substring(1, node.Image.Length - 2);
			var element = new DateTimeLiteralElement(image, context);
			node.AddValue(element);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitTimespan(PerCederberg.Grammatica.Runtime.Token node)
		{
			string image = node.Image.Substring(2, node.Image.Length - 3);
			var element = new TimeSpanLiteralElement(image);
			node.AddValue(element);
			return node;
		}

		private string DoEscapes(string image)
		{
			// Remove outer quotes
			image = image.Substring(1, image.Length - 2);
			image = MyUnicodeEscapeRegex.Replace(image, UnicodeEscapeMatcher);
			image = MyRegularEscapeRegex.Replace(image, RegularEscapeMatcher);
			return image;
		}

		private string RegularEscapeMatcher(Match m)
		{
			string s = m.Value;
			// Remove leading \
			s = s.Remove(0, 1);

			switch (s) {
				case "\\":
				case "\"":
				case "'":
					return s;
				case "t":
				case "T":
					return Convert.ToChar(9).ToString();
				case "n":
				case "N":
					return Convert.ToChar(10).ToString();
				case "r":
				case "R":
					return Convert.ToChar(13).ToString();
				default:
					Debug.Assert(false, "Unrecognized escape sequence");
					return null;
			}
		}

		private string UnicodeEscapeMatcher(Match m)
		{
			string s = m.Value;
			// Remove \u
			s = s.Remove(0, 2);
			int code = int.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier);
			char c = Convert.ToChar(code);
			return c.ToString();
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitIdentifier(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(node.Image);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitNullLiteral(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(new NullLiteralElement());
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitArrayBraces(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue("[]");
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitAdd(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(BinaryArithmeticOperation.Add);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitSub(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(BinaryArithmeticOperation.Subtract);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitMul(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(BinaryArithmeticOperation.Multiply);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitDiv(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(BinaryArithmeticOperation.Divide);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitMod(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(BinaryArithmeticOperation.Mod);
			return node;
		}

        public override PerCederberg.Grammatica.Runtime.Node ExitPower(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(BinaryArithmeticOperation.Power);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitEq(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(LogicalCompareOperation.Equal);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitNe(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(LogicalCompareOperation.NotEqual);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitLt(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(LogicalCompareOperation.LessThan);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitGt(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(LogicalCompareOperation.GreaterThan);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitLte(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(LogicalCompareOperation.LessThanOrEqual);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitGte(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(LogicalCompareOperation.GreaterThanOrEqual);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitAnd(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(AndOrOperation.And);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitOr(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(AndOrOperation.Or);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitXor(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue("Xor");
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitNot(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(string.Empty);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitLeftShift(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(ShiftOperation.LeftShift);
			return node;
		}

		public override PerCederberg.Grammatica.Runtime.Node ExitRightShift(PerCederberg.Grammatica.Runtime.Token node)
		{
			node.AddValue(ShiftOperation.RightShift);
			return node;
		}

		public override void Child(PerCederberg.Grammatica.Runtime.Production node, PerCederberg.Grammatica.Runtime.Node child)
		{
			base.Child(node, child);
            MyInUnaryNegate = (ExpressionConstants)node.Id == ExpressionConstants.NEGATE_EXPRESSION & (ExpressionConstants)child.Id == ExpressionConstants.SUB;
		}
	}
}
