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
- ImageSorting.Data: EF Core entities, DbContext, and migrations used for persisted uploads/metadata.
- ImageSorting.API: Web API exposing local and Azure Blob workflows. Swagger UI enabled.
- ImageSorting.Web: Angular 18 web UI for browsing blobs, uploading, and previewing media.
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

`ImageSorting.API/appsettings.Development.json` defaults Azure Storage to emulator and includes a local SQL Server connection string:

```json
{
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  },
  "Sorting": {
    "DefaultLogDirectory": "D:\\Library\\Documentation for sorting"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ImageSorting;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

For production, set `AzureStorage:ConnectionString` to your storage account connection string (or via environment variables). Configure `ConnectionStrings:DefaultConnection` for SQL Server when using metadata‑persisting endpoints.

Database (for metadata persistence)

- The API registers `ImageSortingContext` using `ConnectionStrings:DefaultConnection`.
- Apply migrations before using the metadata‑persisting upload endpoint:

```bash
dotnet tool install -g dotnet-ef # if not already installed
dotnet ef database update --project ImageSorting.Data --startup-project ImageSorting.API
```

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

GET /api/blob/prefixes/all?container={name}

- Lists all prefixes recursively under the container.

Response:

```json
{ "count": 12, "items": ["2024", "2024/01 - January" /* ... */] }
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

API – Managed containers (DB-backed)

- Manage a list of logical containers persisted in the database. Creating a managed container also ensures the Azure Storage container exists.

GET /api/containers

Response:

```json
[
  { "id": 1, "name": "photos", "dateCreatedUtc": "2025-11-07T09:50:00Z" }
]
```

POST /api/containers

Request:

```json
{ "name": "photos" }
```

Behavior:

- Validates name (3–63 chars, lowercase letters/numbers/hyphens, must start/end with alphanumeric, no "--").
- Creates the Azure container if it does not exist.
- Persists a row in the `Containers` table; 409 if it already exists.

Response:

```json
{ "id": 1, "name": "photos", "dateCreatedUtc": "2025-11-07T09:50:00Z" }
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

Notes:

- Default request size limit is 500 MB per request (configurable in code).

POST /api/upload/blob-with-metadata (multipart/form-data)

- Same form fields as above.
- Extracts basic file metadata and persists a record to the database for each upload (requires a valid `DefaultConnection` and applied migrations).

Response:

```json
{
  "uploaded": 3,
  "items": [
    {
      "blobName": "incoming/2025/11/a.jpg",
      "metadata": { "bestDateTakenUtc": "2025-11-05T10:00:00Z", "contentType": "image/jpeg", "width": 4000, "height": 3000 }
    },
    { "blobName": "incoming/2025/11/b.jpg", "metadata": { /* ... */ } },
    { "blobName": "incoming/2025/11/c.jpg", "metadata": { /* ... */ } }
  ]
}
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

API – Azure Blob content streaming

GET /api/blob/content?container={name}&name={blobPath}

- Streams the blob bytes with the original `Content-Type`. Useful for inline image/video rendering in the web app.

Example:

```http
GET /api/blob/content?container=photos&name=2025/11/photo.jpg
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

```powershell
# Upload and persist metadata
$form = @{ container='photos'; prefix='incoming/2025/11' }
Invoke-RestMethod -Method Post -Uri http://localhost:5148/api/upload/blob-with-metadata -Form $form -InFile 'C:\\temp\\image.jpg' -ContentType 'multipart/form-data'
```

```powershell
# Fetch content for inline display
Invoke-WebRequest -OutFile photo.jpg "http://localhost:5148/api/blob/content?container=photos&name=2025/11/photo.jpg"
```

Managed containers:

```powershell
Invoke-RestMethod -Method Get http://localhost:5148/api/containers
Invoke-RestMethod -Method Post -ContentType 'application/json' -Uri http://localhost:5148/api/containers -Body (@{ name='photos' } | ConvertTo-Json)
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

Run the Web App (Angular)

Development:

```bash
cd ImageSorting.Web
npm install
npm start
# Opens http://localhost:4200 with a dev proxy to http://localhost:5148/api
```

Notes:

- The dev proxy (`ImageSorting.Web/proxy.conf.json`) forwards `/api` to the API base URL, avoiding CORS in development.
- Ensure the API is running on `http://localhost:5148` (default launch profile) or update the proxy target accordingly.

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
- Blob upload endpoint `/api/upload/blob` and upload-with-metadata `/api/upload/blob-with-metadata`.
- Blob sort endpoint `/api/sort-images/blob` with optional move and log.
- Blob content streaming endpoint `/api/blob/content`.

Roadmap / Next Steps

- Optional dry-run mode to preview operations.
- Include/exclude patterns, size/type filters.
- Duplicate detection (hash-based) and smarter conflict handling.
- Progress reporting and cancellation.
- Tests and CI pipeline.

License

MIT or similar (update as needed).


