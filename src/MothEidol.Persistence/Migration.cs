namespace MothEidol.Persistence;

/// <summary>A numbered schema migration. Versions must be consecutive starting at 1.</summary>
public sealed record Migration(int Version, string Sql);
