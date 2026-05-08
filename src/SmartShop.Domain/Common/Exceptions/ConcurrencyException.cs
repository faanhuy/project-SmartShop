namespace SmartShop.Domain.Common.Exceptions;

public class ConcurrencyException(string message) : Exception(message);
