using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using AngleSharp;
using AngleSharp.Dom;

namespace Zlorp.Console;

public class HtmlProcessor
{
    private readonly ConcurrentDictionary<Uri, Uri> _extractedUrls;
    public readonly TransformBlock<WebContent, WebContent> Input;
    private readonly ContentQueue _queue;

    public HtmlProcessor(ContentQueue queue, int maxDegreeOfParallelism)
    {
        _queue = queue;
        _extractedUrls = new ConcurrentDictionary<Uri, Uri>();
        Input = new TransformBlock<WebContent, WebContent>(ProcessHtml, new ExecutionDataflowBlockOptions(){ MaxDegreeOfParallelism = maxDegreeOfParallelism});
    }

    public async Task<WebContent> ProcessHtml(WebContent content)
    {
        var host = content.Url.GetLeftPart(UriPartial.Authority);
        using var stream = await content.Response!.Content.ReadAsStreamAsync();
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        
        var document = await context.OpenAsync((x) => x.Content(stream));

        static bool HasAttributeWithValue(IElement el, string attribute) => el.HasAttribute(attribute) && !string.IsNullOrWhiteSpace(el.GetAttribute(attribute));
        static Uri BuildUri(string? path, Uri source)
        {
            var urlString = path switch{
                null => source.AbsoluteUri,
                //ex http://foo.bar/buzz
                string s when s.StartsWith("http") => path,
                // ex: ./foo/bar, /foo/bar
                string s when s.StartsWith('/')  => Path.Join(source.GetLeftPart(UriPartial.Authority), path),
                // ex. foo/bar, ./foo/bar, foo.html
                _ => Path.GetExtension(source.AbsoluteUri) != null
                    ? Path.Join(source.AbsoluteUri.Replace(Path.GetFileName(source.AbsoluteUri), ""), path)
                    : Path.Join(source.AbsoluteUri, path)
            };

            return Uri.TryCreate(urlString, UriKind.Absolute, out var result) ? new Uri(result.GetLeftPart(UriPartial.Path)) : source;
        }

        var urls = document.QuerySelectorAll("a, link")
            .Where(x => HasAttributeWithValue(x, "href"))
            .Select(x => x.GetAttribute("href"))
            .Where(x => !x.StartsWith("#") && !x.StartsWith("javascript:") && !x.StartsWith("mailto:") && !x.StartsWith("tel:"))
            .Select(x => BuildUri(x, content.Url))
            .ToList();

        var imgScriptUrls = document.QuerySelectorAll("img, script")
            .Where(x => HasAttributeWithValue(x, "src"))
            .Select(x => x.GetAttribute("src"))
            .Where(x => !x.StartsWith("data:"))
            .Select(x => BuildUri(x, content.Url))
            .ToList();

        urls.AddRange(imgScriptUrls);

        urls.Where(x => x.GetLeftPart(UriPartial.Authority).Equals(host) && _extractedUrls.TryAdd(x, x)).ToList().ForEach(x => {
            _queue.Add(new WebContent(x));
        });

        return content;
    }

    public IDisposable LinkTo(ITargetBlock<WebContent> target)
        => Input.LinkTo(target, new DataflowLinkOptions(){ PropagateCompletion = true});
}