using System;
using System.Threading.Tasks;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using Warlogic.RegistryBrowser;

namespace Warlogic.RegistryBrowser.Mcp.Editor
{
    [McpForUnityTool(
        "manage_registry_browser",
        Description =
            "Registry Browser for Warlogic packages. Manages scoped-registry packages with embed/de-embed workflows.\n" +
            "\n" +
            "Actions:\n" +
            "  status          — List configured registries and installed packages. Returns registry scope, URL, " +
            "and per-package status (latest version, installed version, embed status, git branch, uncommitted changes). " +
            "Optional filters: scope, package_id.\n" +
            "  embed           — Copy a package from its Git repository into Packages/Embeds/ for local editing. " +
            "Updates manifest to use file:Embeds/{package_id} dependency. Requires package_id. " +
            "Optional: repository_url (defaults to registry-resolved URL), commit_sha (defaults to latest).\n" +
            "  de_embed        — Remove the local embed copy and revert manifest to registry version. " +
            "DESTRUCTIVE: permanently deletes the Packages/Embeds/{package_id} directory. " +
            "Fails if the package has uncommitted changes or locked files. Requires package_id. " +
            "Optional: target_version (defaults to latest registry version).\n" +
            "  publish         — Pack the embedded package into a tarball and publish it to the scoped NPM registry. " +
            "Runs preflight checks (version bump, changelog, uncommitted changes). Requires package_id. " +
            "Optional: registry_url (defaults to matching scoped registry), confirm_republish (default false). " +
            "If the version already exists on the registry, publish fails unless confirm_republish=true. " +
            "After successful publish, the embed is removed and manifest is updated to the published version.\n" +
            "  create_package  — Scaffold a new local UPM package in Packages/Embeds/{package_id}/. " +
            "Creates assembly definitions, folder structure, package.json, README, CHANGELOG, and LICENSE. " +
            "Requires package_id and display_name. Optional: init_git (defaults to editor preference).",
        Group = "core")]
    public static class RegistryBrowserMcpTool
    {
        public class Parameters
        {
            [ToolParameter(
                "Action to perform. Must be one of: status, embed, de_embed, publish, create_package.")]
            public string Action { get; set; }

            [ToolParameter(
                "Package ID in reverse-domain format (e.g. com.warlogic.registrybrowser). " +
                "Required for embed, de_embed, publish, and create_package. " +
                "Optional for status — when provided, filters output to this package only.",
                Required = false)]
            public string PackageId { get; set; }

            [ToolParameter(
                "Git repository HTTPS URL for embed action (e.g. https://github.com/Warlander/registry-browser.git). " +
                "Optional; if omitted, the tool resolves the repository URL from the package's registry metadata.",
                Required = false)]
            public string RepositoryUrl { get; set; }

            [ToolParameter(
                "Git commit SHA to checkout for embed action. " +
                "Optional; if omitted, the tool fetches and uses the latest commit from the repository.",
                Required = false)]
            public string CommitSha { get; set; }

            [ToolParameter(
                "Target version string for de_embed action (e.g. 1.2.3). " +
                "Optional; if omitted, the tool resolves the latest version from the registry and updates the manifest to that version.",
                Required = false)]
            public string TargetVersion { get; set; }

            [ToolParameter(
                "NPM registry URL for publish action (e.g. https://upm.maciejcyranowicz.com). " +
                "Optional; if omitted, the tool resolves the registry URL from the configured scoped registries by matching the package scope prefix.",
                Required = false)]
            public string RegistryUrl { get; set; }

            [ToolParameter(
                "Set to true to allow publishing a version that already exists on the registry. " +
                "When true, the existing version is unpublished before the new tarball is published. " +
                "Default: false.",
                Required = false,
                DefaultValue = "false")]
            public bool ConfirmRepublish { get; set; }

            [ToolParameter(
                "Human-readable display name for create_package action (e.g. 'Registry Browser'). " +
                "Required for create_package; ignored for all other actions.",
                Required = false)]
            public string DisplayName { get; set; }

            [ToolParameter(
                "Initialize a Git repository in the newly created package directory for create_package. " +
                "Optional; if omitted, uses the editor preference from RegistryBrowserConfig.",
                Required = false)]
            public bool InitGit { get; set; }

            [ToolParameter(
                "Filter status output to a specific registry scope (e.g. com.warlogic). " +
                "Optional; only packages under this scope are returned.",
                Required = false)]
            public string Scope { get; set; }
        }

        public static async Task<object> HandleCommand(JObject @params)
        {
            if (@params == null)
            {
                return new ErrorResponse("Parameters cannot be null.");
            }

            var p = new ToolParams(@params);

            var actionResult = p.GetRequired("action");
            if (!actionResult.IsSuccess)
            {
                return new ErrorResponse(actionResult.ErrorMessage);
            }

            string action = actionResult.Value.ToLowerInvariant();

            try
            {
                switch (action)
                {
                    case "embed":
                        return await EmbedAsync(p);
                    case "de_embed":
                        return await DeEmbedAsync(p);
                    case "publish":
                        return await PublishAsync(p);
                    case "create_package":
                        return await CreatePackageAsync(p);
                    case "status":
                        return await StatusAsync(p);
                    default:
                        return new ErrorResponse(
                            $"Unknown action: '{action}'. Supported actions: status, embed, de_embed, publish, create_package.");
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, new { stackTrace = ex.StackTrace });
            }
        }

        private static async Task<object> EmbedAsync(ToolParams p)
        {
            var packageResult = p.GetRequired("package_id", "'package_id' parameter is required for embed.");
            if (!packageResult.IsSuccess)
            {
                return new ErrorResponse(packageResult.ErrorMessage);
            }

            string repositoryUrl = p.Get("repository_url", null);
            string commitSha = p.Get("commit_sha", null);

            await RegistryBrowserAPI.EmbedAsync(packageResult.Value, repositoryUrl, commitSha);

            return new SuccessResponse(
                $"Embedded {packageResult.Value}.",
                new { package_id = packageResult.Value });
        }

        private static async Task<object> DeEmbedAsync(ToolParams p)
        {
            var packageResult = p.GetRequired("package_id", "'package_id' parameter is required for de_embed.");
            if (!packageResult.IsSuccess)
            {
                return new ErrorResponse(packageResult.ErrorMessage);
            }

            string targetVersion = p.Get("target_version", null);

            await RegistryBrowserAPI.DeEmbedAsync(packageResult.Value, targetVersion);

            return new SuccessResponse(
                $"De-embedded {packageResult.Value}.",
                new { package_id = packageResult.Value });
        }

        private static async Task<object> PublishAsync(ToolParams p)
        {
            var packageResult = p.GetRequired("package_id", "'package_id' parameter is required for publish.");
            if (!packageResult.IsSuccess)
            {
                return new ErrorResponse(packageResult.ErrorMessage);
            }

            string registryUrl = p.Get("registry_url", null);
            bool confirmRepublish = p.GetBool("confirm_republish", false);

            await RegistryBrowserAPI.PublishAsync(packageResult.Value, registryUrl, confirmRepublish);

            return new SuccessResponse(
                $"Published {packageResult.Value}.",
                new { package_id = packageResult.Value });
        }

        private static async Task<object> CreatePackageAsync(ToolParams p)
        {
            var packageResult = p.GetRequired("package_id", "'package_id' parameter is required for create_package.");
            if (!packageResult.IsSuccess)
            {
                return new ErrorResponse(packageResult.ErrorMessage);
            }

            var displayResult = p.GetRequired("display_name", "'display_name' parameter is required for create_package.");
            if (!displayResult.IsSuccess)
            {
                return new ErrorResponse(displayResult.ErrorMessage);
            }

            bool initGit = p.GetBool("init_git", RegistryBrowserConfig.LoadInitGitForNewPackages());

            await RegistryBrowserAPI.CreatePackageAsync(packageResult.Value, displayResult.Value, initGit);

            return new SuccessResponse(
                $"Created local package {packageResult.Value} ({displayResult.Value}).",
                new { package_id = packageResult.Value, display_name = displayResult.Value, init_git = initGit });
        }

        private static async Task<object> StatusAsync(ToolParams p)
        {
            string filterScope = p.Get("scope", null);
            string filterPackageId = p.Get("package_id", null);

            StatusResult result = await RegistryBrowserAPI.GetStatusAsync(filterScope, filterPackageId);

            return new SuccessResponse("Status retrieved.", result);
        }
    }
}
