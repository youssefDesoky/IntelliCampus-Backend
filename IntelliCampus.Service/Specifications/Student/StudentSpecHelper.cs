using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;
using System.Linq.Expressions;

namespace IntelliCampus.Service.Specifications
{
    internal static class StudentSpecHelper
    {
        public static Expression<Func<Student, bool>> GetStudentCriteria(StudentQueryParams queryParams)
        {
            StudentType? parsedStatus = null;
            if (!string.IsNullOrEmpty(queryParams.Status) && Enum.TryParse<StudentType>(queryParams.Status, ignoreCase: true, out var st))
                parsedStatus = st;

            return s =>
                (!queryParams.DepartmentId.HasValue || s.DepartmentId == queryParams.DepartmentId.Value) &&
                (!queryParams.FacultyId.HasValue || s.User.FacultyId == queryParams.FacultyId.Value) &&
                (!queryParams.Level.HasValue || s.Level == queryParams.Level.Value) &&
                (string.IsNullOrEmpty(queryParams.Search) || s.User.FullName.Contains(queryParams.Search)) &&
                (!parsedStatus.HasValue || s.StudentType == parsedStatus.Value) &&
                (!queryParams.IsOnProbation.HasValue ||
                    (queryParams.IsOnProbation.Value
                        ? (s.Gpa > 0
                            && s.Bylaw != null
                            && s.Bylaw.Settings.ProbationThreshold.HasValue
                            && s.Gpa < (double)s.Bylaw.Settings.ProbationThreshold.Value)
                        : !(s.Gpa > 0
                            && s.Bylaw != null
                            && s.Bylaw.Settings.ProbationThreshold.HasValue
                            && s.Gpa < (double)s.Bylaw.Settings.ProbationThreshold.Value)));
        }
    }
}
