# Registry Browser MCP

MCP (Model Context Protocol) integration for Registry Browser. Exposes Registry Browser operations — embed, de-embed, publish, and create packages — as MCP tools, enabling AI agents and IDE integrations to manage Unity packages programmatically.

# Installation

## Via Git URL

Open **Window → Package Manager**, click **+**, and choose **Add package from git URL**.

To install the latest version:
```
https://github.com/Warlander/registry-browser-mcp.git
```

To install a specific release, append the tag:
```
https://github.com/Warlander/registry-browser-mcp.git#2.0.0
```

## Via Scoped Registry

Add the Warlogic registry to your `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "Warlogic",
      "url": "https://upm.maciejcyranowicz.com",
      "scopes": ["com.warlogic"]
    }
  ],
  "dependencies": {
    "com.warlogic.registrybrowser.mcp": "2.0.0"
  }
}
```

Then open **Window > Package Manager** and look for `com.warlogic.registrybrowser.mcp`.

# Prerequisites

- **com.unity.ai.assistant** — the official Unity AI Assistant package, which provides the Unity MCP framework this package integrates with.
- **com.warlogic.registrybrowser** — the Registry Browser tool whose operations are exposed via MCP.

# Setup

1. Ensure `com.unity.ai.assistant` and `com.warlogic.registrybrowser` are installed in your project.
2. Install this package. The `manage_registry_browser` MCP tool is automatically discovered and registered by Unity's MCP tool registry.

# Usage

Once installed, the following MCP tool becomes available to any connected MCP client (e.g., AI assistants, IDE integrations):

- **`manage_registry_browser`** — performs Registry Browser operations programmatically.
  - **`status`** — list configured registries and installed packages with their versions and embed status.
  - **`embed`** — clone a package repository into `Packages/Embeds/` at a specific commit.
  - **`de_embed`** — remove a local embed and restore the registry-hosted version.
  - **`publish`** — publish an embedded or local package to a configured UPM registry.
  - **`create_package`** — scaffold a new local UPM package with optional Git initialization.
