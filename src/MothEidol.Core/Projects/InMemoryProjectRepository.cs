namespace MothEidol.Core.Projects;

/// <summary>Non-persistent projects, used when the database cannot be used and in tests.</summary>
public sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly object _gate = new();
    private readonly List<Project> _projects = [];
    private long _nextId = 1;

    public IReadOnlyList<Project> List()
    {
        lock (_gate) return [.. _projects];
    }

    public Project Add(string name)
    {
        lock (_gate)
        {
            var project = new Project(_nextId++, name, Enabled: true, AlertSettings.Default);
            _projects.Add(project);
            return project;
        }
    }

    public void Update(Project project)
    {
        lock (_gate)
        {
            var index = _projects.FindIndex(p => p.Id == project.Id);
            if (index >= 0) _projects[index] = project;
        }
    }

    public void Remove(long id)
    {
        lock (_gate) _projects.RemoveAll(p => p.Id == id);
    }
}
