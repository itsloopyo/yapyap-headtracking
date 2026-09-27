using CameraUnlock.Core.Config;

namespace YapyapHeadTracking.Config
{
    /// <summary>
    /// Everything the mod reads from BepInEx\config\CameraUnlock.ini. Unity-free, so the test
    /// project compiles it and holds the committed file to it.
    /// </summary>
    internal sealed class YapyapConfig : HeadTrackingConfigData
    {
        /// <summary>The game's name as data/games.json spells it.</summary>
        public const string DisplayName = "YAPYAP";

        /// <summary>
        /// Metres from the neck pivot forward to the point the tracker follows, as every published
        /// build shipped it. The tracker owns the pivot, so no setting changes it.
        /// </summary>
        public const float NeckPivotForward = 0.08f;

        public bool ShowStartupNotification { get; set; } = true;

        public bool ShowConnectionNotifications { get; set; } = true;

        public static ConfigTable<YapyapConfig> Table()
        {
            return HeadTrackingConfigTable.Create<YapyapConfig>(
                    ConfigConcepts.UdpPort,
                    ConfigConcepts.EnableOnStartup,
                    ConfigConcepts.WorldSpaceYaw,
                    ConfigConcepts.RotationEnabled,
                    ConfigConcepts.LocalSmoothing,
                    ConfigConcepts.RemoteSmoothing,
                    ConfigConcepts.PositionEnabled,
                    ConfigConcepts.PositionLimitX,
                    ConfigConcepts.PositionLimitY,
                    ConfigConcepts.PositionLimitYDown,
                    ConfigConcepts.PositionLimitZ,
                    ConfigConcepts.PositionLimitZBack,
                    ConfigConcepts.ToggleKey,
                    ConfigConcepts.CycleTrackingModeKey,
                    ConfigConcepts.YawModeKey)
                .Select(ConfigConcepts.WorldSpaceYaw).Writable()
                .Select(ConfigConcepts.RotationEnabled).Writable()
                .Select(ConfigConcepts.PositionEnabled).Writable()
                .Local("Notifications", "ShowStartupNotification", c => c.ShowStartupNotification,
                    (c, v) => c.ShowStartupNotification = v, new BoolCodec(),
                    "true: show whether head tracking is on, and its hotkeys, when the game starts.")
                .Local("Notifications", "ShowConnectionNotifications", c => c.ShowConnectionNotifications,
                    (c, v) => c.ShowConnectionNotifications = v, new BoolCodec(),
                    "true: show a message when tracker data starts or stops arriving.");
        }
    }
}
