using System.Collections.Generic;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;

namespace FortnitePorting.Models;

/**
 * Base reference from source:
 * https://github.com/icsharpcode/SharpDevelop/blob/master/samples/AvalonEdit.Sample/BraceFoldingStrategy.cs
 */
public class JsonFoldingStrategy()
{
    public const char JSON_BRACE_OPEN = '{';
    public const char JSON_BRACE_CLOSE = '}';
    public const char JSON_BRACKET_OPEN = '[';
    public const char JSON_BRACKET_CLOSE = ']';

    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var newFoldings = CreateNewFoldings(document, out var firstErrorOffset);
        manager.UpdateFoldings(newFoldings, firstErrorOffset);
    }

    // <summary>
    // Create <see cref="NewFolding"/>s for the specified document.
    // </summary>
    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        return CreateNewFoldings(document);
    }

    // <summary>
    // Create <see cref="NewFolding"/>s for the specified document.
    // </summary>
    public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
    {
        var newFoldings = new List<NewFolding>();

        var startOffsets = new Stack<int>();
        var lastNewLineOffset = 0;
        for (var i = 0; i < document.TextLength; i++)
        {
            var c = document.GetCharAt(i);
            if (c is JSON_BRACE_OPEN or JSON_BRACKET_OPEN)
            {
                startOffsets.Push(i);
            }
            else if ((c is JSON_BRACE_CLOSE or JSON_BRACKET_CLOSE) && startOffsets.Count > 0)
            {
                var startOffset = startOffsets.Pop();
                // don't fold if opening and closing brace are on the same line
                if (startOffset < lastNewLineOffset)
                {
                    newFoldings.Add(new NewFolding(startOffset, i + 1));
                }
            }
            else if (c is '\n' or '\r')
            {
                lastNewLineOffset = i + 1;
            }
        }

        newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return newFoldings;
    }
}