using Jint;
using Jint.Native;
using Jint.Runtime.Interop;
using SqlToolManagerMaui.Core.Enums.Queries;
using SqlToolManagerMaui.Core.Models.Queries;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Objet console d'une execution : chaque appel devient un message de l'onglet Messages.</summary>
internal sealed class JavaScriptConsole
{
    // Une boucle d'affichage ne doit pas saturer la memoire ni l'onglet Messages.
    private const int MaxLength = 200_000;

    private readonly List<QueryMessage> _messages = [];
    private int _length;
    private bool _isTruncated;

    public JsObject Create(Engine engine)
    {
        var console = new JsObject(engine);
        Add(engine, console, "log", QueryMessageKind.Output);
        Add(engine, console, "info", QueryMessageKind.Output);
        Add(engine, console, "debug", QueryMessageKind.Output);
        Add(engine, console, "warn", QueryMessageKind.Output);
        Add(engine, console, "error", QueryMessageKind.Error);
        return console;
    }

    public void Write(string text, QueryMessageKind kind)
    {
        var room = MaxLength - _length;
        if (room <= 0)
        {
            _isTruncated = true;
            return;
        }

        if (text.Length > room)
        {
            _isTruncated = true;
            text = text[..room];
        }

        _length += text.Length;

        // Une ligne par message, comme la console LINQ.
        _messages.AddRange(text.ReplaceLineEndings("\n").Split('\n').Select(l => new QueryMessage(l, kind)));
    }

    public IReadOnlyList<QueryMessage> ToMessages()
    {
        if (!_isTruncated)
            return _messages;

        return [.. _messages, new QueryMessage($"Sortie console tronquée à {MaxLength:N0} caractères.", QueryMessageKind.Error)];
    }

    private void Add(Engine engine, JsObject console, string name, QueryMessageKind kind)
        => console.CreateDataProperty(name, new ClrFunction(engine, name, (_, arguments) =>
        {
            Write(string.Join(' ', arguments.Select(a => JavaScriptValueFormatter.ToConsole(engine, a))), kind);
            return JsValue.Undefined;
        }));
}
