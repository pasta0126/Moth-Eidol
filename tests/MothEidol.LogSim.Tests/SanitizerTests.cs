namespace MothEidol.LogSim.Tests;

public class SanitizerTests
{
    private static Sanitizer Create(Dictionary<string, string>? terms = null) =>
        new(new AnonymizerConfig { Terms = terms ?? new() });

    [Fact]
    public void Same_email_gets_the_same_pseudonym_and_different_emails_differ()
    {
        var sanitizer = Create();

        var result = sanitizer.Apply("to ana.perez@corp.es, again ANA.PEREZ@corp.es and bob@corp.es");

        Assert.Equal("to user001@example.test, again user001@example.test and user002@example.test", result);
    }

    [Fact]
    public void Tax_ids_are_replaced_by_valid_looking_fakes()
    {
        var sanitizer = Create();

        var result = sanitizer.Apply("NIF 12345678Z and NIE X1234567L and file Y7654321B_CERT-1");

        Assert.DoesNotContain("12345678Z", result);
        Assert.DoesNotContain("X1234567L", result);
        Assert.DoesNotContain("Y7654321B", result);
        Assert.Matches(@"X9000001[A-Z]", result);
        Assert.Matches(@"Y9000002[A-Z]_CERT-1", result);
        Assert.Contains("NIF 90000003", result);
    }

    [Fact]
    public void Private_ips_are_mapped_but_assembly_versions_are_left_alone()
    {
        var sanitizer = Create();

        var result = sanitizer.Apply("from 10.64.4.40 via 192.168.1.7, Version=4.0.0.0");

        Assert.Equal("from 192.0.2.1 via 192.0.2.2, Version=4.0.0.0", result);
    }

    [Fact]
    public void Windows_user_names_in_paths_are_replaced()
    {
        var result = Create().Apply(@"in C:\Users\jdoe\source\repos\App\File.cs:line 5");

        Assert.Equal(@"in C:\Users\dev\source\repos\App\File.cs:line 5", result);
    }

    [Fact]
    public void Url_and_unc_hosts_are_replaced()
    {
        var sanitizer = Create();

        var result = sanitizer.Apply(@"GET https://docs.corp.es/Download and \\fileserver\share\a.pdf");

        Assert.Equal(@"GET https://host002.example.test/Download and \\server001\share\a.pdf", result);
    }

    [Fact]
    public void Configured_terms_are_replaced_case_insensitively_longest_first()
    {
        var sanitizer = Create(new() { ["Corp"] = "Acme", ["CorpGroup"] = "AcmeGroup" });

        var result = sanitizer.Apply("corpgroup runs CORP services");

        Assert.Equal("AcmeGroup runs Acme services", result);
    }

    [Fact]
    public void Config_is_rejected_when_a_replacement_contains_a_term()
    {
        var config = new AnonymizerConfig { Terms = new() { ["Corp"] = "SuperCorp" } };

        Assert.Throws<InvalidDataException>(config.Validate);
    }

    [Fact]
    public void Multi_line_structure_is_untouched_by_replacements()
    {
        var result = Create().Apply("   at App.Worker.ExecuteAsync() in C:\\src\\Worker.cs:line 60");

        Assert.StartsWith("   at ", result);
    }
}
