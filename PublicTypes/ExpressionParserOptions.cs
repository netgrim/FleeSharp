using System;
using System.Globalization;

namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionParserOptions/Class/*' />
	public class ExpressionParserOptions
	{

		private PropertyDictionary MyProperties;
		private ExpressionContext MyOwner;
		private CultureInfo MyParseCulture;

		private const NumberStyles NumberStylesConst = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

		internal ExpressionParserOptions(ExpressionContext owner)
		{
			MyOwner = owner;
			MyProperties = new PropertyDictionary();
			MyParseCulture = CultureInfo.InvariantCulture;

			this.InitializeProperties();
		}

		#region "Methods - Public"

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionParserOptions/RecreateParser/*' />
		public void RecreateParser()
		{
			MyOwner.RecreateParser();
		}

		#endregion

		#region "Methods - Internal"

		internal ExpressionParserOptions Clone()
		{
            ExpressionParserOptions copy = (ExpressionParserOptions)this.MemberwiseClone();
			copy.MyProperties = MyProperties.Clone();
			return copy;
		}

		internal double ParseDouble(string image)
		{
			return double.Parse(image, NumberStylesConst, MyParseCulture);
		}

		internal float ParseSingle(string image)
		{
			return float.Parse(image, NumberStylesConst, MyParseCulture);
		}

		internal decimal ParseDecimal(string image)
		{
			return decimal.Parse(image, NumberStylesConst, MyParseCulture);
		}
		#endregion

		#region "Methods - Private"

		private void InitializeProperties()
		{
			this.DateTimeFormat = "dd/MM/yyyy";
			this.RequireDigitsBeforeDecimalPoint = false;
			this.DecimalSeparator = ".";
			this.FunctionArgumentSeparator = ",";
		}

		#endregion

		#region "Properties - Public"

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionParserOptions/DateTimeFormat/*' />
		public string DateTimeFormat {
			get { return MyProperties.GetValue<string>("DateTimeFormat"); }
			set { MyProperties.SetValue("DateTimeFormat", value); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionParserOptions/RequireDigitsBeforeDecimalPoint/*' />
		public bool RequireDigitsBeforeDecimalPoint {
			get { return MyProperties.GetValue<bool>("RequireDigitsBeforeDecimalPoint"); }
			set { MyProperties.SetValue("RequireDigitsBeforeDecimalPoint", value); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionParserOptions/DecimalSeparator/*' />
		public string DecimalSeparator {
            get { return MyProperties.GetValue<string>("DecimalSeparator"); }
			set {
				MyProperties.SetValue("DecimalSeparator", value);
                var culture = (CultureInfo)MyParseCulture.Clone();
                culture.NumberFormat.NumberDecimalSeparator = value.ToString();
                MyParseCulture = culture;
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionParserOptions/FunctionArgumentSeparator/*' />
        public string FunctionArgumentSeparator
        {
            get { return MyProperties.GetValue<string>("FunctionArgumentSeparator"); }
			set { MyProperties.SetValue("FunctionArgumentSeparator", value); }
		}

		#endregion

	}
}
