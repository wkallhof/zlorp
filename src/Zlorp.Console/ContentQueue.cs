using System.Threading.Tasks.Dataflow;

namespace Zlorp.Console;

public class ContentQueue
{
    private BufferBlock<WebContent> _queue;

    public ContentQueue()
    {
        _queue = new BufferBlock<WebContent>();
    }

    public void Add(WebContent content) => _queue.Post(content);

    public IDisposable LinkTo(ITargetBlock<WebContent> target)
        => _queue.LinkTo(target, new DataflowLinkOptions(){ PropagateCompletion = true});
}