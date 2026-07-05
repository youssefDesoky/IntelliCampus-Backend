using IntelliCampus.Service_Abstraction;

namespace IntelliCampus.Service;

public sealed class FahimUserService : IFahimUserService
{
    public const string FahimBotId = "-1";
    string IFahimUserService.FahimBotId => FahimBotId;

    public bool IsFahim(string userId)
        => !string.IsNullOrEmpty(userId) && userId == FahimBotId;
}
