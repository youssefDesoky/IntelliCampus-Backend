namespace IntelliCampus.Service_Abstraction;

public class RouterNotInitializedException : Exception
{
    public RouterNotInitializedException(string courseId)
        : base($"Router not initialized for course '{courseId}'.")
    {
        CourseId = courseId;
    }

    public string CourseId { get; }
}
