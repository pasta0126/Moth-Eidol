using System.Globalization;
using MothEidol.LogSim;

const string Usage = """
    MothEidol.LogSim: anonymize real logs into fixtures and simulate live log writing.

    anonymize  --input <dir> --output <dir> --config <anonymize.json>
    generate   --target <file>  --samples <dir> [--count 50] [--interval-ms 200] [--seed 1] [--mix INF=70,WRN=20,ERR=10]
    replay     --target <file>  --source <file> [--interval-ms 200]
    partial    --target <file>  --samples <dir> [--delay-ms 2000] [--seed 1]
    rollover   --target <dir> --name <prefix> --samples <dir> [--date yyyyMMdd] [--count 10] [--interval-ms 200] [--seed 1]
    truncate   --target <file>  --samples <dir> [--count 10] [--interval-ms 200] [--seed 1]
    rotate     --target <file>  --samples <dir> [--count 10] [--interval-ms 200] [--seed 1]
    disappear  --target <file>  --samples <dir> [--count 10] [--interval-ms 200] [--delay-ms 3000] [--seed 1]
    """;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };

try
{
    var args1 = new Args(args);
    var interval = TimeSpan.FromMilliseconds(args1.GetInt("interval-ms", 200));
    var delay = TimeSpan.FromMilliseconds(args1.GetInt("delay-ms", 2000));
    var count = args1.GetInt("count", 10);
    var ct = cancellation.Token;

    EntrySource Source()
    {
        var templates = TemplateLearner.Learn(args1.Required("samples"));
        var mix = args1.Get("mix") is { } text ? EntryGenerator.ParseMix(text) : null;
        return new EntryGenerator(templates, args1.GetInt("seed", 1), mix).Next;
    }

    switch (args1.Command)
    {
        case "anonymize":
        {
            var result = Anonymizer.Run(
                args1.Required("input"), args1.Required("output"), AnonymizerConfig.Load(args1.Required("config")));
            foreach (var leak in result.Leaks) Console.Error.WriteLine(leak);
            if (result.Leaks.Count > 0)
            {
                Console.Error.WriteLine($"Leak check failed ({result.Leaks.Count}); nothing was written.");
                return 1;
            }
            Console.WriteLine($"Wrote {result.FilesWritten} anonymized files to {args1.Required("output")}.");
            return 0;
        }
        case "generate":
            await Scenarios.AppendAsync(args1.Required("target"), Source(), args1.GetInt("count", 50), interval, ct);
            return 0;
        case "replay":
        {
            var entries = LogFormat.Group(LogFormat.SplitLines(File.ReadAllText(args1.Required("source")))).ToList();
            var next = 0;
            await Scenarios.AppendAsync(args1.Required("target"), _ => entries[next++].Lines, entries.Count, interval, ct);
            return 0;
        }
        case "partial":
            await Scenarios.PartialAsync(args1.Required("target"), Source(), delay, ct);
            return 0;
        case "rollover":
        {
            var date = args1.Get("date") is { } text
                ? DateOnly.ParseExact(text, "yyyyMMdd", CultureInfo.InvariantCulture)
                : DateOnly.FromDateTime(DateTime.Today);
            await Scenarios.RolloverAsync(args1.Required("target"), args1.Required("name"), date, Source(), count, interval, ct);
            return 0;
        }
        case "truncate":
            await Scenarios.TruncateAsync(args1.Required("target"), Source(), count, interval, ct);
            return 0;
        case "rotate":
            await Scenarios.RotateAsync(args1.Required("target"), Source(), count, interval, ct);
            return 0;
        case "disappear":
            await Scenarios.DisappearAsync(args1.Required("target"), Source(), count, interval, delay, ct);
            return 0;
        default:
            Console.Error.WriteLine(Usage);
            return args.Length == 0 ? 0 : 2;
    }
}
catch (Exception ex) when (ex is ArgumentException or InvalidDataException or InvalidOperationException or IOException or OperationCanceledException)
{
    Console.Error.WriteLine(ex.Message);
    return ex is OperationCanceledException ? 130 : 1;
}
