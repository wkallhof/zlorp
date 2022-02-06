namespace Zlorp.Console;

public record WebContent(Uri Url, Uri Source)
{
    public HttpResponseMessage? Response { get; init; }

    public WebContentType Type => Response?.Content?.Headers?.ContentType?.MediaType switch {
        "text/html" => WebContentType.Html,
        "text/javascript" => WebContentType.Javascript,
        "text/css" => WebContentType.Css,
        _ => WebContentType.File
    };
}

public enum WebContentType
{
    File,
    Html,
    Javascript,
    Css
}