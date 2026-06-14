namespace IntelliCampus.Service_Abstraction;

public class ConflictGraph
{
    private readonly Dictionary<int, HashSet<int>> _adjacency = [];

    public void AddEdge(int courseA, int courseB)
    {
        if (!_adjacency.ContainsKey(courseA))
            _adjacency[courseA] = [];
        if (!_adjacency.ContainsKey(courseB))
            _adjacency[courseB] = [];
        _adjacency[courseA].Add(courseB);
        _adjacency[courseB].Add(courseA);
    }

    public bool HasConflict(int courseA, int courseB) =>
        _adjacency.TryGetValue(courseA, out var neighbors) && neighbors.Contains(courseB);

    public HashSet<int> GetConflicts(int courseId) =>
        _adjacency.TryGetValue(courseId, out var set) ? set : [];

    public int Degree(int courseId) => GetConflicts(courseId).Count;

    public Dictionary<int, HashSet<int>> Adjacency => _adjacency;

    public List<int> GetSortedByDegreeDesc() =>
        _adjacency.Keys.OrderByDescending(c => Degree(c)).ThenBy(c => c).ToList();
}
