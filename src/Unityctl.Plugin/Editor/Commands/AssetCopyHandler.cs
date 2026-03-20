using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetCopyHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetCopy;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var source = request.GetParam("source", null);
            if (string.IsNullOrEmpty(source))
                return InvalidParameters("Parameter 'source' is required.");

            var destination = request.GetParam("destination", null);
            if (string.IsNullOrEmpty(destination))
                return InvalidParameters("Parameter 'destination' is required.");

            // Check if source is an internal asset (has GUID in AssetDatabase)
            var sourceGuid = UnityEditor.AssetDatabase.AssetPathToGUID(source);
            bool isExternal = string.IsNullOrEmpty(sourceGuid);

            if (isExternal)
            {
                return CopyExternal(source, destination);
            }

            var success = UnityEditor.AssetDatabase.CopyAsset(source, destination);
            if (!success)
                return Fail(StatusCode.UnknownError, $"Failed to copy '{source}' to '{destination}'.");

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(destination);

            return Ok($"Copied '{source}' to '{destination}'", new JObject
            {
                ["sourcePath"] = source,
                ["sourceIsExternal"] = false,
                ["path"] = destination,
                ["guid"] = guid
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private CommandResponse CopyExternal(string source, string destination)
        {
            // Resolve absolute source path
            string absoluteSource;
            if (System.IO.Path.IsPathRooted(source))
                absoluteSource = System.IO.Path.GetFullPath(source);
            else
                absoluteSource = System.IO.Path.GetFullPath(source);

            bool isDirectory = System.IO.Directory.Exists(absoluteSource);
            bool isFile = System.IO.File.Exists(absoluteSource);

            if (!isFile && !isDirectory)
                return Fail(StatusCode.NotFound, $"External source not found: {absoluteSource}");

            // Resolve destination to absolute path within project
            var projectRoot = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
            var absoluteDest = System.IO.Path.IsPathRooted(destination)
                ? destination
                : System.IO.Path.Combine(projectRoot, destination);

            try
            {
                if (isDirectory)
                {
                    CopyDirectory(absoluteSource, absoluteDest);
                }
                else
                {
                    var destDir = System.IO.Path.GetDirectoryName(absoluteDest);
                    if (!string.IsNullOrEmpty(destDir) && !System.IO.Directory.Exists(destDir))
                        System.IO.Directory.CreateDirectory(destDir);

                    System.IO.File.Copy(absoluteSource, absoluteDest, overwrite: false);
                }
            }
            catch (System.IO.IOException ex)
            {
                return Fail(StatusCode.UnknownError, $"Failed to copy external source: {ex.Message}");
            }

            UnityEditor.AssetDatabase.ImportAsset(destination, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(destination);

            return Ok($"Copied external '{source}' to '{destination}'", new JObject
            {
                ["sourcePath"] = source,
                ["sourceIsExternal"] = true,
                ["path"] = destination,
                ["guid"] = guid ?? ""
            });
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            if (!System.IO.Directory.Exists(destDir))
                System.IO.Directory.CreateDirectory(destDir);

            foreach (var file in System.IO.Directory.GetFiles(sourceDir))
            {
                var destFile = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(file));
                System.IO.File.Copy(file, destFile, overwrite: false);
            }

            foreach (var subDir in System.IO.Directory.GetDirectories(sourceDir))
            {
                var destSubDir = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }
#endif
    }
}
