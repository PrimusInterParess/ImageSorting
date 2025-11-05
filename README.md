ImageSorting – Media Organizer (Console + Web API)

Overview

ImageSorting organizes photos and videos into Year/Month folders based on the best available timestamp. It prefers metadata (EXIF/QuickTime) and falls back to filesystem or blob timestamps.

You can use it as:

- Console utility (`ImageSorting`) for local folders
- ASP.NET Core Web API (`ImageSorting.API`) with Swagger UI

Direction of the app

- The API is now blob‑first: browse containers and prefixes, upload files, and sort blobs between containers into `YYYY/MM - MonthName` prefixes. Local filesystem sorting remains available.

Projects

- ImageSorting.Core: Sorting logic, options, and interfaces. Uses MetadataExtractor.
- ImageSorting.API: Web API exposing local and Azure Blob workflows. Swagger UI enabled.
- ImageSorting: Console app to run sorting locally.

Architecture

- Core contains the application services and models (local sort and Azure Blob browse/upload/sort). The API and Console reference Core.
- The API wires dependencies via DI and provides HTTP endpoints over Core services. `BlobServiceClient` is created in the API and injected into Core services.

Prerequisites

- .NET 8 SDK
- Windows, macOS, or Linux
- Azure Storage account (or Azurite). Development default uses `UseDevelopmentStorage=true`.

Supported Formats

jpg, jpeg, png, gif, bmp, tif, tiff, webp, heic, heif, dng, mov, mp4, avi

How it works (high level)

1) For each media file/blob, attempt to read capture/creation time from metadata:
   - EXIF SubIFD: DateTimeOriginal, DateTimeDigitized
   - EXIF IFD0: DateTime
   - QuickTime headers (for HEIC/HEIF and many videos)
2) If no metadata date is found, fall back to filesystem or blob last-modified time.
3) Build destination path/prefix: `<Year>/<MM - MonthName>`.
4) Avoid overwriting: if the name exists, append “(1)”, “(2)”, …
5) Write a run log with per-file results and a summary (local: file; blob: blob in a log container/prefix).

Run the Web API

Development (with Swagger):

```bash
dotnet run --project ImageSorting.API
```

- Swagger UI: http://localhost:5148/swagger
- HTTP base URL: http://localhost:5148

Configuration

`ImageSorting.API/appsettings.Development.json` defaults Azure Storage to emulator:

```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  },
  "Sorting": {
    "DefaultLogDirectory": "D:\\Library\\Documentation for sorting"
  }
}
```

For production, set `AzureStorage:ConnectionString` to your storage account connection string (or via environment variables).

API – Local filesystem sorting

POST /api/sort-images

Request (JSON):

```json
{
  "sourcePath": "C:\\Path\\To\\Your\\Media",
  "destinationPath": "D:\\Organized\\Media",
  "moveFiles": true,
  "logDirectory": "D:\\Logs\\MediaSorting"
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

API – Azure Blob browsing

GET /api/blob/containers

Response:

```json
{ "count": 2, "items": ["photos", "videos"] }
```

GET /api/blob/prefixes?container={name}&prefix={optional}

- Lists virtual folders (prefixes) within a container (and optional sub-prefix). Items do not include a trailing `/`.

Response:

```json
{ "count": 3, "items": ["2024", "2025/01 - January", "raw"] }
```

GET /api/blob/list?container={name}&prefix={optional}

Response:

```json
{
  "count": 2,
  "items": [
    { "name": "2025/11/photo.jpg", "size": 123456, "contentType": "image/jpeg", "lastModified": "2025-11-05T10:00:00+00:00" },
    { "name": "2025/11/video.mp4", "size": 987654, "contentType": "video/mp4", "lastModified": "2025-11-05T11:00:00+00:00" }
  ]
}
```

API – Azure Blob upload

POST /api/upload/blob (multipart/form-data)

Form fields:

- `container` (required): target container name
- `prefix` (optional): virtual folder path (e.g., `incoming/2025/11`). If empty, uploads to container root
- `files`: 1..N files

Response:

```json
{ "uploaded": 3, "items": ["incoming/2025/11/a.jpg", "incoming/2025/11/b.jpg", "incoming/2025/11/c.jpg"] }
```

Notes on `prefix`

- In blob APIs, `prefix` is a virtual folder path segment, not a string prepended to the filename. Example: container `photos`, prefix `incoming/2025/11`, file `image.jpg` → blob `incoming/2025/11/image.jpg`.

API – Azure Blob sorting

POST /api/sort-images/blob

Request (JSON):

```json
{
  "sourceContainer": "photos",
  "sourcePrefix": "incoming/2025/11",
  "destinationContainer": "photos-sorted",
  "moveBlobs": true,
  "logContainer": "photos-logs",
  "logPrefix": "runs"
}
```

Behavior:

- Reads each blob under `sourceContainer/sourcePrefix`, extracts best creation timestamp (metadata → fallback to blob time), and writes to `destinationContainer` under `YYYY/MM - MonthName/filename`.
- Avoids collisions by appending “(1)”, “(2)”, …
- If `moveBlobs` is true, deletes source after successful copy.
- If `logContainer` is provided, writes a UTF-8 log blob under `logPrefix` (default `logs`), returning its path.

Response (JSON):

```json
{ "moved": 120, "skipped": 0, "errors": 3, "logBlobPath": "photos-logs/runs/120 - 11.txt" }
```

PowerShell examples

Local sort:

```powershell
Invoke-RestMethod -Method Post `
  -Uri http://localhost:5148/api/sort-images `
  -ContentType 'application/json' `
  -Body (@{sourcePath='C:\\Media'; destinationPath='D:\\Photos'; moveFiles=$true} | ConvertTo-Json)
```

Blob browse and upload:

```powershell
Invoke-RestMethod -Method Get http://localhost:5148/api/blob/containers
Invoke-RestMethod -Method Get "http://localhost:5148/api/blob/prefixes?container=photos"
Invoke-RestMethod -Method Get "http://localhost:5148/api/blob/list?container=photos&prefix=incoming/2025/11"
```

```powershell
$form = @{ container='photos'; prefix='incoming/2025/11' }
Invoke-RestMethod -Method Post -Uri http://localhost:5148/api/upload/blob -Form $form -InFile 'C:\\temp\\image.jpg' -ContentType 'multipart/form-data'
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

Docker (API)

Build and run the API container:

```bash
docker build -t imagesorting-api .
docker run -p 8080:8080 -p 8081:8081 -e "AzureStorage__ConnectionString=UseDevelopmentStorage=true" --name imagesorting-api imagesorting-api
```

When sorting local files in Docker, mount host directories as volumes and reference the mounted paths in requests.

Repository Hygiene

- Ignore IDE and build artifacts (`.vs/`, `bin/`, `obj/`).
- Optional `.gitattributes` for line endings:

```gitattributes
* text=auto
*.cs      text eol=crlf
*.csproj  text eol=crlf
*.sln     text eol=crlf
*.sh      text eol=lf
```

Current Status

- Local sort endpoint `/api/sort-images` implemented.
- Blob browse endpoints: `/api/blob/containers`, `/api/blob/prefixes`, `/api/blob/list`.
- Blob upload endpoint `/api/upload/blob`.
- Blob sort endpoint `/api/sort-images/blob` with optional move and log.

Roadmap / Next Steps

- Optional dry-run mode to preview operations.
- Include/exclude patterns, size/type filters.
- Duplicate detection (hash-based) and smarter conflict handling.
- Progress reporting and cancellation.
- Tests and CI pipeline.

License

MIT or similar (update as needed).


