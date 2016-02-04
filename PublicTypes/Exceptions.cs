using System;

namespace Ciloci.Flee
{
	/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/Class/*' />	
	public enum CompileExceptionReason
	{
        Unknown = -1,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/SyntaxError/*' />	
		SyntaxError,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/ConstantOverflow/*' />	
		ConstantOverflow,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/TypeMismatch/*' />	
		TypeMismatch,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/UndefinedName/*' />	
		UndefinedName,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/FunctionHasNoReturnValue/*' />	
		FunctionHasNoReturnValue,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/InvalidExplicitCast/*' />	
		InvalidExplicitCast,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/AmbiguousMatch/*' />	
		AmbiguousMatch,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/AccessDenied/*' />	
		AccessDenied,
		/// <include file='Resources/DocComments.xml' path='DocComments/CompileExceptionReason/InvalidFormat/*' />	
		InvalidFormat
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionCompileException/Class/*' />	
	[Serializable()]
	public sealed class ExpressionCompileException : Exception
	{


		private CompileExceptionReason MyReason;
		internal ExpressionCompileException(string message, CompileExceptionReason reason) : base(message)
		{
			MyReason = reason;
		}

		internal ExpressionCompileException(PerCederberg.Grammatica.Runtime.ParserLogException parseException) : base(string.Empty, parseException)
		{
			MyReason = CompileExceptionReason.SyntaxError;
		}

		private ExpressionCompileException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{
			MyReason = (CompileExceptionReason)info.GetInt32("Reason");
		}
        
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Reason", Convert.ToInt32(MyReason));
		}

		public override string Message {
			get {
				if (MyReason == CompileExceptionReason.SyntaxError) {
					Exception innerEx = this.InnerException;
					string msg = string.Format("{0}: {1}", Utility.GetCompileErrorMessage(CompileErrorResourceKeys.SyntaxError), innerEx.Message);
					return msg;
				} else {
					return base.Message;
				}
			}
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/ExpressionCompileException/Reason/*' />	
		public CompileExceptionReason Reason {
			get { return MyReason; }
		}
	}
}
