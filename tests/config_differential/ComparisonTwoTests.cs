using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CameraUnlock.Core.Config;
using CameraUnlock.Core.Input;
using YapyapHeadTracking.Config;
using YapyapHeadTracking.Legacy;
using Xunit;

namespace YapyapHeadTracking.Tests.Differential
{
    /// <summary>
    /// Comparison 2, the import against the migration, and what the import does with each value
    /// v0.2.0 ran on. Every input of comparison 1 is migrated into a new CameraUnlock.ini, from a
    /// writable and from a read-only legacy file, once over the built-in Defaults.ini and once over
    /// a Defaults.ini that differs on every row this game takes from it.
    /// </summary>
    public class ComparisonTwoTests
    {
        /// <summary>Every global row the table binds, each away from its built-in value.</summary>
        private const string OtherDefaults =
            "[CameraUnlock]\r\nConfigFormat=1\r\n\r\n" +
            "[Network]\r\nUdpPort=4343\r\n\r\n" +
            "[General]\r\nEnableOnStartup=false\r\nWorldSpaceYaw=false\r\nRotationEnabled=true\r\n\r\n" +
            "[Smoothing]\r\nLocalSmoothing=0.25\r\nRemoteSmoothing=0.35\r\n\r\n" +
            "[Position]\r\nPositionEnabled=false\r\nPositionLimitX=0.26\r\nPositionLimitY=0.16\r\nPositionLimitYDown=0.17\r\n" +
            "PositionLimitZ=0.36\r\nPositionLimitZBack=0.06\r\n\r\n" +
            "[Hotkeys]\r\nToggleKey=F8\r\nCycleTrackingModeKey=F7\r\nYawModeKey=F6\r\n";

        private static readonly Lazy<string> MigratedDir = new Lazy<string>(() =>
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "migrated");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            return dir;
        });

        [Fact]
        public void TheMigrationHoldsWhatTheImportReadOverTheBuiltInDefaults()
        {
            Compare(null);
        }

        [Fact]
        public void TheMigrationHoldsWhatTheImportReadOverOtherDefaults()
        {
            Compare(OtherDefaults);
        }

        private static void Compare(string defaultsIni)
        {
            List<DifferentialInput> inputs = Inputs.All().ToList();
            var failures = new ConcurrentBag<string>();
            var refused = new ConcurrentBag<string>();
            var created = new ConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);
            byte[] committed = File.ReadAllBytes(ConfigTests.Committed());
            // What every row holds in a new CameraUnlock.ini over this Defaults.ini, which is what a
            // row that follows Defaults.ini migrates to.
            string fresh = MigrationOutcome.Describe(MigrationOutcome.Run(new DifferentialInput("no file", null), defaultsIni, false).Config);
            Parallel.ForEach(inputs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, input =>
            {
                ImportOutcome import = ImportOutcome.Run(input);
                foreach (bool readOnly in input.Bytes == null ? new[] { false } : new[] { false, true })
                {
                    string name = input.Name + (readOnly ? " (read-only)" : "");
                    MigrationOutcome migration = MigrationOutcome.Run(input, defaultsIni, readOnly);

                    if (import.Error != null || migration.Error != null)
                    {
                        if (import.Error != migration.Error) failures.Add(name + ": import " + import.Error + ", migration " + migration.Error);
                        if (!readOnly) refused.Add(input.Name);
                        continue;
                    }

                    string imported = FollowDefaults(MigrationOutcome.Describe(import.Config), import.Result.FollowsDefaultsIni, fresh);
                    string migrated = MigrationOutcome.Describe(migration.Config);
                    if (input.Bytes == null)
                    {
                        if (migration.Status != ConfigLoadStatus.Created) failures.Add(name + ": " + migration.Status);
                        if (!migration.Created.SequenceEqual(committed)) failures.Add(name + ": the created file is not config/CameraUnlock.ini");
                        if (imported != migrated) failures.Add(name + ":\n" + Diff(imported, migrated));
                        continue;
                    }

                    if (migration.Status != ConfigLoadStatus.Migrated)
                    {
                        failures.Add(name + ": " + migration.Status + ": " + migration.Reason);
                        continue;
                    }
                    created[Sha256(migration.Created)] = migration.Created;
                    string text = Encoding.ASCII.GetString(migration.Created);
                    foreach (ConceptDescriptor concept in import.Result.FollowsDefaultsIni)
                    {
                        if (!text.Contains("\r\n" + concept.Key + "=default\r\n"))
                            failures.Add(name + ": " + concept.Key + " follows Defaults.ini and is not written as default");
                    }
                    if (imported != migrated) failures.Add(name + ":\n" + Diff(imported, migrated));
                }
            });
            Assert.True(failures.IsEmpty, string.Join("\n", failures.OrderBy(f => f, StringComparer.Ordinal).Take(20)));
            // Handed to core's canonical config lint by tests/config_differential/lint-migrated.mjs,
            // which pixi run test runs next.
            foreach (KeyValuePair<string, byte[]> file in created)
            {
                File.WriteAllBytes(Path.Combine(MigratedDir.Value, file.Key + ".ini"), file.Value);
            }
            Assert.Equal(ComparisonOneTests.RefusedByBepInEx(), refused.OrderBy(n => n, StringComparer.Ordinal));
        }

        /// <summary>
        /// The map proof: on every input, what the converted plugin runs on from the import is what
        /// v0.2.0 ran on, apart from exactly the values the approved changes drop.
        /// </summary>
        [Fact]
        public void TheImportKeepsEverySettingButTheApprovedDrops()
        {
            var failures = new ConcurrentBag<string>();
            Parallel.ForEach(Inputs.All().ToList(), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, input =>
            {
                LegacyOutcome oracle = Oracle.Run(input);
                ImportOutcome import = ImportOutcome.Run(input);
                if (oracle.Error != null)
                {
                    if (import.Error != oracle.Error) failures.Add(input.Name + ": " + import.Error);
                    return;
                }
                LegacyConfig old = oracle.Config;
                ImportResult result = import.Result;
                ImportStatus status = input.Bytes == null ? ImportStatus.Absent : ImportStatus.Imported;
                if (result.Status != status) failures.Add(input.Name + ": " + result.Status);

                SortedDictionary<string, string> before = LegacyStartup.Of(old);
                SortedDictionary<string, string> after = ConvertedStartup.Of(import.Config);
                // A sensitivity group is compared unless one of its values was dropped: the folded
                // values are what the runtime applies now, so they must equal what v0.2.0 applied.
                bool rotationDropped = result.Dropped.Any(d => d.Rule == DropRule.PoseShaping && d.Section == "Sensitivity");
                bool positionDropped = result.Dropped.Any(d => d.Rule == DropRule.PoseShaping && d.Section == "Position");
                bool crosshairDropped = result.Dropped.Any(d => d.Rule == DropRule.Reticle && d.Key == "CompensateCrosshair");
                bool pivotDropped = result.Dropped.Any(d => d.Rule == DropRule.TrackerPivot);
                foreach (string key in before.Keys)
                {
                    if (key == "RotationSensitivity" && rotationDropped) continue;
                    if (key == "PositionSensitivity" && positionDropped) continue;
                    if (key == "CrosshairFollowsAim" && crosshairDropped) continue;
                    if (key == "TrackerPivotForward" && pivotDropped) continue;
                    if (result.Dropped.Any(d => (d.Rule == DropRule.ModifierKey || d.Rule == DropRule.KeyCodeOutOfRange) && d.Key == key)) continue;
                    if (before[key] != after[key]) failures.Add(input.Name + ": " + key + " " + before[key] + " -> " + after[key]);
                }

                var expectedDrops = new List<string>();
                var expectedShaping = new List<string>();
                Action<string, string, float, float> shaping = (section, key, value, shipped) =>
                {
                    bool folded = value == shipped;
                    expectedShaping.Add(section + " " + key + " " + Codec(value) + " " + Codec(shipped) + " " + folded);
                    if (!folded) expectedDrops.Add("PoseShaping " + section + " " + key + " " + Codec(value));
                };
                Action<string, UnityEngine.KeyCode> modifier = (key, value) =>
                {
                    if (IsModifierKey(value)) expectedDrops.Add("ModifierKey Keybindings " + key + " " + value);
                    else if (value != UnityEngine.KeyCode.None && !KeyBindings.HasName((int)value))
                        expectedDrops.Add("KeyCodeOutOfRange Keybindings " + key + " " + ((int)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                };
                modifier("ToggleKey", old.ToggleKey);
                modifier("CycleTrackingModeKey", old.CycleTrackingModeKey);
                modifier("YawModeKey", old.YawModeKey);
                if (!old.CompensateCrosshair) expectedDrops.Add("Reticle UI CompensateCrosshair false");
                shaping("Sensitivity", "YawSensitivity", old.YawSensitivity, 1.0f);
                shaping("Sensitivity", "PitchSensitivity", old.PitchSensitivity, 1.0f);
                shaping("Sensitivity", "RollSensitivity", old.RollSensitivity, 1.0f);
                shaping("Position", "PositionSensitivityX", old.PositionSensitivityX, 1.0f);
                shaping("Position", "PositionSensitivityY", old.PositionSensitivityY, 1.0f);
                shaping("Position", "PositionSensitivityZ", old.PositionSensitivityZ, 1.0f);
                if (old.TrackerPivotForward != YapyapConfig.NeckPivotForward)
                    expectedDrops.Add("TrackerPivot Position TrackerPivotForward " + Codec(old.TrackerPivotForward));

                // A setting at the value every published build shipped was no player's choice, so
                // it follows Defaults.ini, and the tracking mode goes as one unit.
                var shipped = new LegacyConfig();
                var expectedFollows = new List<string>();
                Action<string, bool> follows = (key, unchanged) => { if (unchanged) expectedFollows.Add(key); };
                follows("UdpPort", old.UDPPort == shipped.UDPPort);
                follows("EnableOnStartup", old.EnabledOnStartup == shipped.EnabledOnStartup);
                follows("WorldSpaceYaw", old.WorldSpaceYaw == shipped.WorldSpaceYaw);
                follows("RotationEnabled", old.PositionEnabled == shipped.PositionEnabled);
                follows("PositionEnabled", old.PositionEnabled == shipped.PositionEnabled);
                follows("LocalSmoothing", old.LocalSmoothing == shipped.LocalSmoothing);
                follows("RemoteSmoothing", old.RemoteSmoothing == shipped.RemoteSmoothing);
                follows("PositionLimitX", old.PositionLimitX == shipped.PositionLimitX);
                follows("PositionLimitY", old.PositionLimitY == shipped.PositionLimitY);
                follows("PositionLimitYDown", old.PositionLimitY == shipped.PositionLimitY);
                follows("PositionLimitZ", old.PositionLimitZ == shipped.PositionLimitZ);
                follows("PositionLimitZBack", old.PositionLimitZBack == shipped.PositionLimitZBack);
                follows("ToggleKey", old.ToggleKey == shipped.ToggleKey);
                follows("CycleTrackingModeKey", old.CycleTrackingModeKey == shipped.CycleTrackingModeKey);
                follows("YawModeKey", old.YawModeKey == shipped.YawModeKey);
                string[] followed = result.FollowsDefaultsIni.Select(c => c.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();
                if (!followed.SequenceEqual(expectedFollows.OrderBy(k => k, StringComparer.Ordinal)))
                    failures.Add(input.Name + ": follows Defaults.ini " + string.Join(", ", followed));

                string[] drops = result.Dropped.Select(d => d.Rule + " " + d.Section + " " + d.Key + " " + d.Value).ToArray();
                string[] poses = result.PoseShaping.Select(p => p.Section + " " + p.Key + " " + p.Value + " " + p.Shipped + " " + p.Folded).ToArray();
                if (!drops.SequenceEqual(expectedDrops)) failures.Add(input.Name + ": dropped " + string.Join("; ", drops));
                if (!poses.SequenceEqual(expectedShaping)) failures.Add(input.Name + ": pose shaping " + string.Join("; ", poses));
            });
            Assert.True(failures.IsEmpty, string.Join("\n", failures.OrderBy(f => f, StringComparer.Ordinal).Take(20)));
        }

        /// <summary>
        /// Every build's shipped defaults fold: the pose-shaping values of each first-run file are
        /// the ones the conversion moved into code, so nothing is dropped for a player who never
        /// changed them.
        /// </summary>
        [Fact]
        public void EveryFirstRunFileFoldsItsPoseShaping()
        {
            foreach (DifferentialInput input in Inputs.FirstRuns())
            {
                ImportOutcome import = ImportOutcome.Run(input);
                Assert.True(import.Result.PoseShaping.All(p => p.Folded), input.Name);
                Assert.DoesNotContain(import.Result.Dropped, d => d.Rule == DropRule.PoseShaping);
            }
        }

        /// <summary>
        /// Fresh equals upgrade: every published build's first-run file, which holds only what that
        /// build shipped, migrates into the committed file with every global row default, over the
        /// built-in Defaults.ini and over one that differs on every row.
        /// </summary>
        [Fact]
        public void EveryFirstRunMigratesToTheCommittedFile()
        {
            string committed = Encoding.ASCII.GetString(File.ReadAllBytes(ConfigTests.Committed()));
            foreach (DifferentialInput input in Inputs.FirstRuns())
            {
                foreach (string defaultsIni in new[] { null, OtherDefaults })
                {
                    MigrationOutcome migration = MigrationOutcome.Run(input, defaultsIni, false);

                    Assert.Equal(ConfigLoadStatus.Migrated, migration.Status);
                    Assert.Equal(committed, Encoding.ASCII.GetString(migration.Created));
                    Assert.DoesNotContain(migration.Log, l => l.Contains("CompensateCrosshair") || l.Contains("TrackerPivot"));
                }
            }
        }

        /// <summary>Every KeyCode a .cfg can name converts to the key name that reads back as it.</summary>
        [Fact]
        public void EveryUnityKeyCodeConvertsToItsName()
        {
            string keys = File.ReadAllText(Path.Combine(ConfigTests.RepoRoot(), "cameraunlock-core", "data", "keys.json"));
            var codes = new List<int>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(keys, "\"unity\":\\s*(\\d+)"))
            {
                codes.Add(int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
            }
            Assert.True(codes.Count > 300, "keys.json gave " + codes.Count + " Unity codes");
            foreach (int code in codes.Where(c => c != 0))
            {
                var dropped = new List<DroppedValue>();
                string list = LegacyConfigImport.HotkeyList((UnityEngine.KeyCode)code, UnityEngine.KeyCode.Y, "ToggleKey", dropped);
                KeyBinding[] bindings;
                string error;
                Assert.True(KeyBindings.TryParse(list, out bindings, out error), code + ": " + list + ": " + error);
                if (IsModifierKey((UnityEngine.KeyCode)code))
                {
                    // N3: a Ctrl, Shift or Alt key alone is unbound, and the chord stays.
                    Assert.Equal(new[] { new KeyBinding(KeyModifiers.Ctrl | KeyModifiers.Shift, (int)UnityEngine.KeyCode.Y) }, bindings);
                    DroppedValue drop = Assert.Single(dropped);
                    Assert.Equal(DropRule.ModifierKey, drop.Rule);
                    Assert.Equal("Keybindings", drop.Section);
                    Assert.Equal(((UnityEngine.KeyCode)code).ToString(), drop.Value);
                    continue;
                }
                Assert.Empty(dropped);
                Assert.Equal(new KeyBinding(KeyModifiers.None, code), bindings[0]);
                Assert.Equal(new KeyBinding(KeyModifiers.Ctrl | KeyModifiers.Shift, (int)UnityEngine.KeyCode.Y), bindings[1]);
            }
            Assert.Equal("Ctrl+Shift+G", LegacyConfigImport.HotkeyList(UnityEngine.KeyCode.None, UnityEngine.KeyCode.G, "CycleTrackingModeKey", new List<DroppedValue>()));
        }

        /// <summary>
        /// <paramref name="described"/> with every row in <paramref name="follows"/> replaced by
        /// what <paramref name="fresh"/> holds on it. The smoothing pair also sets the position
        /// processor's copy.
        /// </summary>
        private static string FollowDefaults(string described, IEnumerable<ConceptDescriptor> follows, string fresh)
        {
            Dictionary<string, string> freshLines = Lines(fresh);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (ConceptDescriptor concept in follows)
            {
                if (!freshLines.ContainsKey(concept.Key)) throw new InvalidOperationException(concept.Key + " is not described");
                names.Add(concept.Key);
                if (concept == ConfigConcepts.LocalSmoothing) names.Add("Position.LocalSmoothing");
                if (concept == ConfigConcepts.RemoteSmoothing) names.Add("Position.RemoteSmoothing");
            }
            var s = new StringBuilder();
            foreach (string line in described.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = line.Substring(0, line.IndexOf('='));
                s.Append(names.Contains(name) ? name + "=" + freshLines[name] : line).Append('\n');
            }
            return s.ToString();
        }

        private static Dictionary<string, string> Lines(string described)
        {
            var lines = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string line in described.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = line.IndexOf('=');
                lines.Add(line.Substring(0, eq), line.Substring(eq + 1));
            }
            return lines;
        }

        /// <summary>
        /// Normalisation N1: a key code Unity names no key for, which a .cfg can hold as a number,
        /// is left unbound, the drop is logged, and the action keeps its Ctrl+Shift chord. A Ctrl,
        /// Shift or Alt key alone (N3) in the same file is unbound beside it, and the file migrates.
        /// </summary>
        [Fact]
        public void AKeyCodeUnityNamesNoKeyForImportsAsUnboundAndKeepsTheChord()
        {
            foreach (int code in new[] { -1, 1, 2, 10, 999 })
            {
                Assert.False(KeyBindings.HasName(code), code + " names a key");
                var dropped = new List<DroppedValue>();
                Assert.Equal("Ctrl+Shift+H", LegacyConfigImport.HotkeyList((UnityEngine.KeyCode)code, UnityEngine.KeyCode.H, "YawModeKey", dropped));
                DroppedValue drop = Assert.Single(dropped);
                Assert.Equal(DropRule.KeyCodeOutOfRange, drop.Rule);
                Assert.Equal("Keybindings YawModeKey " + code.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    drop.Section + " " + drop.Key + " " + drop.Value);
            }

            DifferentialInput first = Inputs.FirstRuns().Last();
            string cfg = Encoding.ASCII.GetString(first.Bytes)
                .Replace("\nToggleKey = End\r\n", "\nToggleKey = 999\r\n")
                .Replace("\nCycleTrackingModeKey = PageUp\r\n", "\nCycleTrackingModeKey = LeftShift\r\n");
            Assert.Contains("\nToggleKey = 999\r\n", cfg);
            Assert.Contains("\nCycleTrackingModeKey = LeftShift\r\n", cfg);
            MigrationOutcome migration = MigrationOutcome.Run(new DifferentialInput("ToggleKey 999", Encoding.ASCII.GetBytes(cfg)), null, false);
            Assert.Equal(ConfigLoadStatus.Migrated, migration.Status);
            Assert.Equal("Ctrl+Shift+Y", migration.Config.ToggleKeyName);
            Assert.Equal("Ctrl+Shift+G", migration.Config.CycleTrackingModeKeyName);
            string created = Encoding.ASCII.GetString(migration.Created);
            Assert.Contains("\r\nToggleKey=Ctrl+Shift+Y\r\n", created);
            Assert.Contains("\r\nCycleTrackingModeKey=Ctrl+Shift+G\r\n", created);
            Assert.Contains(migration.Log, l => l.Contains("ToggleKey=999, it is not a key code Unity names"));
            Assert.Contains(migration.Log, l => l.Contains("CycleTrackingModeKey=LeftShift, it is a Ctrl, Shift or Alt key"));
        }

        private static bool IsModifierKey(UnityEngine.KeyCode key)
        {
            return key == UnityEngine.KeyCode.LeftShift || key == UnityEngine.KeyCode.RightShift
                || key == UnityEngine.KeyCode.LeftControl || key == UnityEngine.KeyCode.RightControl
                || key == UnityEngine.KeyCode.LeftAlt || key == UnityEngine.KeyCode.RightAlt;
        }

        private static string Codec(float value)
        {
            return Encoding.ASCII.GetString(new FloatCodec().Render(value));
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                var text = new StringBuilder();
                foreach (byte b in sha.ComputeHash(bytes)) text.Append(b.ToString("x2"));
                return text.ToString();
            }
        }

        private static string Diff(string expected, string actual)
        {
            string[] e = expected.Split('\n');
            string[] a = actual.Split('\n');
            var lines = new List<string>();
            for (int i = 0; i < e.Length && i < a.Length; i++)
            {
                if (e[i] != a[i]) lines.Add("  expected " + e[i] + " | got " + a[i]);
            }
            return string.Join("\n", lines);
        }
    }
}
