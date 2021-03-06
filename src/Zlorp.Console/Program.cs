using Zlorp.Console;

/*
TODO: 
Zlorp Snapshotter 

4. Possibly when handling HTML, after reading in the anchor links, adjust them to be .HTML if they are not already
*/

if (args.Length != 1)
{
    Console.WriteLine("Expecting 1 argument for initial url");
    return 1;
}

if(!Uri.TryCreate(args[0], new UriCreationOptions(), out var startUrl))
{
    Console.WriteLine("Invalid start url");
    return 1;
}

Console.WriteLine("Starting with: "+startUrl.AbsoluteUri);

var maxParallelism = 10;

var contentQueue = new ContentQueue();
var contentFetcher = new ContentFetcher(maxParallelism);
var htmlProcessor = new HtmlProcessor(contentQueue, maxParallelism);
var cssProcessor = new CssProcessor(contentQueue, maxParallelism);
var contentDownloader = new ContentDownloader(maxParallelism);

contentQueue.LinkTo(contentFetcher.Input);

contentFetcher.LinkTo(htmlProcessor.Input, onCondition: x => x.Type == WebContentType.Html);
contentFetcher.LinkTo(cssProcessor.Input, onCondition: x => x.Type == WebContentType.Css);

contentFetcher.LinkTo(contentDownloader.Input, 
    onCondition: x
     => x.Type == WebContentType.Javascript 
    || x.Type == WebContentType.File);

htmlProcessor.LinkTo(contentDownloader.Input);
cssProcessor.LinkTo(contentDownloader.Input);

contentQueue.Add(new WebContent(startUrl));

await contentDownloader.Input.Completion;

return 0;