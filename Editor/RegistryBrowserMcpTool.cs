using System;
using System.Threading.Tasks;
using Unity.AI.MCP.Editor.Helpers;
using Unity.AI.MCP.Editor.ToolRegistry;
using Warlogic.RegistryBrowser;

namespace Warlogic.RegistryBrowser.Mcp.Editor
{
    public static class RegistryBrowserMcpTool
    {
        public class Parameters
        {
            [McpDescription(
                "Action to perform. Must be one of: status, embed, de_embed, publish, create_package.",
                Required = true)]
            public string Action { get; set; }

            [McpDescription(
                "Package ID in reverse-domain format (e.g. com.warlogic.registrybrowser). " +
                "Required for embed, de_embed, publish, and create_package. " +
                "Optional for status — when provided, filters output to this package only.",
                Required = false)]
            public string PackageId { get; set; }

            [McpDescription(
                "Git repository HTTPS URL for embed action (e.g. https://github.com/Warlander/registry-browser.git). " +
                "Optional; if omitted, the tool resolves the repository URL from the package's registry metadata.",
                Required = false)]
            public string RepositoryUrl { get; set; }

            [McpDescription(
                "Git commit SHA to checkout for embed action. " +
                "Optional; if omitted, the tool fetches and uses the latest commit from the repository.",
                Required = false)]
            public string CommitSha { get; set; }

            [McpDescription(
                "Target version string for de_embed action (e.g. 1.2.3). " +
                "Optional; if omitted, the tool resolves the latest version from the registry and updates the manifest to that version.",
                Required = false)]
            public string TargetVersion { get; set; }

            [McpDescription(
                "NPM registry URL for publish action (e.g. https://upm.maciejcyranowicz.com). " +
                "Optional; if omitted, the tool resolves the registry URL from the configured scoped registries by matching the package scope prefix.",
                Required = false)]
            public string RegistryUrl { get; set; }

            [McpDescription(
                "Set to true to allow publishing a version that already exists on the registry. " +
                "When true, the existing version is unpublished before the new tarball is published. " +
                "Default: false.",
                Required = false,
                Default = false)]
            public bool ConfirmRepublish { get; set; }

            [McpDescription(
                "Human-readable display name for create_package action (e.g. 'Registry Browser'). " +
                "Required for create_package; ignored for all other actions.",
                Required = false)]
            public string DisplayName { get; set; }

            [McpDescription(
                "Initialize a Git repository in the newly created package directory for create_package. " +
                "Optional; if omitted, uses the editor preference from RegistryBrowserConfig.",
                Required = false)]
            public bool? InitGit { get; set; }

            [McpDescription(
                "Filter status output to a specific registry scope (e.g. com.warlogic). " +
                "Optional; only packages under this scope are returned.",
                Required = false)]
            public string Scope { get; set; }
        }

        [McpTool(
            "manage_registry_browser",
            "Registry Browser for Warlogic packages. Manages scoped-registry packages with embed/de-embed workflows.\n" +
            "\n" +
            "Actions:\n" +
            "  status          — List configured registries and installed packages. Returns registry scope, URL, " +
            "and per-package status (latest version, installed version, embed status, git branch, uncommitted changes). " +
            "Optional filters: Scope, PackageId.\n" +
            "  embed           — Copy a package from its Git repository into Packages/Embeds/ for local editing. " +
            "Updates manifest to use file:Embeds/{PackageId} dependency. Requires PackageId. " +
            "Optional: RepositoryUrl (defaults to registry-resolved URL), CommitSha (defaults to latest).\n" +
            "  de_embed        — Remove the local embed copy and revert manifest to registry version. " +
            "DESTRUCTIVE: permanently deletes the Packages/Embeds/{PackageId} directory. " +
            "Fails if the package has uncommitted changes or locked files. Requires PackageId. " +
            "Optional: TargetVersion (defaults to latest registry version).\n" +
            "  publish         — Pack the embedded package into a tarball and publish it to the scoped NPM registry. " +
            "Runs preflight checks (version bump, changelog, uncommitted changes). Requires PackageId. " +
            "Optional: RegistryUrl (defaults to matching scoped registry), ConfirmRepublish (default false). " +
            "If the version already exists on the registry, publish fails unless ConfirmRepublish=true. " +
            "After successful publish, the embed is removed and manifest is updated to the published version.\n" +
            "  create_package  — Scaffold a new local UPM package in Packages/Embeds/{PackageId}/. " +
            "Creates assembly definitions, folder structure, package.json, README, CHANGELOG, and LICENSE. " +
            "Requires PackageId and DisplayName. Optional: InitGit (defaults to editor preference).",
            EnabledByDefault = true,
            Groups = new[] { "core" })]
        public static async Task<object> HandleCommand(Parameters parameters)
        {
            if (parameters == null)
            {
                return Response.Error("Parameters cannot be null.");
            }

            string action = parameters.Action?.ToLowerInvariant();

            try
            {
                switch (action)
                {
                    case "embed":
                        return await EmbedAsync(parameters);
                    case "de_embed":
                        return await DeEmbedAsync(parameters);
                    case "publish":
                        return await PublishAsync(parameters);
                    case "create_package":
                        return await CreatePackageAsync(parameters);
                    case "status":
                        return await StatusAsync(parameters);
                    default:
                        return Response.Error(
                            $"Unknown action: '{action}'. Supported actions: status, embed, de_embed, publish, create_package.");
                }
            }
            catch (Exception ex)
            {
                return Response.Error(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static async Task<object> EmbedAsync(Parameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.PackageId))
            {
                return Response.Error("'PackageId' parameter is required for embed.");
            }

            await RegistryBrowserAPI.EmbedAsync(parameters.PackageId, parameters.RepositoryUrl, parameters.CommitSha);

            return Response.Success(
                $"Embedded {parameters.PackageId}.",
                new { package_id = parameters.PackageId });
        }

        private static async Task<object> DeEmbedAsync(Parameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.PackageId))
            {
                return Response.Error("'PackageId' parameter is required for de_embed.");
            }

            await RegistryBrowserAPI.DeEmbedAsync(parameters.PackageId, parameters.TargetVersion);

            return Response.Success(
                $"De-embedded {parameters.PackageId}.",
                new { package_id = parameters.PackageId });
        }

        private static async Task<object> PublishAsync(Parameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.PackageId))
            {
                return Response.Error("'PackageId' parameter is required for publish.");
            }

            await RegistryBrowserAPI.PublishAsync(parameters.PackageId, parameters.RegistryUrl, parameters.ConfirmRepublish);

            return Response.Success(
                $"Published {parameters.PackageId}.",
                new { package_id = parameters.PackageId });
        }

        private static async Task<object> CreatePackageAsync(Parameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.PackageId))
            {
                return Response.Error("'PackageId' parameter is required for create_package.");
            }

            if (string.IsNullOrWhiteSpace(parameters.DisplayName))
            {
                return Response.Error("'DisplayName' parameter is required for create_package.");
            }

            bool initGit = parameters.InitGit ?? RegistryBrowserConfig.LoadInitGitForNewPackages();

            await RegistryBrowserAPI.CreatePackageAsync(parameters.PackageId, parameters.DisplayName, initGit);

            return Response.Success(
                $"Created local package {parameters.PackageId} ({parameters.DisplayName}).",
                new { package_id = parameters.PackageId, display_name = parameters.DisplayName, init_git = initGit });
        }

        private static async Task<object> StatusAsync(Parameters parameters)
        {
            StatusResult result = await RegistryBrowserAPI.GetStatusAsync(parameters.Scope, parameters.PackageId);

            return Response.Success("Status retrieved.", result);
        }
    }
}
