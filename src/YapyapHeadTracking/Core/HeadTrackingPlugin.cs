using BepInEx;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Tracking;
using CameraUnlock.Core.Unity.Extensions;
using CameraUnlock.Core.Unity.Tracking;
using CameraUnlock.Core.Unity.UI;
using YapyapHeadTracking.Camera;
using YapyapHeadTracking.Config;

namespace YapyapHeadTracking.Core
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class HeadTrackingPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.cameraunlock.yapyap.headtracking";
        public const string PluginName = "YAPYAP Head Tracking";
        public const string PluginVersion = "0.2.0";

        private const float StartupNotificationSeconds = 4f;
        private const float StatusNotificationSeconds = 1.5f;
        private const int TrackingModeCount = 3;

        private ConfigManager _config;
        private OpenTrackReceiver _receiver;
        private TrackingProcessor _processor;
        private PoseInterpolator _interpolator;
        private PositionProcessor _positionProcessor;
        private PositionInterpolator _positionInterpolator;
        private ViewMatrixTrackingController _cameraController;
        private GameStateDetector _gameStateDetector;
        private InputHandler _inputHandler;
        private NotificationUI _notificationUI;
        private AnchoredOffsetCompensator _crosshairCompensator;
        private bool _trackingEnabled;
        private bool _wasReceiving;
        // Last logged connection locality, for edge detection only. The value that
        // selects LocalSmoothing vs RemoteSmoothing is pushed onto the processors by
        // the controller, which re-reads it every frame.
        private bool _cachedIsRemoteConnection;
        private TrackingMode _trackingMode;
        private bool _initialized;

        private void Awake()
        {
            Logger.LogInfo($"{PluginName} v{PluginVersion} initializing...");

            GameTypes.Log = msg => Logger.LogInfo(msg);

            _config = new ConfigManager();
            _config.Initialize(Config);

            BuildPipeline();
            BuildCameraController();
            // UI must exist before the state detector: its first UpdateState() fires
            // StateChanged synchronously, and that handler touches the crosshair compensator.
            BuildUI();
            BuildGameStateDetector();
            BuildInput();

            _receiver.Start(_config.UDPPort.Value);
            _trackingEnabled = _config.EnabledOnStartup.Value;
            _initialized = true;

            Logger.LogInfo($"{PluginName} initialized. Tracking {(_trackingEnabled ? "enabled" : "disabled")}");
            Logger.LogInfo($"Listening on UDP port {_config.UDPPort.Value}");

            if (_config.ShowStartupNotification.Value)
            {
                string status = _trackingEnabled ? "Head Tracking: ON" : "Head Tracking: OFF";
                _notificationUI.ShowNotification($"{status}\n{BuildHotkeyInfo()}", StartupNotificationSeconds);
            }
        }

        private void BuildPipeline()
        {
            _receiver = new OpenTrackReceiver();
            _receiver.Log = msg => Logger.LogInfo(msg);

            _processor = new TrackingProcessor
            {
                LocalSmoothing = _config.LocalSmoothing.Value,
                RemoteSmoothing = _config.RemoteSmoothing.Value,
                Sensitivity = new SensitivitySettings(
                    _config.YawSensitivity.Value,
                    _config.PitchSensitivity.Value,
                    _config.RollSensitivity.Value,
                    invertYaw: false,
                    invertPitch: true,
                    invertRoll: false),
                Deadzone = DeadzoneSettings.None
            };
            _interpolator = new PoseInterpolator();

            _positionProcessor = new PositionProcessor
            {
                Settings = PositionSettings.Symmetric(
                    _config.PositionSensitivityX.Value,
                    _config.PositionSensitivityY.Value,
                    _config.PositionSensitivityZ.Value,
                    _config.PositionLimitX.Value,
                    _config.PositionLimitY.Value,
                    _config.PositionLimitZ.Value,
                    _config.PositionLimitZBack.Value,
                    _config.LocalSmoothing.Value,
                    _config.RemoteSmoothing.Value,
                    invertX: true, invertY: false, invertZ: false),
                TrackerPivotForward = _config.TrackerPivotForward.Value
            };
            _positionInterpolator = new PositionInterpolator();
        }

        private void BuildCameraController()
        {
            // View-matrix modification matters twice over in YAPYAP: spell aim reads the
            // Cinemachine state, and FirstPersonCamFollowIK reads camera.transform.rotation
            // to drive the player body - both must see the clean rotation.
            _cameraController = new ViewMatrixTrackingController(
                _receiver, _processor, _interpolator,
                _positionProcessor, _positionInterpolator,
                GameTypes.GetGameMainCamera);
            _cameraController.WorldSpaceYaw = _config.WorldSpaceYaw.Value;
            // Seed the mode from config so the first cycle press transitions away
            // from the current mode rather than back to it.
            SetTrackingMode(_config.PositionEnabled.Value
                ? TrackingMode.RotationAndPosition
                : TrackingMode.RotationOnly);
            _cameraController.Enable();
        }

        private void BuildGameStateDetector()
        {
            _gameStateDetector = new GameStateDetector();
            _gameStateDetector.StateChanged += OnGameStateChanged;
            _gameStateDetector.Initialize();
        }

        private void BuildInput()
        {
            _inputHandler = new InputHandler(_config);
            _inputHandler.OnTogglePressed += HandleToggle;
            _inputHandler.OnCycleTrackingModePressed += HandleCycleTrackingMode;
            _inputHandler.OnToggleYawModePressed += HandleToggleYawMode;
        }

        private void BuildUI()
        {
            _notificationUI = new NotificationUI();
            _crosshairCompensator = new AnchoredOffsetCompensator(GameTypes.GetCrosshairRoot);
        }

        private string BuildHotkeyInfo()
        {
            return $"[{_inputHandler.ToggleKey}/Ctrl+Shift+{ChordHotkeys.ToggleLetter}] Toggle, " +
                   $"[{_inputHandler.CycleTrackingModeKey}/Ctrl+Shift+{ChordHotkeys.PositionLetter}] Cycle Mode, " +
                   $"[{_inputHandler.YawModeKey}/Ctrl+Shift+{ChordHotkeys.FourthToggleLetter}] Yaw";
        }

        private void Update()
        {
            // Awake may have failed partway, leaving a subset of fields null.
            // A single guard avoids per-field NRE risk if init ordering changes.
            if (!_initialized) return;
            _inputHandler.CheckInput();
            _gameStateDetector.Update();
            _notificationUI.Update();
            MonitorConnectionState();
            MonitorConnectionLocality();
        }

        // Logs a change in the receiver's connection locality. Read only: the controller
        // owns the write, pushing the same flag into both processors from ProcessFrame
        // immediately before either one runs (it owns them from construction), so a
        // second push here would be redundant rather than authoritative.
        private void MonitorConnectionLocality()
        {
            bool isRemoteConnection = _receiver.IsRemoteConnection;
            if (isRemoteConnection == _cachedIsRemoteConnection) return;

            _cachedIsRemoteConnection = isRemoteConnection;
            Logger.LogInfo($"Connection locality changed: remote={isRemoteConnection}");
        }

        private void LateUpdate()
        {
            if (!_initialized) return;
            bool shouldTrack = _trackingEnabled && _gameStateDetector.IsGameplayActive;
            bool applying = _cameraController.ProcessFrame(shouldTrack);
            UpdateCrosshair(applying);
        }

        private void OnGUI()
        {
            _notificationUI?.Draw();
        }

        private void OnDestroy()
        {
            Logger.LogInfo($"{PluginName} shutting down...");

            if (_inputHandler != null)
            {
                _inputHandler.OnTogglePressed -= HandleToggle;
                _inputHandler.OnCycleTrackingModePressed -= HandleCycleTrackingMode;
                _inputHandler.OnToggleYawModePressed -= HandleToggleYawMode;
            }
            if (_gameStateDetector != null)
            {
                _gameStateDetector.StateChanged -= OnGameStateChanged;
                _gameStateDetector.Shutdown();
            }

            _crosshairCompensator?.Restore();
            _cameraController?.Disable();
            _receiver?.Dispose();
        }

        private void UpdateCrosshair(bool trackingApplied)
        {
            // Restore on the config-off path too: BepInEx configs are runtime-mutable
            // (ConfigurationManager / file reload), and an offset applied before the
            // toggle would otherwise stick permanently. Camera resolution stays gated
            // behind the cheap checks so disabled frames never touch the resolver.
            bool compensate = _config.CompensateCrosshair.Value && trackingApplied;
            UnityEngine.Vector2 offset;
            if (!compensate || !_cameraController.TryGetAimScreenOffset(out offset))
            {
                _crosshairCompensator.Restore();
                return;
            }

            _crosshairCompensator.ApplyOffset(offset);
        }

        private void MonitorConnectionState()
        {
            bool isReceiving = _receiver.IsReceiving;
            if (isReceiving == _wasReceiving)
                return;

            // The log line is outside the notification gate: it is the only evidence in
            // LogOutput.log that tracker packets ever arrived, and a user who turned the
            // on-screen popup off should not lose the ability to diagnose "no tracking".
            if (isReceiving)
            {
                Logger.LogInfo($"OpenTrack connection established on port {_config.UDPPort.Value} (remote sender: {_receiver.IsRemoteConnection})");
                if (_config.ShowConnectionNotifications.Value)
                    _notificationUI.ShowConnectionEstablished();
            }
            else
            {
                Logger.LogInfo("OpenTrack connection lost");
                if (_config.ShowConnectionNotifications.Value)
                    _notificationUI.ShowConnectionLost();
            }
            _wasReceiving = isReceiving;
        }

        private void HandleToggle()
        {
            _trackingEnabled = !_trackingEnabled;
            if (_trackingEnabled)
            {
                _cameraController.OnTrackingEnabled();
                _notificationUI.ShowTrackingEnabled();
                Logger.LogInfo("Head tracking enabled");
            }
            else
            {
                _cameraController.OnTrackingDisabled();
                _notificationUI.ShowTrackingDisabled();
                Logger.LogInfo("Head tracking disabled");
            }
        }

        private void HandleCycleTrackingMode()
        {
            SetTrackingMode((TrackingMode)(((int)_trackingMode + 1) % TrackingModeCount));

            string label = "Tracking: " + _trackingMode.Description();
            _notificationUI.ShowNotification(label, NotificationType.Info, StatusNotificationSeconds);
            Logger.LogInfo(label);
        }

        private void SetTrackingMode(TrackingMode mode)
        {
            _trackingMode = mode;
            _cameraController.RotationEnabled = mode != TrackingMode.PositionOnly;
            _cameraController.PositionEnabled = mode != TrackingMode.RotationOnly;
        }

        private void HandleToggleYawMode()
        {
            _cameraController.WorldSpaceYaw = !_cameraController.WorldSpaceYaw;
            _notificationUI.ShowNotification(
                _cameraController.WorldSpaceYaw ? "Yaw: World-locked" : "Yaw: Camera-local",
                NotificationType.Info,
                StatusNotificationSeconds);
            Logger.LogInfo($"Yaw mode: {(_cameraController.WorldSpaceYaw ? "world-locked" : "camera-local")}");
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Gameplay && _trackingEnabled)
            {
                _cameraController.OnTrackingEnabled();
            }
            else if (newState != GameState.Gameplay)
            {
                _crosshairCompensator.Restore();
                _cameraController.ResetState();
            }
        }
    }
}
