using System;
using Ciloci.Flee.CalcEngine;
using System.IO;

namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/Class/*' />
	public sealed class ExpressionContext
	{

		#region "Fields"

		private PropertyDictionary MyProperties;

		private object MySyncRoot = new object();
		/// <remarks>Keep variables as a field to make access fast</remarks>

		private VariableCollection MyVariables;
		#endregion

		#region "Constructor"

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/New1/*' />
		public ExpressionContext() : this(DefaultExpressionOwner.Instance)
		{
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/New2/*' />
		public ExpressionContext(object expressionOwner)
		{
			Utility.AssertNotNull(expressionOwner, "expressionOwner");
			MyProperties = new PropertyDictionary();

			MyProperties.SetValue("CalculationEngine", null);
			MyProperties.SetValue("CalcEngineExpressionName", null);
			MyProperties.SetValue("IdentifierParser", null);

			MyProperties.SetValue("ExpressionOwner", expressionOwner);

			MyProperties.SetValue("ParserOptions", new ExpressionParserOptions(this));

			MyProperties.SetValue("Options", new ExpressionOptions(this));
			MyProperties.SetValue("Imports", new ExpressionImports());
			this.Imports.SetContext(this);
			MyVariables = new VariableCollection(this);

			MyProperties.SetToDefault<bool>("NoClone");

			this.RecreateParser();
		}

		#endregion

		#region "Methods - Private"

		private void AssertTypeIsAccessibleInternal(Type t)
		{
			bool isPublic = t.IsPublic;

			if (t.IsNested == true) {
				isPublic = t.IsNestedPublic;
			}

			bool isSameModuleAsOwner = object.ReferenceEquals(t.Module, this.ExpressionOwner.GetType().Module);

			// Public types are always accessible.  Otherwise they have to be in the same module as the owner
			bool isAccessible = isPublic | isSameModuleAsOwner;

			if (isAccessible == false) {
				string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.TypeNotAccessibleToExpression, t.Name);
				throw new ArgumentException(msg);
			}
		}

		private void AssertNestedTypeIsAccessible(Type t)
		{
			while ((t != null)) {
				AssertTypeIsAccessibleInternal(t);
				t = t.DeclaringType;
			}
		}
		#endregion

		#region "Methods - Internal"

		internal ExpressionContext CloneInternal(bool cloneVariables)
		{
            var context = (ExpressionContext)this.MemberwiseClone();
			context.MyProperties = MyProperties.Clone();
			context.MyProperties.SetValue("Options", this.Options.Clone());
			context.MyProperties.SetValue("ParserOptions", this.ParserOptions.Clone());
			context.MyProperties.SetValue("Imports", this.Imports.Clone());
			context.Imports.SetContext(context);

			if (cloneVariables == true) {
				context.MyVariables = new VariableCollection(this);
				this.Variables.Copy(context.MyVariables);
			}

			return context;
		}

		internal void AssertTypeIsAccessible(Type t)
		{
			if (t.IsNested == true) {
				AssertNestedTypeIsAccessible(t);
			} else {
				AssertTypeIsAccessibleInternal(t);
			}
		}

        // Does the actual parsing of an expression.  Thead-safe.
		internal ExpressionElement Parse(string expression, IServiceProvider services)
		{
			lock (MySyncRoot) {
				var sr = new StringReader(expression);
				var parser = this.Parser;
                parser.Reset(sr);

                var analyzer = parser.Analyzer as FleeExpressionAnalyzer;
				analyzer.SetServices(services);
				var rootNode = DoParse();
				analyzer.Reset();
                var topElement = rootNode.Values[0] as ExpressionElement;
				return topElement;
			}
		}

		internal void RecreateParser()
		{
			lock (MySyncRoot) {
				var analyzer = new FleeExpressionAnalyzer();
				var parser = new ExpressionParser(System.IO.StringReader.Null, analyzer, this);
				MyProperties.SetValue("ExpressionParser", parser);
			}
		}

		internal PerCederberg.Grammatica.Runtime.Node DoParse()
		{
			try {
				return this.Parser.Parse();
			} catch (PerCederberg.Grammatica.Runtime.ParserLogException ex) {
				// Syntax error; wrap it in our exception and rethrow
				throw new ExpressionCompileException(ex);
			}
		}

		internal void SetCalcEngine(CalculationEngine engine, string calcEngineExpressionName)
		{
			MyProperties.SetValue("CalculationEngine", engine);
			MyProperties.SetValue("CalcEngineExpressionName", calcEngineExpressionName);
		}

		internal IdentifierAnalyzer ParseIdentifiers(string expression)
		{
			var parser = this.IdentifierParser;
			var sr = new StringReader(expression);
            parser.Reset(sr);

            var analyzer = parser.Analyzer as IdentifierAnalyzer;
			analyzer.Reset();

			parser.Parse();

            return analyzer;
		}
		#endregion

		#region "Methods - Public"

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/Clone/*' />
		public ExpressionContext Clone()
		{
			return this.CloneInternal(true);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/CompileDynamic/*' />
		public IDynamicExpression CompileDynamic(string expression)
		{
			return new Expression<object>(expression, this, false);
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/CompileGeneric/*' />
		public IGenericExpression<TResultType> CompileGeneric<TResultType>(string expression)
		{
			return new Expression<TResultType>(expression, this, true);
		}

		#endregion

		#region "Properties - Private"

		private ExpressionParser IdentifierParser {
			get {
				var parser = MyProperties.GetValue<ExpressionParser>("IdentifierParser");

				if (parser == null) {
					var analyzer = new IdentifierAnalyzer();
					parser = new ExpressionParser(System.IO.StringReader.Null, analyzer, this);
					MyProperties.SetValue("IdentifierParser", parser);
				}

				return parser;
			}
		}

		#endregion

		#region "Properties - Internal"

		internal bool NoClone {
			get { return MyProperties.GetValue<bool>("NoClone"); }
			set { MyProperties.SetValue("NoClone", value); }
		}

		internal object ExpressionOwner {
			get { return MyProperties.GetValue<object>("ExpressionOwner"); }
		}

		internal string CalcEngineExpressionName {
			get { return MyProperties.GetValue<string>("CalcEngineExpressionName"); }
		}

		internal ExpressionParser Parser {
			get { return MyProperties.GetValue<ExpressionParser>("ExpressionParser"); }
		}
		#endregion

		#region "Properties - Public"

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/Options/*' />
		public ExpressionOptions Options {
			get { return MyProperties.GetValue<ExpressionOptions>("Options"); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/Imports/*' />
		public ExpressionImports Imports {
			get { return MyProperties.GetValue<ExpressionImports>("Imports"); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/Variables/*' />
		public VariableCollection Variables {
			get { return MyVariables; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/CalculationEngine/*' />
		public CalculationEngine CalculationEngine {
			get { return MyProperties.GetValue<CalculationEngine>("CalculationEngine"); }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionContext/ParserOptions/*' />
		public ExpressionParserOptions ParserOptions {
			get { return MyProperties.GetValue<ExpressionParserOptions>("ParserOptions"); }
		}

		#endregion
	}
}
