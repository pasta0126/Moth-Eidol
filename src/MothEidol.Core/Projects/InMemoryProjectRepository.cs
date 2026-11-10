namespace MothEidol.Core.Projects;

/// <summary>Non-persistent projects, used when the database cannot be used and in tests.</summary>
public sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly object _gate = new();
    private readonly List<Project> _projects = [];
    private long _nextId = 1;

    public IReadOnlyList<Project> List()
    {
        lock (_gate) return [.. Ordered()];
    }

    public Project Add(string name)
    {
        lock (_gate)
        {
            var position = _projects.Count == 0 ? 1 : _projects.Max(p => p.Position) + 1;
            var project = new Project(_nextId++, name, Enabled: true, AlertSettings.Default, Position: position);
            _projects.Add(project);
            return project;
        }
    }

    public void Update(Project project)
    {
        lock (_gate)
        {
            var index = _projects.FindIndex(p => p.Id == project.Id);
            if (index >= 0) _projects[index] = project with { Position = _projects[index].Position };
        }
    }

    public void Remove(long id)
    {
        lock (_gate) _projects.RemoveAll(p => p.Id == id);
    }

    public void Reorder(IReadOnlyList<long> orderedIds)
    {
        lock (_gate)
        {
            var current = Ordered().ToList();
            var order = orderedIds.Distinct().Select(id => current.FirstOrDefault(p => p.Id == id))
                .OfType<Project>()
                .Concat(current.Where(p => !orderedIds.Contains(p.Id)))
                .ToList();
            for (var i = 0; i < order.Count; i++)
            {
                var index = _projects.FindIndex(p => p.Id == order[i].Id);
                _projects[index] = _projects[index] with { Position = i + 1 };
            }
        }
    }

    private IEnumerable<Project> Ordered() => _projects.OrderBy(p => p.Position).ThenBy(p => p.Id);
}
