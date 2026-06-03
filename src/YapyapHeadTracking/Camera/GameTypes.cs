using System;
using System.Reflection;
using CameraUnlock.Core.Reflection;

namespace YapyapHeadTracking.Camera
{
    /// <summary>
    /// Cached reflection lookups into YAPYAP's Assembly-CSharp.
    /// All lookups happen once; a missing type/member logs once and leaves the
    /// accessor returning null so callers can degrade to engine-level heuristics.
    ///
    /// Verified against YAPYAP build 23509070 (Unity 6000.0.58f2):
    ///   YAPYAP.CameraManager : public static CameraManager Instance (field),
    ///                          public Camera MainCamera (property)
    ///   YAPYAP.InputManager  : public static InputManager Instance (field),
    ///                          public bool HasFocus (property)
    ///   YAPYAP.Pawn          : public static Pawn LocalInstance (field)
    ///   YAPYAP.UIManager     : public static UIManager Instance (field),
    ///                          public UIGame uiGame (field)
    ///   YAPYAP.UIGame        : public GameObject crosshairRootObj (field)
    /// </summary>
    internal static class GameTypes
    {
        public static Action<string> Log = _ => { };

        private static bool _resolved;

        private static Func<object> _getCameraManagerInstance;
        private static Func<object, object> _getCameraManagerMainCamera;

        private static Func<object> _getInputManagerInstance;
        private static Func<object, object> _getInputManagerHasFocus;

        private static Func<object> _getPawnLocalInstance;

        private static Func<object> _getUiManagerInstance;
        private static Func<object, object> _getUiManagerUiGame;
        private static Func<object, object> _getUiGameCrosshairRoot;

        public static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            var assemblyCSharp = FindAssembly("Assembly-CSharp");
            if (assemblyCSharp == null)
            {
                Log("Assembly-CSharp not found; game-specific integration disabled");
                return;
            }

            var cameraManager = assemblyCSharp.GetType("YAPYAP.CameraManager");
            if (cameraManager != null)
            {
                var instance = cameraManager.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                var mainCamera = cameraManager.GetProperty("MainCamera", BindingFlags.Public | BindingFlags.Instance);
                if (instance != null && mainCamera != null)
                {
                    _getCameraManagerInstance = CompiledGetters.ForStaticField(instance);
                    _getCameraManagerMainCamera = CompiledGetters.ForInstanceProperty(mainCamera);
                }
            }
            ReportLookup("CameraManager.Instance.MainCamera", HasCameraManager);

            var inputManager = assemblyCSharp.GetType("YAPYAP.InputManager");
            if (inputManager != null)
            {
                var instance = inputManager.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                var hasFocus = inputManager.GetProperty("HasFocus", BindingFlags.Public | BindingFlags.Instance);
                // PropertyType check: IsLocalPlayerFocused unboxes this as bool. If a game
                // update changes the property's type, degrade to the Cursor.lockState
                // fallback instead of throwing InvalidCastException every frame.
                if (instance != null && hasFocus != null && hasFocus.PropertyType == typeof(bool))
                {
                    _getInputManagerInstance = CompiledGetters.ForStaticField(instance);
                    _getInputManagerHasFocus = CompiledGetters.ForInstanceProperty(hasFocus);
                }
            }

            var pawn = assemblyCSharp.GetType("YAPYAP.Pawn");
            if (pawn != null)
            {
                var localInstance = pawn.GetField("LocalInstance", BindingFlags.Public | BindingFlags.Static);
                if (localInstance != null)
                    _getPawnLocalInstance = CompiledGetters.ForStaticField(localInstance);
            }
            ReportLookup("InputManager.HasFocus + Pawn.LocalInstance gameplay gate", HasFocusGate);

            var uiManager = assemblyCSharp.GetType("YAPYAP.UIManager");
            var uiGame = assemblyCSharp.GetType("YAPYAP.UIGame");
            if (uiManager != null && uiGame != null)
            {
                var instance = uiManager.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                var uiGameField = uiManager.GetField("uiGame", BindingFlags.Public | BindingFlags.Instance);
                var crosshairRoot = uiGame.GetField("crosshairRootObj", BindingFlags.Public | BindingFlags.Instance);
                if (instance != null && uiGameField != null && crosshairRoot != null)
                {
                    _getUiManagerInstance = CompiledGetters.ForStaticField(instance);
                    _getUiManagerUiGame = CompiledGetters.ForInstanceField(uiGameField);
                    _getUiGameCrosshairRoot = CompiledGetters.ForInstanceField(crosshairRoot);
                }
            }
            ReportLookup("UIManager.uiGame.crosshairRootObj", HasCrosshair);
        }

        public static bool HasCameraManager =>
            _getCameraManagerInstance != null && _getCameraManagerMainCamera != null;

        public static bool HasFocusGate =>
            _getInputManagerInstance != null && _getInputManagerHasFocus != null && _getPawnLocalInstance != null;

        public static bool HasCrosshair =>
            _getUiManagerInstance != null && _getUiManagerUiGame != null && _getUiGameCrosshairRoot != null;

        public static UnityEngine.Camera GetGameMainCamera()
        {
            if (!HasCameraManager) return null;
            var manager = _getCameraManagerInstance() as UnityEngine.Object;
            if (manager == null) return null;
            return _getCameraManagerMainCamera(manager) as UnityEngine.Camera;
        }

        /// <summary>Null when the gate members are unavailable (game update changed them).</summary>
        public static bool? IsLocalPlayerFocused()
        {
            if (!HasFocusGate) return null;

            var pawn = _getPawnLocalInstance() as UnityEngine.Object;
            if (pawn == null) return false;

            var inputManager = _getInputManagerInstance() as UnityEngine.Object;
            if (inputManager == null) return false;

            return (bool)_getInputManagerHasFocus(inputManager);
        }

        public static UnityEngine.GameObject GetCrosshairRoot()
        {
            if (!HasCrosshair) return null;
            var manager = _getUiManagerInstance() as UnityEngine.Object;
            if (manager == null) return null;
            var uiGame = _getUiManagerUiGame(manager) as UnityEngine.Object;
            if (uiGame == null) return null;
            return _getUiGameCrosshairRoot(uiGame) as UnityEngine.GameObject;
        }

        private static Assembly FindAssembly(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == name)
                    return asm;
            }
            return null;
        }

        private static void ReportLookup(string what, bool found)
        {
            Log(found
                ? $"Resolved {what}"
                : $"Could not resolve {what} - feature degraded to engine heuristics");
        }
    }
}
