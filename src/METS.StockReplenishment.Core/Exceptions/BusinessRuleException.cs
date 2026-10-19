namespace METS.StockReplenishment.Core.Exceptions;

public class BusinessRuleException : Exception
{
	/// <summary>
	/// Initializes a new instance of BusinessRuleException with the specified error message.
	/// </summary>
	/// <remarks>The message is passed to the base Exception.</remarks>
	/// <param name="message">The error message that explains the business rule violation.</param>
	public BusinessRuleException(string message) : base(message) { }
}