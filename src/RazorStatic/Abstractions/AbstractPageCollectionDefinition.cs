using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Abstractions;

public abstract class AbstractPageCollectionDefinition : IPageCollectionDefinition
{
    public abstract string RootPath { get; }

    public abstract IAsyncEnumerable<RenderedResult> RenderComponentsAsync(
        Type pageType,
        CancellationToken cancellationToken);

    public abstract IAsyncEnumerable<RenderedResult> RenderGroupComponentsAsync(
        Type pageType,
        CancellationToken cancellationToken);

    protected static async Task<(string? FrontMatter, string? Markdown)> GetFileContentAsync(
        TextReader reader,
        CancellationToken cancellationToken)
    {
        var line = await reader.ReadLineAsync(cancellationToken);
        if (line is not "---")
        {
            var rest = await reader.ReadToEndAsync(cancellationToken);
            return (string.Empty, (line + Environment.NewLine + rest).Trim());
        }

        var frontMatterBuilder = new StringBuilder();
        while (true)
        {
            if (reader.Peek() == -1)
                break;

            line = await reader.ReadLineAsync(cancellationToken);
            if (line is "---")
            {
                break;
            }
            frontMatterBuilder.AppendLine(line);
        }

        var markdown = await reader.ReadToEndAsync(cancellationToken);

        return (frontMatterBuilder.ToString().Trim(), markdown.Trim());
    }
}