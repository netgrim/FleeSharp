using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;

namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/Class/*' />
	public sealed class ExpressionOptions
	{

		private PropertyDictionary MyProperties;
		private Type MyOwnerType;

		private ExpressionContext MyOwner;
		internal event EventHandler CaseSensitiveChanged;

		internal ExpressionOptions(ExpressionContext owner)
		{
			MyOwner = owner;
			MyProperties = new PropertyDictionary();

			this.InitializeProperties();
		}

		#region "Methods - Private"

		private void InitializeProperties()
		{
			this.StringComparison = System.StringComparison.Ordinal;
			this.OwnerMemberAccess = BindingFlags.Public;

			MyProperties.SetToDefault<bool>("CaseSensitive");
			MyProperties.SetToDefault<bool>("Checked");
			MyProperties.SetToDefault<bool>("EmitToAssembly");
			MyProperties.SetToDefault<Type>("ResultType");
			MyProperties.SetToDefault<bool>("IsGeneric");
			MyProperties.SetToDefault<bool>("IntegersAsDoubles");
			MyProperties.SetValue("ParseCulture", CultureInfo.CurrentCulture);
			this.SetParseCulture(this.ParseCulture);
			MyProperties.SetValue("RealLiteralDataType", RealLiteralDataType.Double);
		}

		private void SetParseCulture(CultureInfo ci)
		{
			ExpressionParserOptions po = MyOwner.ParserOptions;
			po.DecimalSeparator = ci.NumberFormat.NumberDecimalSeparator;
			po.FunctionArgumentSeparator = ci.TextInfo.ListSeparator;
			po.DateTimeFormat = ci.DateTimeFormat.ShortDatePattern;
		}

		#endregion

		#region "Methods - Internal"

		internal ExpressionOptions Clone()
		{
			ExpressionOptions clonedOptions = (ExpressionOptions)this.MemberwiseClone();
			clonedOptions.MyProperties = MyProperties.Clone();
			return clonedOptions;
		}

		internal bool IsOwnerType(Type t)
		{
			return this.MyOwnerType.IsAssignableFrom(t);
		}

		internal void SetOwnerType(Type ownerType)
		{
			MyOwnerType = ownerType;
		}

		#endregion

		#region "Properties - Public"
		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/ResultType/*' />
		public Type ResultType {
			get { return MyProperties.GetValue<Type>("ResultType"); }
			set {
				Utility.AssertNotNull(value, "value");
				MyProperties.SetValue("ResultType", value);
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/Checked/*' />
		public bool Checked {
			get { return MyProperties.GetValue<bool>("Checked"); }
			set { MyProperties.SetValue("Checked", value); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/StringComparison/*' />
		public StringComparison StringComparison {
			get { return MyProperties.GetValue<StringComparison>("StringComparison"); }
			set { MyProperties.SetValue("StringComparison", value); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/EmitToAssembly/*' />
		public bool EmitToAssembly {
			get { return MyProperties.GetValue<bool>("EmitToAssembly"); }
			set { MyProperties.SetValue("EmitToAssembly", value); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/OwnerMemberAccess/*' />
		public BindingFlags OwnerMemberAccess {
			get { return MyProperties.GetValue<BindingFlags>("OwnerMemberAccess"); }
			set { MyProperties.SetValue("OwnerMemberAccess", value); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/CaseSensitive/*' />
		public bool CaseSensitive {
			get { return MyProperties.GetValue<bool>("CaseSensitive"); }
			set {
				if (this.CaseSensitive != value) {
					MyProperties.SetValue("CaseSensitive", value);
					if (CaseSensitiveChanged != null) {
						CaseSensitiveChanged(this, EventArgs.Empty);
					}
				}
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/IntegersAsDoubles/*' />
		public bool IntegersAsDoubles {
			get { return MyProperties.GetValue<bool>("IntegersAsDoubles"); }
			set { MyProperties.SetValue("IntegersAsDoubles", value); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/ParseCulture/*' />
		public CultureInfo ParseCulture {
			get { return MyProperties.GetValue<CultureInfo>("ParseCulture"); }
			set {
				Utility.AssertNotNull(value, "ParseCulture");
				if ((value.LCID != this.ParseCulture.LCID)) {
					MyProperties.SetValue("ParseCulture", value);
					this.SetParseCulture(value);
					MyOwner.ParserOptions.RecreateParser();
				}
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionOptions/RealLiteralDataType/*' />
		public RealLiteralDataType RealLiteralDataType {
			get { return MyProperties.GetValue<RealLiteralDataType>("RealLiteralDataType"); }
			set { MyProperties.SetValue("RealLiteralDataType", value); }
		}
		#endregion

		#region "Properties - Non Public"
		internal IEqualityComparer<string> StringComparer {
			get {
				if (this.CaseSensitive == true) {
					return System.StringComparer.Ordinal;
				} else {
					return System.StringComparer.OrdinalIgnoreCase;
				}
			}
		}

		internal MemberFilter MemberFilter {
			get {
				if (this.CaseSensitive == true) {
					return Type.FilterName;
				} else {
					return Type.FilterNameIgnoreCase;
				}
			}
		}

		internal StringComparison MemberStringComparison {
			get {
				if (this.CaseSensitive == true) {
					return System.StringComparison.Ordinal;
				} else {
					return System.StringComparison.OrdinalIgnoreCase;
				}
			}
		}

		internal Type OwnerType {
			get { return MyOwnerType; }
		}

		internal bool IsGeneric {
			get { return MyProperties.GetValue<bool>("IsGeneric"); }
			set { MyProperties.SetValue("IsGeneric", value); }
		}
		#endregion
	}
}
