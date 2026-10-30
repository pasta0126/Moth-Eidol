using MothEidol.Core.Logs;
using MothEidol.Core.Projects;

namespace MothEidol.Core.Tests.Projects;

public sealed class ProjectDraftTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_name_is_rejected(string name)
    {
        Assert.NotNull(new ProjectDraft { Name = name }.ValidateName([]));
    }

    [Fact]
    public void Duplicate_name_is_rejected_ignoring_case_and_spaces()
    {
        Assert.NotNull(new ProjectDraft { Name = " billing " }.ValidateName(["Billing"]));
        Assert.Null(new ProjectDraft { Name = "Orders" }.ValidateName(["Billing"]));
    }

    [Fact]
    public void Folder_needs_a_pattern_and_file_does_not()
    {
        Assert.NotNull(ProjectDraft.ValidateSource(new SourceDraft(SourceKind.Folder, "/logs", " ")));
        Assert.Null(ProjectDraft.ValidateSource(new SourceDraft(SourceKind.Folder, "/logs", "*.log")));
        Assert.Null(ProjectDraft.ValidateSource(new SourceDraft(SourceKind.File, "/logs/a.log", null)));
        Assert.NotNull(ProjectDraft.ValidateSource(new SourceDraft(SourceKind.File, "  ", null)));
    }

    [Fact]
    public void Needs_at_least_one_source()
    {
        var draft = new ProjectDraft();
        Assert.NotNull(draft.ValidateSources());

        Assert.Null(draft.AddSource(new SourceDraft(SourceKind.Folder, "/logs", "*.log")));
        Assert.Null(draft.ValidateSources());
    }

    [Fact]
    public void Same_source_is_not_added_twice()
    {
        var draft = new ProjectDraft();
        Assert.Null(draft.AddSource(new SourceDraft(SourceKind.Folder, "/logs", "*.log")));
        Assert.NotNull(draft.AddSource(new SourceDraft(SourceKind.Folder, " /LOGS ", "*.LOG ")));
        Assert.Single(draft.Sources);
    }

    [Fact]
    public void File_source_drops_the_pattern()
    {
        var draft = new ProjectDraft();
        draft.AddSource(new SourceDraft(SourceKind.File, "/logs/a.log", "*.log"));
        Assert.Null(draft.Sources[0].Pattern);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(AlertSettings.MaxMinutes + 1)]
    public void Minutes_out_of_range_are_rejected(int minutes)
    {
        var draft = new ProjectDraft { Alerts = new AlertSettings(false, FrequencyMode.Immediate, minutes) };
        Assert.NotNull(draft.ValidateAlerts());
    }

    [Fact]
    public void Defaults_are_valid()
    {
        Assert.Null(new ProjectDraft().ValidateAlerts());
        Assert.Equal(AlertSettings.Default, new ProjectDraft().Alerts);
    }
}
