using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class NoteSpec : BaseSpecifications<Note>
{
    public NoteSpec(int noteId) : base(n => n.NoteId == noteId)
    {
        AddInclude(n => n.MaterialFolder!);
        AddInclude(n => n.Course!);
        AddInclude(n => n.NoteSummary!);
    }

    public NoteSpec(int courseId, bool byCourse) : base(n => n.CourseId == courseId)
    {
    }
}
