using System.Threading.Tasks.Dataflow;

namespace Zlorp.Console;

public class ContentDownloader
{
    public readonly ActionBlock<WebContent> Input;

    public ContentDownloader(int maxDegreeOfParallelism)
    {
        Input = new ActionBlock<WebContent>(DownloadContent, new ExecutionDataflowBlockOptions(){ MaxDegreeOfParallelism = maxDegreeOfParallelism});
    }

    public static async Task DownloadContent(WebContent content)
    {
        var url = content.Url;

        if(url.AbsolutePath == "/")
            return;

        if(!url.AbsolutePath.Contains('.'))
            url = new Uri(content.Url.AbsoluteUri + ".html");

        var saveLocation = "./out"+url.AbsolutePath;

        System.Console.WriteLine($"Downloading {url} to {saveLocation}");

        Directory.CreateDirectory(Path.GetDirectoryName(saveLocation)!);

        var bytes = await content.Response!.Content.ReadAsByteArrayAsync();

        await File.WriteAllBytesAsync(saveLocation, bytes);
    }
}