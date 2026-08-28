namespace Ob.Sqlite;

/// <summary>The kind of a completion suggestion, used to pick its icon.</summary>
public enum CompletionKind
{
    Keyword,
    Table,
    Column
}

/// <summary>A single suggestion shown in the autocomplete popup.</summary>
public sealed class CompletionItem
{
    public CompletionItem(string text, CompletionKind kind)
    {
        Text = text;
        Kind = kind;
    }

    public string Text { get; }
    public CompletionKind Kind { get; }

    public override string ToString() => Text;
}
