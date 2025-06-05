/*
HI! This program was written by @magicmathman1 on Discord.
This is my first program I've ever written in C#, so please be kind, and
I appologize for the less-than-ideal code.

This is completely open-source under the GNU General Public License v3 allowing you to
    - Use this software,
    - Modify this software,
    - Redistribute this software,
free of charge, with the sole condition that all modified and/or
redistributed versions utilize the same license.(See LICENSE for license.)

Credit is not required, but appreciated!
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data;

namespace RMMS_CLI;

public class Program {
    public static List<Exception> Errors { get; set; } = new();
    public static int PageNo { get; set; } = 0; // Current Page
    public static int CursorPageNo { get; set; } = 0; // Current SELECTED Page
    public static int SelectedMod { get; set; } = 1; // Which mod is selected?
    public static int SelectedModCategory { get; set; } = 0;

    public static HashSet<string> HighImpactIds { get; set; } = new();
    public static HashSet<string> BannedIds { get; set; } = new();

    public static int CheckBoxes { get; set; } = Enum.GetValues<RMMSCategories>().Length - 1;

    public static ModCategorySaveData UserData { get; set; } = IO.LoadUserData();
    public static Settings UserSettings { get; set; } = IO.LoadSettings();

    static void Main() {
        bool programQuit = false; // If true, program will end.
        bool redraw = true;

        (HighImpactIds, BannedIds) = IO.LoadModCategories();

        int lastWidth = Console.WindowWidth;

        Console.Clear();
        if (UserSettings.ShowLogo) UI.ShowAsciiTitle();

        foreach (string i in IO.GetUserLocalMods()) {
            _ = new Mod(i);
        }

        Console.WriteLine("| Press Any Key...\n");

        Console.ReadKey(true);
        Console.WriteLine("");

        while (!programQuit) {
            if (Console.WindowWidth != lastWidth) {
                lastWidth = Console.WindowWidth;

                redraw = true;
            }

            if (Console.KeyAvailable) {
                var key = Console.ReadKey(true).Key;

                if (true) {
                    switch (key) {
                        case ConsoleKey.UpArrow:
                            if (PageNo == 0) {
                                if (SelectedMod > 0) SelectedMod--;
                            }
                            redraw = true;
                            break;

                        case ConsoleKey.DownArrow:
                            if (PageNo == 0) {
                                if (SelectedMod < Mod.Mods.Count) SelectedMod++;
                            }
                            redraw = true;
                            break;

                        case ConsoleKey.LeftArrow:
                            if (SelectedModCategory > 0) SelectedModCategory--;
                            redraw = true;
                            break;

                        case ConsoleKey.RightArrow:
                            if (SelectedModCategory < CheckBoxes - 1) SelectedModCategory++;
                            redraw = true;
                            break;

                        case ConsoleKey.Escape:
                            programQuit = true;
                            break;

                        default:
                            break;
                    }
                }

                switch (key) {
                    case ConsoleKey.Z:
                        SelectBox();
                        redraw = true;
                        break;

                    default:
                        break;
                } 

                while (Console.KeyAvailable)
                    Console.ReadKey(true);
            }

            if (redraw) {
                Console.Clear();
                if (UserSettings.ShowLogo) UI.ShowAsciiTitle();

                Console.WriteLine("");

                switch (PageNo) {
                    case 0:
                        UI.DrawPageList();
                        UI.DrawModTable();
                        break;

                    case 1:
                        UI.DrawHelpMenu();
                        UI.DrawPageList();
                        break;

                    default:
                        break;
                }

                redraw = false;
            }

            Thread.Sleep(32);
        }
    }

    public static void SelectBox() {
        switch (PageNo) {
            case 0:
                if (SelectedMod > 0) {
                    Mod.Mods[SelectedMod - 1].Categories ^= (RMMSCategories)(1 << SelectedModCategory); // Most proud line of code here :D

                    if ((Mod.Mods[SelectedMod - 1].Categories & (RMMSCategories.Allowed | RMMSCategories.Banned)) == (RMMSCategories.Allowed | RMMSCategories.Banned)) {
                        Mod.Mods[SelectedMod - 1].Categories &= ~(RMMSCategories.Allowed | RMMSCategories.Banned) | (RMMSCategories)(1 << SelectedModCategory);
                    }
                }
                break;

            default:
                break;
        }

        if (SelectedMod == 0) {
            if (SelectedModCategory != 2) {
                PageNo = SelectedModCategory;
            } else {
                IO.SaveData();
                PageNo = 0;
            }
        }
    }
}

[Flags]
public enum RMMSCategories {
    Unspecified = 0,
    Allowed     = 1 << 0, // 1
    HighImpact  = 1 << 1, // 2
    Banned      = 1 << 2, // 4
}

public class ModCategorySaveData {
    [JsonPropertyName("Unspecified")]
    public HashSet<string> Unspecified { get; set; } = new();

    [JsonPropertyName("Ignored")]
    public HashSet<string> Ignored { get; set; } = new();
}

public class Settings {
    [JsonPropertyName("ShowLogo")]
    public bool ShowLogo { get; set; } = true;
}

public class Mod {
    public static List<Mod> Mods { get; private set; } = new();

    public string? ModFile;
    public bool IsIgnored = false;
    public RMMSCategories Categories = RMMSCategories.Allowed;
    public ModInfo? ModData { get; private set; }

    public Mod(string file) {
        try {
            ModFile = file;

            string jsonString = File.ReadAllText(file + "/modinfo.json");
            ModData = JsonSerializer.Deserialize<ModInfo>(jsonString, JSON.Options);

            if (ModData == null) {
                ModData = new ModInfo();
                Program.Errors.Add(new Exception($"Failed to parse JSON from {file}"));
            }

            if (!string.IsNullOrEmpty(ModData.ID)) {
                if (Program.BannedIds.Contains(ModData.ID)) Categories = RMMSCategories.Banned; // This line must be first since it will override Allowed, not combine it.

                if (Program.HighImpactIds.Contains(ModData.ID)) Categories |= RMMSCategories.HighImpact;
                if (Program.UserData.Unspecified.Contains(ModData.ID)) Categories = RMMSCategories.Unspecified;

                if (this.ModData.ID != null && this.ModData.ModName != null) Mods.Add(this);
            }
        } catch (Exception err) {
            Program.Errors.Add(err);
            ModData = new ModInfo();
        }
    }
}

public static class IO {
    private static readonly string UserModsFolder = @"C:\Program Files (x86)\Steam\steamapps\common\Rain World\RainWorld_Data\StreamingAssets\mods";
    private static readonly string WorkshopModsFolder = @"C:\Program Files (x86)\Steam\steamapps\workshop\content\312520";
    private static readonly string HighImpactModsPath = @"C:\Program Files (x86)\Steam\steamapps\common\Rain World\RainWorld_Data\StreamingAssets\meadow-highimpactmods.txt";
    private static readonly string BannedModsPath = @"C:\Program Files (x86)\Steam\steamapps\common\Rain World\RainWorld_Data\StreamingAssets\meadow-bannedmods.txt";

    public static ModCategorySaveData LoadUserData() {
        return JsonSerializer.Deserialize<ModCategorySaveData>(File.ReadAllText("userdata.json"), JSON.Options);
    }

    public static Settings LoadSettings() {
        return JsonSerializer.Deserialize<Settings>(File.ReadAllText("settings.json"), JSON.Options);
    }

    public static void SaveData() {
        var userdata = new ModCategorySaveData {};

        HashSet<string> highImpactData = LoadModIdList(HighImpactModsPath);
        HashSet<string> bannedData = LoadModIdList(BannedModsPath);

        foreach (Mod i in Mod.Mods) {
            if (i.Categories == RMMSCategories.Unspecified) {
                userdata.Unspecified.Add(i.ModData.ID);
            }

            if (i.IsIgnored) {
                userdata.Ignored.Add(i.ModData.ID);
            }

            if ((i.Categories & RMMSCategories.HighImpact) == RMMSCategories.HighImpact) {
                if (!highImpactData.Contains(i.ModData.ID)) {
                    highImpactData.Add(i.ModData.ID);
                }
            } else {
                if (highImpactData.Contains(i.ModData.ID)) {
                    highImpactData.Remove(i.ModData.ID);
                }
            }

            if ((i.Categories & RMMSCategories.Banned) == RMMSCategories.Banned) {
                if (!bannedData.Contains(i.ModData.ID)) {
                    bannedData.Add(i.ModData.ID);
                }
            } else {
                if (bannedData.Contains(i.ModData.ID)) {
                    bannedData.Remove(i.ModData.ID);
                }
            }
        }

        File.WriteAllLines(HighImpactModsPath, highImpactData);
        File.WriteAllLines(BannedModsPath, bannedData);

        string json = JsonSerializer.Serialize(userdata, JSON.Options);
        File.WriteAllText("userdata.json", json);

        UI.ShowSavedCheck = true;
    }

    public static List<string> GetUserLocalMods() {
        List<string> userMods = new();

        userMods.AddRange(Directory.GetDirectories(UserModsFolder));
        userMods.AddRange(Directory.GetDirectories(WorkshopModsFolder));

        return userMods;
    }

    public static HashSet<string> LoadModIdList(string filePath) {
        if (!File.Exists(filePath)) return new();
        return File.ReadAllLines(filePath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line))
            .ToHashSet();
    }

    public static (HashSet<string> highImpact, HashSet<string> banned) LoadModCategories() {
        return (
            LoadModIdList(HighImpactModsPath),
            LoadModIdList(BannedModsPath)
        );
    }
}

public static class UI {
    public static bool ShowSavedCheck { get; set; } = false;

    public static void DrawModTable() {
        Console.WriteLine("Local Installed Mods:");
        Console.WriteLine("");

        // Table Labels
        Console.Write("                  Mod Name");
        for (int i = 0; i < (Math.Round((double)Console.WindowWidth / 2) - 26); i++) {
            Console.Write(" ");
        }

        Console.Write("| ");
        Console.WriteLine("Mod ID");

        // Top Line
        for (int i = 0; i < Console.WindowWidth; i++) {
            Console.Write("_");
        }
        Console.WriteLine("");

        Console.Write("|  A   H   B");
        for (int l = 0; l < Console.WindowWidth - 13; l++) {
            Console.Write(" ");
        }
        Console.WriteLine("|");

        // Mods
        for (int iModIndex = 0; iModIndex < Mod.Mods.Count; iModIndex++) {
            Mod iMod = Mod.Mods[iModIndex];

            if (iMod.IsIgnored) continue;

            Console.Write("| ");

            RMMSCategories[] allCategories = Enum.GetValues<RMMSCategories>()
                .Where(e => e != RMMSCategories.Unspecified)
                .OrderBy(e => (int)e)
                .ToArray();

            for (int l = 0; l < allCategories.Length; l++) {
                RMMSCategories currentCategory = allCategories[l];

                if (Program.SelectedMod == iModIndex + 1 && l == Program.SelectedModCategory) {
                    Console.ForegroundColor = ConsoleColor.Green;
                }

                Console.Write("[");

                if ((iMod.Categories & currentCategory) == currentCategory) {
                    Console.Write("X");
                } else {
                    Console.Write(" ");
                }

                Console.Write("] ");
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.Write(iMod.ModData.ModName);
            for (int i = 0; i < (Math.Round((double)Console.WindowWidth / 2) - (iMod.ModData.ModName.Length + (Program.CheckBoxes * 4) + 2)); i++) {
                Console.Write(" ");
            }

            Console.Write("| ");
            Console.Write(iMod.ModData.ID);

            double modifier = Math.Max((iMod.ModData.ModName.Length + (Program.CheckBoxes * 4) + 2) - Math.Round((double)Console.WindowWidth / 2), 0);
            for (int i = 0; i < (Console.WindowWidth - Math.Round((double)Console.WindowWidth / 2)) - (iMod.ModData.ID.Length + 2) - 1 - modifier; i++) {
                Console.Write(" ");
            }

            Console.WriteLine("|");

            Console.ForegroundColor = ConsoleColor.White;
        }

        // Bottom Line
        Console.Write("|");
        for (int i = 0; i < Console.WindowWidth - 2; i++) {
            Console.Write("_");
        }
        Console.Write("|");

        Console.WriteLine(" ");
        Console.WriteLine("Arrow Keys - Move Selector | Z - Select | Esc - Quit\nA - Allowed\nH - High Impact\nB - Banned");
    }

    public static void ShowAsciiTitle() {
        if (Console.WindowWidth >= 76) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@" __________      ______      ______     ______      ______     ____________
|   ____   |    |      \    /      |   |      \    /      |   |            |
|  |    |  |    |       \  /       |   |       \  /       |   |     _______|
|  |____|  |    |        \/        |   |        \/        |   |            |
|         _|    |    |\      /|    |   |    |\      /|    |   |______      |
|         \     |    | \    / |    |   |    | \    / |    |    ______|     |
|    |\    \    |    |  \  /  |    |   |    |  \  /  |    |   |            |
|____| \____\   |____|   \/   |____|   |____|   \/   |____|   |____________|
        ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=  Rain Meadow Mod Sorter  =-=-=-=-=-=-=-=-=-=-=-=-=");
            Console.WriteLine("");

            Console.ResetColor();
        } else {
            Console.WriteLine("no logo :(");
        }
    }

    public static void DrawPageList() {
        if (Program.PageNo == 0) Console.ForegroundColor = ConsoleColor.Blue; else Console.ForegroundColor = ConsoleColor.White;
        if (Program.SelectedMod == 0 && Program.SelectedModCategory == 0) Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  [MODS]");
        if (Program.PageNo == 1) Console.ForegroundColor = ConsoleColor.Blue; else Console.ForegroundColor = ConsoleColor.White;
        if (Program.SelectedMod == 0 && Program.SelectedModCategory == 1) Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("     [HELP]");
        if (Program.PageNo == 2) Console.ForegroundColor = ConsoleColor.Blue; else Console.ForegroundColor = ConsoleColor.White;
        if (Program.SelectedMod == 0 && Program.SelectedModCategory == 2) Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("     [SAVE]");
        Console.ForegroundColor = ConsoleColor.White;

        if (ShowSavedCheck) {
            Console.WriteLine(" ✓ saved");
            ShowSavedCheck = false;
        } else {
            Console.WriteLine("");
        }

        Console.WriteLine("");
    }

    public static void DrawHelpMenu() {
        Console.Write("(All text above this glitched text)");
        for (int i = 0; i < Console.WindowWidth - 35; i++) {
            Console.Write("-");
        }

            Console.WriteLine("");
        Console.WriteLine("");
        Console.WriteLine(@"     Help Menu:

Controls:
    Arrow Keys - Move Selection Box
    Z - Select
    Esc - Quit Program

Mod Categories:
    A - Allowed Mods
    H - High Impact Mods
    B - Banned Mods

+----------------------------+

Rain Meadow Mod Sorter allows you to easily add mods to the Rain Meadow banned and high impact mod lists, so that you don't have to do the tedious task of doing it manually.
If your mods are not appearing in RMMS:
    1. Make sure the modinfo.json file in your mod AT LEAST has ""name"", ""id"", and ""version"". Without these, RMMS will not recognize your mod.
    (THIS DOES NOT WORK YET SORRY) 2. Make sure your mod directory is located inside of RMMS's 'settings.json' file. (exluding the default Rain World and Workshop mod directories.)

+----------------------------+

Credits:
Rain Meadow Mod Sorter - Made By magicmathman1/Resueman/whatever you want to call me :)
This project is completely open-source under GNU-GPLv3! - https://github.com/magicmathman1/RMMS
(it's kinda empty here?)

        ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Scroll Up ^");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("");
    }
}

public class ModInfo {
    [JsonPropertyName("id")]
    public string? ID {get; set;}

    [JsonPropertyName("name")]
    public string? ModName {get; set;}
}

public static class JSON {
    public static readonly JsonSerializerOptions Options = new() {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };
}

