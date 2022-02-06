using System.Threading.Tasks.Dataflow;

namespace Zlorp.Console;

public class ContentFetcher
{
    public TransformBlock<WebContent, WebContent> Input;

    public ContentFetcher(int maxDegreeOfParallelism)
    {
        Input = new TransformBlock<WebContent, WebContent>(FetchContent, new ExecutionDataflowBlockOptions(){ MaxDegreeOfParallelism = maxDegreeOfParallelism});
    }

    public async Task<WebContent> FetchContent(WebContent content)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(content.Url);
        content = content with { Response = response };
        return content;
    }

    public IDisposable LinkTo(ITargetBlock<WebContent> target, Predicate<WebContent> onCondition)
        => Input.LinkTo(target, new DataflowLinkOptions(){ PropagateCompletion = true }, onCondition);
}