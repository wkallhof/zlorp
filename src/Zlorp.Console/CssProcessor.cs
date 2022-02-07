using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;

namespace Zlorp.Console;

public class CssProcessor
{
    private readonly ConcurrentDictionary<Uri, Uri> _extractedUrls;
    public readonly TransformBlock<WebContent, WebContent> Input;
    private readonly ContentQueue _queue;

    public CssProcessor(ContentQueue queue, int maxDegreeOfParallelism)
    {
        _queue = queue;
        _extractedUrls = new ConcurrentDictionary<Uri, Uri>();
        Input = new TransformBlock<WebContent, WebContent>(ProcessCss, new ExecutionDataflowBlockOptions(){ MaxDegreeOfParallelism = maxDegreeOfParallelism});
    }

    public async Task<WebContent> ProcessCss(WebContent content)
    {
        var css = await content.Response.Content.ReadAsStringAsync();
        
        var matches = Regex.Matches(css, @"url\(""(.*?)""\)");
        if(!matches.Any(x => x.Success))
            return content;

        static Uri BuildUri(string? path, Uri source)
            => path switch{
                null => source,
                //ex http://foo.bar/buzz
                string s when s.StartsWith("http") => new Uri(path),
                // ex: ./foo/bar, /foo/bar
                string s when s.StartsWith('/')  => new Uri(Path.Join(source.GetLeftPart(UriPartial.Authority), path)),
                // ex. foo/bar, ./foo/bar, foo.html
                _ => Path.GetExtension(source.AbsoluteUri) != null
                    ? new Uri(Path.Join(source.AbsoluteUri.Replace(Path.GetFileName(source.AbsoluteUri), ""), path))
                    : new Uri(Path.Join(source.AbsoluteUri, path)),
            };

        var urls = matches
            .Where(x => x.Success && x.Groups.Count >=2)
            .Select(x => x.Groups.Values.ElementAt(1).Value)
            .Where(x => !x.StartsWith("data"))
            .Select(x => BuildUri(x, content.Url))
            .ToList();

        urls.Where(x => x.GetLeftPart(UriPartial.Authority).Equals(content.Url.GetLeftPart(UriPartial.Authority)) && _extractedUrls.TryAdd(x, x)).ToList().ForEach(x => {
            _queue.Add(new WebContent(x));
        });

        return content;
    }

    public IDisposable LinkTo(ITargetBlock<WebContent> target)
        => Input.LinkTo(target, new DataflowLinkOptions(){ PropagateCompletion = true});
}