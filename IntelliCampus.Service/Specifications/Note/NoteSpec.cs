using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class NoteSpec : BaseSpecifications<Note>
{
    public NoteSpec(int noteId) : base(n => n.NoteId == noteId)
    {
        AddInclude(n => n.MaterialFolder!);
    }
}
