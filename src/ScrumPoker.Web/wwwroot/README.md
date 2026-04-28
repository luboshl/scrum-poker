# wwwroot

This directory is the web root for static assets served by the ASP.NET Core host.

## Migration Status

Frontend assets currently live in `public/` at the repository root and are served
by the Node.js backend. They will be moved here as part of the frontend SignalR
migration (see `CSHARP_BACKEND_MIGRATION_SPEC.md` and the corresponding issue).

During development, the legacy Node.js app (`scrum-poker-legacy`) continues to
serve the existing frontend through the .NET Aspire orchestration.
