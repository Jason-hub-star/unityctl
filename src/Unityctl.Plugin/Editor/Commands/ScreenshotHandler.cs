using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScreenshotHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Screenshot;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            if (UnityEngine.Application.isBatchMode)
                return Fail(StatusCode.InvalidParameters, "Screenshot capture is not available in batch mode (no GPU rendering)");

            var viewType = request.GetParam("view", "scene");
            var width = request.GetParam("width", 1920);
            var height = request.GetParam("height", 1080);
            var format = request.GetParam("format", "png");
            var quality = request.GetParam("quality", 75);
            var outputPath = request.GetParam("outputPath", null);

            if (width <= 0 || height <= 0)
                return InvalidParameters("width and height must be positive integers");

            UnityEngine.Camera camera;
            if (string.Equals(viewType, "game", StringComparison.OrdinalIgnoreCase))
            {
                camera = UnityEngine.Camera.main;
                if (camera == null)
                {
                    // Fallback: find any camera in the scene
                    camera = UnityEngine.Object.FindObjectOfType<UnityEngine.Camera>();
                }
                if (camera == null)
                    return InvalidParameters("No camera found in the scene for Game View capture. Add a Camera component to the scene.");
            }
            else
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView == null)
                    return InvalidParameters("No active Scene View found. Open a Scene View in the Unity Editor.");

                camera = sceneView.camera;
                if (camera == null)
                    return InvalidParameters("Scene View camera is not available");
            }

            // Save original state
            var originalTargetTexture = camera.targetTexture;
            var originalActiveRT = UnityEngine.RenderTexture.active;

            UnityEngine.RenderTexture rt = null;
            UnityEngine.Texture2D tex = null;

            try
            {
                rt = UnityEngine.RenderTexture.GetTemporary(width, height, 24, UnityEngine.RenderTextureFormat.ARGB32);
                camera.targetTexture = rt;
                camera.Render();

                UnityEngine.RenderTexture.active = rt;
                tex = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
                tex.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] imageBytes;
                string actualFormat;

                if (string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    imageBytes = UnityEngine.ImageConversion.EncodeToJPG(tex, quality);
                    actualFormat = "jpg";
                }
                else
                {
                    imageBytes = UnityEngine.ImageConversion.EncodeToPNG(tex);
                    actualFormat = "png";
                }

                var base64 = Convert.ToBase64String(imageBytes);

                // Save to file if outputPath specified
                string savedPath = null;
                if (!string.IsNullOrEmpty(outputPath))
                {
                    var dir = System.IO.Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir);

                    System.IO.File.WriteAllBytes(outputPath, imageBytes);
                    savedPath = outputPath;
                }

                var data = new JObject
                {
                    ["width"] = width,
                    ["height"] = height,
                    ["format"] = actualFormat,
                    ["base64"] = base64,
                    ["viewType"] = viewType,
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    ["unityVersion"] = UnityEngine.Application.unityVersion,
                    ["outputPath"] = savedPath
                };

                return Ok("Screenshot captured", data);
            }
            finally
            {
                // Restore original state
                camera.targetTexture = originalTargetTexture;
                UnityEngine.RenderTexture.active = originalActiveRT;

                if (rt != null)
                    UnityEngine.RenderTexture.ReleaseTemporary(rt);

                if (tex != null)
                    UnityEngine.Object.DestroyImmediate(tex);
            }
#else
            return NotInEditor();
#endif
        }

        protected override CommandResponse HandleException(Exception exception)
        {
            return Fail(StatusCode.UnknownError, $"Screenshot capture failed: {exception.Message}",
                errors: GetStackTrace(exception));
        }
    }
}
