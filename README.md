ImageSorting – Media Organizer (Console + Web API)

Overview

ImageSorting sorts photos and videos into Year/Month folders based on the best available timestamp. It prefers metadata (EXIF/QuickTime) and falls back to filesystem times. You can use it as:

- Console utility (`ImageSorting`)
- ASP.NET Core Web API (`ImageSorting.API`) with Swagger UI

Projects

- ImageSorting.Core: Sorting logic, options, and interfaces. Uses MetadataExtractor.
- ImageSorting.API: Minimal Web API exposing the sorter. Swagger UI enabled.
- ImageSorting: Console app to run sorting locally from the terminal.

Prerequisites

- .NET 8 SDK
- Windows, macOS, or Linux (paths in examples use Windows style)

Supported Formats

jpg, jpeg, png, gif, bmp, tif, tiff, webp, heic, heif, dng, mov, mp4, avi

How it works (high level)

1) For each media file, try to read capture/creation time from metadata:
   - EXIF SubIFD: DateTimeOriginal, DateTimeDigitized
   - EXIF IFD0: DateTime
   - QuickTime headers (for HEIC/HEIF and some videos)
2) If no metadata date is available, fall back to filesystem timestamps.
3) Build destination path: <DestinationRoot>/<Year>/<MM - MonthName>
4) Avoid overwriting: if the name exists, append “(1)”, “(2)”, …
5) Write a log file describing all operations and a summary.

Run the Web API

Development (with Swagger):

```bash
dotnet run --project ImageSorting.API
```

- Swagger UI: http://localhost:5148/swagger
- HTTP base URL: http://localhost:5148

Endpoint

POST /api/sort-images

Request body (JSON):

```json
{
  "sourcePath": "C:\\Path\\To\\Your\\Media",
  "destinationPath": "D:\\Organized\\Media",
  "moveFiles": true,
  "logDirectory": "D:\\Logs\\MediaSorting" // optional; falls back to config
}
```

Response (JSON):

```json
{
  "moved": 123,
  "skipped": 4,
  "errors": 2,
  "logFilePath": "D:\\Logs\\MediaSorting\\123 - YourFolder.txt"
}
```

PowerShell example:

```powershell
Invoke-RestMethod -Method Post `
  -Uri http://localhost:5148/api/sort-images `
  -ContentType 'application/json' `
  -Body (@{sourcePath='C:\\Media'; destinationPath='D:\\Photos'; moveFiles=$true} | ConvertTo-Json)
```

Run the Console App

Interactively (prompts for paths if omitted):

```bash
dotnet run --project ImageSorting
```

With arguments:

```bash
dotnet run --project ImageSorting "C:\\Media" "D:\\Photos"
```

Configuration

The API reads `SortingOptions` from configuration. To set a default log directory:

`ImageSorting.API/appsettings.json`

```json
{
  "Sorting": {
    "DefaultLogDirectory": "D:\\Library\\Documentation for sorting"
  }
}
```

If `logDirectory` is not supplied in the request, the service uses `Sorting:DefaultLogDirectory`. If that is empty/unset, it falls back to `D:\\Library\\Documentation for sorting`.

Docker (API)

Build and run the API container:

```bash
docker build -t imagesorting-api .
docker run -p 8080:8080 -p 8081:8081 --name imagesorting-api imagesorting-api
```

Within the container, the API listens on 8080 (HTTP) and 8081 (HTTPS). Map ports as needed and call the same endpoint (`/api/sort-images`). When running in Docker, mount host directories as volumes if you want to sort files on the host.

Example with volume mounts (adjust paths for your OS):

```bash
docker run -p 8080:8080 -p 8081:8081 `
  -v "C:\\Media:C:\\Media" `
  -v "D:\\Photos:D:\\Photos" `
  --name imagesorting-api imagesorting-api
```

Then POST a request with `sourcePath` and `destinationPath` pointing to the mounted paths.

Repository Hygiene

- Ignore IDE and build artifacts (`.vs/`, `bin/`, `obj/`).
- To normalize line endings on Windows, add a `.gitattributes` (optional):

```gitattributes
* text=auto
*.cs      text eol=crlf
*.csproj  text eol=crlf
*.sln     text eol=crlf
*.sh      text eol=lf
```

Current Status

- Core sorting service implemented with MetadataExtractor and filesystem fallback.
- API endpoint `/api/sort-images` available with Swagger UI.
- Console app available for local sorting.
- Logging per run with summary and per-file results; log file finalized with a count-based name.

Roadmap / Next Steps

- Optional dry-run mode to preview operations.
- Configurable include/exclude patterns and size/type filters.
- Duplicate detection (hash-based) and smarter conflict handling.
- Progress reporting and cancellation support in API responses.
- Tests and CI pipeline.

License

MIT or similar (choose and update this section if needed).


