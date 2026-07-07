using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class BylawSpec : BaseSpecifications<Bylaw>
    {
        public BylawSpec()
        {
            AddInclude(b => b.UploadedBy!);
            AddInclude(b => b.Students!);
            AddInclude(b => b.Faculty!);
            EnableSplitQuery();
        }

        public BylawSpec(int bylawId)
            : base(b => b.BylawId == bylawId)
        {
            AddInclude(b => b.UploadedBy!);
            AddInclude(b => b.Students!);
            AddInclude(b => b.Faculty!);
            AddInclude("BylawCourses");
            AddInclude("BylawCourses.Course");
            AddInclude("BylawCourses.Prerequisites");
            AddInclude("BylawCourses.PrerequisiteFor");
            AddInclude("ElectiveBuckets");
            AddInclude("ElectiveBuckets.ElectiveBucketCourses");
            AddInclude("ElectiveBuckets.ElectiveBucketCourses.Course");
            AddInclude("ElectiveBuckets.Department");
            EnableSplitQuery();
        }

        // Lightweight — no includes (for operations needing only scalars/owned entities)
        public BylawSpec(int bylawId, bool lightweight)
            : base(b => b.BylawId == bylawId) { }

        public BylawSpec(BylawQueryParams queryParams)
            : base(b =>
                (string.IsNullOrEmpty(queryParams.Type)
                    || b.Type == Enum.Parse<BylawType>(queryParams.Type, true)) &&
                (string.IsNullOrEmpty(queryParams.Search)
                    || b.Name.Contains(queryParams.Search)
                    || (b.NameAr != null && b.NameAr.Contains(queryParams.Search))) &&
                (!queryParams.FacultyId.HasValue || b.FacultyId == queryParams.FacultyId.Value))
        {
            AddInclude(b => b.UploadedBy!);
            AddInclude(b => b.Students!);
            AddInclude(b => b.Faculty!);
            EnableSplitQuery();
            AddOrderBy(b => b.BylawId);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }
    }
}
