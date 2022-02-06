# zlorp
High-performance CLI website downloader. Takes any website and converts it into a offline-ready static website.

Built on the .NET 6 and the TPL Dataflow library, **zlorp** utilizes parallel processing to crawl a domain and download all linked pages and static assets. 

- [x]: Media type based processors (html, css, js, etc.)
- [x]: Extracts links from CSS files
- [x]: Controllable max degree of parallelism
- [ ]: Option to update HTML links to support static references (ex. change `/foo` to `/foo.html`)


## Build
`dotnet build ./src`

## Run
`dotnet run -- {website url}`