namespace IntelliCampus.Service_Abstraction.Exceptions;

public abstract class NotFoundException(string message) : Exception(message)
{
}
