# WebSiteSyncTool

A C# tool that crawls websites, optionally renders JavaScript content with Playwright, and extracts clean text.

## CLI Usage

```bash
dotnet run --project WebSiteSyncTool -- \
  --startUrl=https://example.com \
  --outputPath=C:\example\ \
  --maxPages=100 \
  --maxDepth=2 \
  --filter=.*docs.* \
  --textrequired="ds9,deep space nine""
  --skipHrefs=mailto:,tel: \
  --urlPrefix=https://example.com/docs \
  --useJs=true
```

## Setup

```bash
dotnet add package HtmlAgilityPack
dotnet add package Microsoft.Playwright
playwright install
```

## Features

- Respects robots.txt
- Filters unwanted hrefs
- Restricts domain/path scope
- Optional JavaScript rendering
