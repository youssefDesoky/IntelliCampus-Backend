namespace IntelliCampus.Service_Abstraction;

public interface IFahimUserService
{
    string FahimBotId { get; }
    bool IsFahim(string userId);
}
