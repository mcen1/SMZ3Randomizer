﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Win32;

using Randomizer.App.ViewModels;
using Randomizer.SMZ3;
using Randomizer.SMZ3.FileData;
using Randomizer.SMZ3.Generation;

namespace Randomizer.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const bool SCAM = true;

        private readonly string _optionsPath;
        private readonly Task _loadSpritesTask;

        public MainWindow()
        {
            InitializeComponent();
            SamusSprites.Add(Sprite.DefaultSamus);
            LinkSprites.Add(Sprite.DefaultLink);

            _loadSpritesTask = Task.Run(() => LoadSprites())
                .ContinueWith(_ => Trace.WriteLine("Finished loading sprites."));

            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SMZ3CasRandomizer");
            Directory.CreateDirectory(basePath);
            _optionsPath = Path.Combine(basePath, "options.json");

            try
            {
                if (!File.Exists(_optionsPath))
                    Options = new RandomizerOptions(this);

                Options = RandomizerOptions.Load(_optionsPath)
                    .WithOwner(this);
            }
            catch
            {
                Options = new RandomizerOptions(this);
            }

            DataContext = Options.Seed;
        }

        public ObservableCollection<Sprite> SamusSprites { get; } = new();

        public ObservableCollection<Sprite> LinkSprites { get; } = new();

        public RandomizerOptions Options { get; }

        public void LoadSprites()
        {
            var spritesPath = Path.Combine(
                Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                "Sprites");
            var sprites = Directory.EnumerateFiles(spritesPath, "*.rdc", SearchOption.AllDirectories)
                .Select(x => Sprite.LoadSprite(x))
                .OrderBy(x => x.Name);

            Dispatcher.Invoke(() =>
            {
                foreach (var sprite in sprites)
                {
                    switch (sprite.SpriteType)
                    {
                        case SpriteType.Samus:
                            SamusSprites.Add(sprite);
                            break;

                        case SpriteType.Link:
                            LinkSprites.Add(sprite);
                            break;
                    }
                }
            }, DispatcherPriority.Loaded);
        }

        public string SaveRomToFile()
        {
            var randomizer = new SMZ3.Generation.Randomizer();
            var rom = GenerateRom(randomizer, out var seed);

            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SMZ3CasRandomizer", "Seeds", $"{DateTimeOffset.Now:yyyyMMdd-HHmmss}_{seed.Seed}");
            Directory.CreateDirectory(folderPath);

            var romFileName = $"SMZ3_Cas_{DateTimeOffset.Now:yyyyMMdd-HHmmss}_{seed.Seed}.sfc";
            var romPath = Path.Combine(folderPath, romFileName);
            EnableMsu1Support(rom, romPath);
            File.WriteAllBytes(romPath, rom);

            var spoilerLog = GetSpoilerLog(seed);
            File.WriteAllText(Path.ChangeExtension(romPath, ".txt"), spoilerLog);

            return romPath;
        }

        private static string GetSpoilerLog(SeedData seed)
        {
            var log = new StringBuilder();

            for (var i = 0; i < seed.Playthrough.Count; i++)
            {
                if (seed.Playthrough[i].Count == 0)
                    continue;

                log.AppendLine(Underline($"Sphere {i + 1}"));
                log.AppendLine();
                foreach (var (location, item) in seed.Playthrough[i])
                    log.AppendLine($"{location}: {item}");
                log.AppendLine();
            }

            log.AppendLine(Underline("All items"));
            log.AppendLine();

            var world = seed.Worlds.Single();
            foreach (var location in world.World.Locations)
            {
                log.AppendLine($"{location}: {location.Item}");
            }

            return log.ToString();
        }

        private static string Underline(string text, char line = '-')
            => text + "\n" + new string(line, text.Length);

        private static bool IsScam(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.ProgressiveTunic => false,
                ItemType.ProgressiveShield => SCAM,
                ItemType.ProgressiveSword => false,
                ItemType.Bow => false,
                ItemType.SilverArrows => false,
                ItemType.BlueBoomerang => SCAM,
                ItemType.RedBoomerang => SCAM,
                ItemType.Hookshot => false,
                ItemType.Mushroom => false,
                ItemType.Powder => false,
                ItemType.Firerod => false,
                ItemType.Icerod => false,
                ItemType.Bombos => false,
                ItemType.Ether => false,
                ItemType.Quake => false,
                ItemType.Lamp => false,
                ItemType.Hammer => false,
                ItemType.Shovel => false,
                ItemType.Flute => false,
                ItemType.Bugnet => SCAM,
                ItemType.Book => false,
                ItemType.Bottle => SCAM,
                ItemType.Somaria => false,
                ItemType.Byrna => SCAM,
                ItemType.Cape => false,
                ItemType.Mirror => false,
                ItemType.Boots => false,
                ItemType.ProgressiveGlove => false,
                ItemType.Flippers => false,
                ItemType.MoonPearl => false,
                ItemType.HalfMagic => false,
                ItemType.Missile => false,
                ItemType.Super => false,
                ItemType.PowerBomb => false,
                ItemType.Grapple => false,
                ItemType.XRay => SCAM,
                ItemType.Charge => false,
                ItemType.Ice => false,
                ItemType.Wave => false,
                ItemType.Spazer => false,
                ItemType.Plasma => false,
                ItemType.Varia => false,
                ItemType.Gravity => false,
                ItemType.Morph => false,
                ItemType.Bombs => false,
                ItemType.SpringBall => false,
                ItemType.ScrewAttack => false,
                ItemType.HiJump => false,
                ItemType.SpaceJump => false,
                ItemType.SpeedBooster => false,
                _ => SCAM
            };
        }

        private byte[] GenerateRom(SMZ3.Generation.Randomizer randomizer, out SeedData seed)
        {
            var options = Options.ToDictionary();
            seed = randomizer.GenerateSeed(options, Options.Seed.Seed, CancellationToken.None);

            byte[] rom;
            using (var smRom = File.OpenRead(Options.General.SMRomPath))
            using (var z3Rom = File.OpenRead(Options.General.Z3RomPath))
            {
                rom = Rom.CombineSMZ3Rom(smRom, z3Rom);
            }

            using (var ips = GetType().Assembly.GetManifestResourceStream("Randomizer.App.zsm.ips"))
            {
                Rom.ApplyIps(rom, ips);
            }
            Rom.ApplySeed(rom, seed.Worlds[0].Patches);

            Options.Seed.SamusSprite.ApplyTo(rom);
            Options.Seed.LinkSprite.ApplyTo(rom);
            return rom;
        }

        private bool EnableMsu1Support(byte[] rom, string romPath)
        {
            var msuPath = Options.Seed.Msu1Path;
            if (!File.Exists(msuPath))
                return false;

            var romDrive = Path.GetPathRoot(romPath);
            var msuDrive = Path.GetPathRoot(msuPath);
            if (!romDrive.Equals(msuDrive, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Due to technical limitations, the MSU-1 " +
                    "pack and the ROM need to be on the same drive. MSU-1 " +
                    "support cannot be enabled. Yell at me on Discord if " +
                    "this is imortant to you and you need this now.\nPlease" +
                    "move or copy the MSU-1 files to somewhere on " + romDrive + ".",
                    "SMZ3 Cas’ Randomizer", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            using (var ips = GetType().Assembly.GetManifestResourceStream("Randomizer.App.msu1-v6.ips"))
            {
                Rom.ApplyIps(rom, ips);
            }

            var romFolder = Path.GetDirectoryName(romPath);
            var msuFolder = Path.GetDirectoryName(msuPath);
            var romBaseName = Path.GetFileNameWithoutExtension(romPath);
            var msuBaseName = Path.GetFileNameWithoutExtension(msuPath);
            foreach (var msuFile in Directory.EnumerateFiles(msuFolder, $"{msuBaseName}*"))
            {
                var fileName = Path.GetFileName(msuFile);
                var suffix = fileName.Replace(msuBaseName, "");

                var link = Path.Combine(romFolder, romBaseName + suffix);
                NativeMethods.CreateHardLink(link, msuFile, IntPtr.Zero);
            }

            return true;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var romPath = SaveRomToFile();
            Process.Start(new ProcessStartInfo
            {
                FileName = romPath,
                UseShellExecute = true
            });
        }

        private void GenerateRomButton_Click(object sender, RoutedEventArgs e)
        {
            var path = SaveRomToFile();
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Options.General.Validate())
            {
                MessageBox.Show(this, "If this is your first time using the randomizer," +
                    " there are some required options you need to configure before you " +
                    "can start playing randomized SMZ3 games. Please do so now.",
                    "SMZ3 Cas’ Randomizer", MessageBoxButton.OK, MessageBoxImage.Information);
                OptionsMenuItem_Click(this, new RoutedEventArgs());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Options.Save(_optionsPath);
            }
            catch
            {
                // Oh well
            }
        }

        private void OptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var optionsDialog = new OptionsWindow(Options.General);
            optionsDialog.ShowDialog();
        }

        private void BrowseMsu1PathButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter = "MSU-1 files (*.msu)|*.msu|All files (*.*)|*.*",
                FileName = Options.Seed.Msu1Path,
                Title = "Browse MSU-1 file - SMZ3 Cas’ Randomizer"
            };

            if (openFileDialog.ShowDialog(this) == true)
            {
                Msu1Path.Text = openFileDialog.FileName;
            }
        }

        private void GenerateStatsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var options = Options.ToDictionary();
            var randomizer = new SMZ3.Generation.Randomizer();

            const int numberOfSeeds = 10000;
            var progressDialog = new ProgressDialog(this, $"Generating {numberOfSeeds} seeds...");
            var stats = new ConcurrentDictionary<string, int>();
            var itemCounts = new ConcurrentDictionary<(int itemId, int locationId), int>();
            var ct = progressDialog.CancellationToken;
            var finished = false;
            var genTask = Task.Run(() =>
            {
                var i = 0;
                Parallel.For(0, numberOfSeeds, (iteration, state) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var seed = randomizer.GenerateSeed(options, null, ct);

                    ct.ThrowIfCancellationRequested();
                    GatherStats(stats, seed);
                    AddToMegaSpoilerLog(itemCounts, seed);

                    var seedsGenerated = Interlocked.Increment(ref i);
                    progressDialog.Report(seedsGenerated / (double)numberOfSeeds);
                });

                finished = true;
                progressDialog.Dispatcher.Invoke(progressDialog.Close);
            }, ct);

            progressDialog.StartTimer();
            var result = progressDialog.ShowDialog();
            try
            {
                genTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Any(x => !x.GetType().IsAssignableTo(typeof(OperationCanceledException))))
                    throw;
            }

            if (finished)
            {
                ReportStats(stats, itemCounts, numberOfSeeds);
                WriteMegaSpoilerLog(itemCounts);
            }
        }

        private void AddToMegaSpoilerLog(ConcurrentDictionary<(int itemId, int locationId), int> itemCounts, SeedData seed)
        {
            foreach (var location in seed.Worlds[0].World.Locations)
            {
                itemCounts.Increment(((int)location.Item.Type, location.Id));
            }
        }

        private void GatherStats(ConcurrentDictionary<string, int> stats, SeedData seed)
        {
            var world = seed.Worlds.Single();

            if (IsScam(world.World.InnerMaridia.ShaktoolItem.Item.Type))
                stats.Increment("Shaktool betrays you");

            if (IsScam(world.World.LightWorldNorthEast.ZorasDomain.Zora.Item.Type))
                stats.Increment("Zora is a scam");

            if (IsScam(world.World.DarkWorldNorthEast.Catfish.Item.Type))
                stats.Increment("Catfish is a scamfish");

            if (world.World.BlueBrinstar.MorphBall.Item.Type == ItemType.Morph)
                stats.Increment("The Morph Ball is in its original location");

            if (world.World.GanonsTower.MoldormChest.Item.Type.IsInCategory(ItemCategory.Metroid))
                stats.Increment("The GT Moldorm chest contains a Metroid item");
        }

        private void WriteMegaSpoilerLog(ConcurrentDictionary<(int itemId, int locationId), int> itemCounts)
        {
            var items = Enum.GetValues<ItemType>().ToDictionary(x => (int)x);
            var locations = new SMZ3.World(new Config(), "", 0, "").Locations;

            var itemLocations = items.Values
                .Where(item => itemCounts.Keys.Any(x => x.itemId == (int)item))
                .ToDictionary(
                    keySelector: item => item.GetDescription(),
                    elementSelector: item => itemCounts.Where(x => x.Key.itemId == (int)item)
                        .OrderByDescending(x => x.Value)
                        .ThenBy(x => locations.Single(y => y.Id == x.Key.locationId).ToString())
                        .ToDictionary(
                            keySelector: x => locations.Single(y => y.Id == x.Key.locationId).ToString(),
                            elementSelector: x => x.Value)
            );

            // Area > region > location
            var locationItems = locations.Select(location => new
            {
                Area = location.Region.Area,
                Name = location.ToString(),
                Items = itemCounts.Where(x => x.Key.locationId == location.Id)
                        .OrderByDescending(x => x.Value)
                        .ThenBy(x => x.Key.itemId)
                        .ToDictionary(
                            keySelector: x => items[x.Key.itemId].GetDescription(),
                            elementSelector: x => x.Value)
            })
                .GroupBy(x => x.Area, x => new { x.Name, x.Items })
                .ToDictionary(x => x.Key, x => x.ToList());

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(new
            {
                ByItem = itemLocations,
                ByLocation = locationItems
            }, options);

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SMZ3CasRandomizer", "item_generation_stats.json");
            File.WriteAllText(path, json);

            var startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }

        private void ReportStats(IDictionary<string, int> stats,
            ConcurrentDictionary<(int itemId, int locationId), int> itemCounts, int total)
        {
            var message = new StringBuilder();
            message.AppendLine($"If you were to play {total} seeds:");
            foreach (var key in stats.Keys)
            {
                var number = stats[key];
                var percentage = number / (double)total;
                message.AppendLine($"- {key} {number} time(s) ({percentage:P1})");
            }
            message.AppendLine();
            message.AppendLine($"Morph ball is in {UniqueLocations(ItemType.Morph)} unique locations.");

            MessageBox.Show(this, message.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Information);

            int UniqueLocations(ItemType item)
            {
                return itemCounts.Keys
                    .Where(x => x.itemId == (int)item)
                    .Select(x => x.locationId)
                    .Count();
            }
        }
    }
}