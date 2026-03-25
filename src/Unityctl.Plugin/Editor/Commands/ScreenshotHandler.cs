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
            var includeOverlayUi = request.GetParam<bool>("includeOverlayUi");
            var outputPath = request.GetParam("outputPath", null);

            if (width <= 0 || height <= 0)
                return InvalidParameters("width and height must be positive integers");

            if (includeOverlayUi && !string.Equals(viewType, "game", StringComparison.OrdinalIgnoreCase))
                return InvalidParameters("includeOverlayUi is only supported for Game View capture");

            UnityEngine.Camera camera;
            var captureMode = "camera-render";
            if (string.Equals(viewType, "game", StringComparison.OrdinalIgnoreCase))
            {
                if (includeOverlayUi)
                {
                    try
                    {
                        var overlayTexture = CaptureGameViewWithOverlay(width, height);
                        return EncodeAndReturnScreenshot(overlayTexture, width, height, format, quality, outputPath, viewType, "game-view-visible", includeOverlayUi);
                    }
                    catch (Exception)
                    {
                        return Fail(
                            StatusCode.InvalidParameters,
                            "Overlay-inclusive Game View capture is not available in this editor state. `screenshot capture --view game` still captures camera-rendered content only; use `ui find` or manual inspection for Screen Space - Overlay UI.");
                    }
                }

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

                return EncodeAndReturnScreenshot(tex, width, height, format, quality, outputPath, viewType, captureMode, includeOverlayUi);
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

#if UNITY_EDITOR
        private static CommandResponse EncodeAndReturnScreenshot(
            UnityEngine.Texture2D sourceTexture,
            int width,
            int height,
            string format,
            int quality,
            string outputPath,
            string viewType,
            string captureMode,
            bool includeOverlayUi)
        {
            byte[] imageBytes;
            string actualFormat;

            if (string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase))
            {
                imageBytes = UnityEngine.ImageConversion.EncodeToJPG(sourceTexture, quality);
                actualFormat = "jpg";
            }
            else
            {
                imageBytes = UnityEngine.ImageConversion.EncodeToPNG(sourceTexture);
                actualFormat = "png";
            }

            var base64 = Convert.ToBase64String(imageBytes);

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
                ["captureMode"] = captureMode,
                ["overlayUiIncluded"] = includeOverlayUi,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["unityVersion"] = UnityEngine.Application.unityVersion,
                ["outputPath"] = savedPath
            };

            return Ok("Screenshot captured", data);
        }

        private static UnityEngine.Texture2D CaptureGameViewWithOverlay(int width, int height)
        {
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
            var editorAssembly = typeof(UnityEditor.EditorWindow).Assembly;
            var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            var gameViewWindow = gameViewType != null
                ? UnityEditor.EditorWindow.GetWindow(gameViewType)
                : null;

            if (gameViewWindow == null)
                throw new InvalidOperationException("Game View window could not be opened for overlay capture.");

            gameViewWindow.Focus();
            gameViewWindow.Repaint();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            var captured = TryCaptureFromRenderView(gameViewWindow, width, height);
            if (captured == null)
            {
                captured = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
            }
            if (captured == null)
            {
                captured = CaptureGameViewWindowPixels(gameViewWindow);
            }

            if (captured == null)
                throw new InvalidOperationException("Game View overlay capture returned no texture.");

            if (captured.width == width && captured.height == height)
                return captured;

            var resized = ResizeTexture(captured, width, height);
            UnityEngine.Object.DestroyImmediate(captured);
            return resized;
        }

        private static UnityEngine.Texture2D ResizeTexture(UnityEngine.Texture2D source, int width, int height)
        {
            var previous = UnityEngine.RenderTexture.active;
            var target = UnityEngine.RenderTexture.GetTemporary(width, height, 0, UnityEngine.RenderTextureFormat.ARGB32);
            var resized = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);

            try
            {
                UnityEngine.Graphics.Blit(source, target);
                UnityEngine.RenderTexture.active = target;
                resized.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
                resized.Apply();
                return resized;
            }
            finally
            {
                UnityEngine.RenderTexture.active = previous;
                UnityEngine.RenderTexture.ReleaseTemporary(target);
            }
        }

        private static UnityEngine.Texture2D CaptureGameViewWindowPixels(UnityEditor.EditorWindow gameViewWindow)
        {
            var pixelsPerPoint = UnityEditor.EditorGUIUtility.pixelsPerPoint;
            var windowRect = gameViewWindow.position;
            var sourceWidth = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(windowRect.width * pixelsPerPoint));
            var sourceHeight = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(windowRect.height * pixelsPerPoint));
            var screenPosition = new UnityEngine.Vector2(windowRect.x * pixelsPerPoint, windowRect.y * pixelsPerPoint);
            var pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(screenPosition, sourceWidth, sourceHeight);
            if (pixels == null || pixels.Length == 0)
                return null;

            var texture = new UnityEngine.Texture2D(sourceWidth, sourceHeight, UnityEngine.TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static UnityEngine.Texture2D TryCaptureFromRenderView(UnityEditor.EditorWindow gameViewWindow, int width, int height)
        {
            var renderMethod = gameViewWindow.GetType().GetMethod(
                "RenderView",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (renderMethod == null)
                return null;

            var renderTexture = renderMethod.Invoke(gameViewWindow, new object[] { UnityEngine.Vector2.zero, true }) as UnityEngine.RenderTexture;
            if (renderTexture == null)
                return null;

            return ReadRenderTexture(renderTexture, width, height);
        }

        private static UnityEngine.Texture2D ReadRenderTexture(UnityEngine.RenderTexture source, int width, int height)
        {
            var previous = UnityEngine.RenderTexture.active;
            try
            {
                UnityEngine.RenderTexture.active = source;
                var texture = new UnityEngine.Texture2D(source.width, source.height, UnityEngine.TextureFormat.RGBA32, false);
                texture.ReadPixels(new UnityEngine.Rect(0, 0, source.width, source.height), 0, 0);
                texture.Apply();

                if (source.width == width && source.height == height)
                    return texture;

                var resized = ResizeTexture(texture, width, height);
                UnityEngine.Object.DestroyImmediate(texture);
                return resized;
            }
            finally
            {
                UnityEngine.RenderTexture.active = previous;
            }
        }
#endif

        protected override CommandResponse HandleException(Exception exception)
        {
            return Fail(StatusCode.UnknownError, $"Screenshot capture failed: {exception.Message}",
                errors: GetStackTrace(exception));
        }
    }
}
