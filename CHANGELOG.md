# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2026-07-29

### Added
- New `remove_from_project` action for `manage_registry_browser`. Removes a package from the project: for embedded packages it deletes the local embed and removes the manifest dependency; for registry-installed packages it uses `Client.Remove`.
- New optional `Force` parameter for `remove_from_project`. When `true`, removes an embedded package even if it has uncommitted local changes. Default is `false`.

## [2.0.0] - 2026-07-29

### Changed
- Migrated from third-party `com.coplaydev.unity-mcp` to Unity's official MCP framework in `com.unity.ai.assistant`.
- Tool is now discovered automatically by Unity's MCP tool registry via `[McpTool]` and `[McpDescription]` attributes.

### Fixed
- Removed invalid `Group = "core"` argument on the Coplay MCP tool attribute, which caused compile error CS0246.

## [1.0.1] - 2026-05-06

### Fixed
- Fixed parameter name casing in `manage_registry_browser` tool. All parameter lookups now use PascalCase (`Action`, `PackageId`, etc.) to match the MCP framework's schema extraction, resolving the `"'action' parameter is required"` error.

## [1.0.0] - 2026-05-06

### This is the first release of *Registry Browser MCP*.
