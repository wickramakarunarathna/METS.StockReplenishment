namespace METS.StockReplenishment.Core.Exceptions;

public class NotFoundException : Exception
{
	/// <summary>
	/// Initializes a new instance of NotFoundException with a specified error message.
	/// </summary>
	/// <remarks>The message is passed to the base Exception constructor.</remarks>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	public NotFoundException(string message) : base(message) { }

	/// <summary>
	/// Initializes a new NotFoundException for the specified entity and key.
	/// </summary>
	/// <param name="entityName">Name of the entity that was not found.</param>
	/// <param name="key">Key value that identifies the missing entity.</param>
	public NotFoundException(string entityName, object key)
		: base($"{entityName} with key '{key}' was not found.") { }
}