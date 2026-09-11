namespace SQL_prototipo.TSql;

/// <summary>
/// A table referenced in a query's FROM / JOIN clause, together with its
/// (optional) schema and alias. Produced by <see cref="TSqlTableCollector"/> and
/// used to scope column completions to the tables actually in the query.
/// </summary>
public sealed record TableReference(string Schema, string Name, string? Alias);
