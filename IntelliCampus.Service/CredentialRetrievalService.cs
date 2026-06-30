using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Helpers;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Auth;
using IntelliCampus.Shared.Params;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class CredentialRetrievalService : ICredentialRetrievalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CredentialRetrievalService> _logger;

    public CredentialRetrievalService(IUnitOfWork unitOfWork, ILogger<CredentialRetrievalService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private IGenericRepository<User, int> Users => _unitOfWork.GetRepository<User, int>();

    public async Task<GetCredentialsResponseDto> GetCredentialsAsync(GetCredentialsDto dto, string? ipAddress, string? userAgent)
    {
        var spec = new UserByNationalIdSpec(dto.NationalId);
        var user = await Users.GetByIdAsync(spec);

        var student = await _unitOfWork.GetRepository<Student, int>().GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = user.UserId }, lightweight: true));
        if (student is null)
        {
            await AuditLogAsync(null, "get-credentials", dto.NationalId, false, "User not found or not a student", ipAddress, userAgent);
            throw new InvalidOperationException("Could not verify your details. Please check the information provided.");
        }

        var normalizedPhone = PhoneNormalizer.Normalize(dto.PhoneNumber);
        var storedPhone = PhoneNormalizer.Normalize(student.User.PhoneNumber);

        if (normalizedPhone != storedPhone || student.User.FacultyId != dto.FacultyId || (dto.Level.HasValue && student.Level != dto.Level.Value))
        {
            await AuditLogAsync(student.UserId, "get-credentials", dto.NationalId, false, "Field mismatch", ipAddress, userAgent);
            throw new InvalidOperationException("Could not verify your details. Please check the information provided.");
        }

        await AuditLogAsync(student.UserId, "get-credentials", dto.NationalId, true, null, ipAddress, userAgent);

        return new GetCredentialsResponseDto
        {
            Email = student.User.Email,
            Message = "Your password is your National ID. Please change it after logging in."
        };
    }

    private async Task AuditLogAsync(int? userId, string purpose, string nationalId, bool success, string? failureReason, string? ipAddress, string? userAgent)
    {
        var masked = nationalId.Length > 6
            ? nationalId[..3] + new string('*', nationalId.Length - 6) + nationalId[^3..]
            : nationalId;

        var log = new SecurityAuditLog
        {
            UserId = userId,
            Purpose = purpose,
            NationalIdMasked = masked,
            Success = success,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AttemptedAt = EgyptTime.Now
        };

        _unitOfWork.GetRepository<SecurityAuditLog, long>().Add(log);
        await _unitOfWork.SaveChangesAsync();
    }
}
