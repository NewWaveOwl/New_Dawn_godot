#nullable disable warnings
using NewDawn.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using IOPath = System.IO.Path;

namespace NewDawn;

/// <summary>Legacy NewGameSetup data flow, with Godot presentation adapters.</summary>
public class NewGameSetupData
{
    private SetupGameMode? _selectedMode;
    private string? _selectedMapFileName;
    private bool _useCustomUnits = false;
    private string _selectedUnitTemplateBaseName = "DefaultUnits";
    private readonly ObservableCollection<UnitTemplateItem> _unitTemplateFiles = new();
    private readonly ObservableCollection<MapFileItem> _mapFiles = new();
    private int _selectedTemplateIndex = -1;
    public bool IsInitializing { get; set; }
    public IReadOnlyList<MapFileItem> Maps => _mapFiles;
    public IReadOnlyList<UnitTemplateItem> Templates => _unitTemplateFiles;
    public bool UseCustomUnits => _useCustomUnits;
    public SetupGameMode? SelectedMode => _selectedMode;
    public string SelectedTemplateName => _selectedUnitTemplateBaseName;
    public string SelectedTemplateTime { get; private set; } = "";
    public int SelectedMapIndex { get; private set; } = -1;
    public int SelectedTemplateIndex {
        get => _selectedTemplateIndex;
        set {
            if (_selectedTemplateIndex == value) return;
            _selectedTemplateIndex = value;
            if (value >= 0 && value < Templates.Count) {
                UnitTemplateItem item = Templates[value];
                _selectedUnitTemplateBaseName = IOPath.GetFileNameWithoutExtension(item.FileName);
                SelectedTemplateTime = "Last modified: " + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
            }
        }
    }
    public event Action<string, string>? Notice;
    private void Message(string text, string title = "") => Notice?.Invoke(text, title);

    public void Initialize()
    {
        IsInitializing = true;
        LoadUnitTemplateFilesOnce();
        LoadMapFilesOnce();
        _selectedMode = SetupGameMode.KillCommander;
        GameModeChecked(true);
        if (_mapFiles.Count > 0) SelectMap(0);
        IsInitializing = false;
        UnitsModeChecked(false);
    }

    public void SelectMap(int index)
    {
        SelectedMapIndex = index;
        if (index >= 0 && index < Maps.Count) _selectedMapFileName = Maps[index].FileName;
        else _selectedMapFileName = null;
    }

    public void UnitsModeChecked(bool custom)
    {
        if (IsInitializing) return;
        _useCustomUnits = custom;
        if (!_useCustomUnits) _selectedUnitTemplateBaseName = "DefaultUnits";
        else if (_unitTemplateFiles.Count > 0 && SelectedTemplateIndex < 0) SelectedTemplateIndex = 0;
    }

    public void GameModeChecked(bool killCommander)
    {
        if (killCommander) _selectedMode = SetupGameMode.KillCommander;
        // The legacy else-if is commented out: the following assignment is unconditional.
        _selectedMode = SetupGameMode.Contest;
    }
public enum SetupGameMode
        {
            KillCommander,
            Contest
        }

public struct MapFileItem
        {
            private string _fileName;
            private string _displayName;
            private DateTime _lastWriteTime;

            public string FileName
            {
               get { return _fileName; }
                set { _fileName = value; }

            }
            public string DisplayName
            {
                get { return _displayName; }
                set { _displayName = value; }

            }
            public DateTime LastWriteTime
            {
                get { return _lastWriteTime; }
                set { _lastWriteTime = value; }
            }
            public MapFileItem(string fileName, string displayName, DateTime lastWriteTime)
            {
                FileName = fileName;
                DisplayName = displayName;
                LastWriteTime = lastWriteTime;
            }
        }

public struct UnitTemplateItem
        {
            private string _fileName;
            private string _displayName;
            private DateTime _lastWriteTime;

            public string FileName
            {
                get { return _fileName; }
                set { _fileName = value; }
            }
            public string DisplayName
            {
                get { return _displayName; }
                set { _displayName = value; }

            }
            public DateTime LastWriteTime
            {
                get { return _lastWriteTime; }
                set { _lastWriteTime = value; }
            }

            public UnitTemplateItem(string fileName, string displayName, DateTime lastWriteTime)
            {
                FileName = fileName;
                DisplayName = displayName;
                LastWriteTime = lastWriteTime;
            }
        }

private void LoadMapFilesOnce()
        {
            _mapFiles.Clear();

            string dir = Map.GetMapsDirectory();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string[] filePaths = Directory.GetFiles(dir, "*.map", SearchOption.TopDirectoryOnly);

            string[] ordered = filePaths
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToArray();

            foreach (string filePath in ordered)
            {
                FileInfo fi = new FileInfo(filePath);

                string fileName = fi.Name; // with extension
                string displayName = IOPath.GetFileNameWithoutExtension(fi.Name);

                MapFileItem item = new MapFileItem(fileName, displayName, fi.LastWriteTime);
                _mapFiles.Add(item);
            }
        }

public void LoadUnitTemplateFilesOnce()
        {
            _unitTemplateFiles.Clear();

            string dir = SaveAndLoaderHandler.GetUnitTemplatesDirectoryPath();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string[] filePaths = Directory.GetFiles(dir, "*.csv", SearchOption.TopDirectoryOnly);

            string[] ordered = filePaths
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToArray();

            for (int i = 0; i < ordered.Length; i++)
            {
                FileInfo fi = new FileInfo(ordered[i]);

                string fileName = fi.Name;
                string displayName = IOPath.GetFileNameWithoutExtension(fi.Name);

                _unitTemplateFiles.Add(new UnitTemplateItem(fileName, displayName, fi.LastWriteTime));
            }

            // Default select newest 
            if (_unitTemplateFiles.Count > 0 && SelectedTemplateIndex < 0)
            {
                SelectedTemplateIndex = 0;
            }
        }

public Game? BuildGame() //Play Button
        {

            if (_selectedMode == null)   _selectedMode = SetupGameMode.KillCommander;

            if (string.IsNullOrWhiteSpace(_selectedMapFileName))
            {
                if (_mapFiles.Count > 0)
                    _selectedMapFileName = _mapFiles[0].FileName;
                else
                {
                    Message("No maps found. Save a map first.");
                    return null;
                }
            }


            Map loadedMap;

            try
            {
                // If LoadMap expects NO extension ensure 
                string mapBaseName = IOPath.GetFileNameWithoutExtension(_selectedMapFileName);
                loadedMap = SaveAndLoaderHandler.LoadMap(mapBaseName);
            }
            catch (Exception ex)
            {
                Message($"Failed to load map.\n\n{ex.Message}", "New Game");
                return null;
            }
            //this part for settuping new game can be moved in another place
            // Create new game
            Player p1 = new Player(startingAlloy: 30, startingFuel: 6);
            Player p2 = new Player(startingAlloy: 45, startingFuel: 6);

            Game game = new Game(p1, p2, loadedMap);

            if (_useCustomUnits)
            {
                if (string.IsNullOrWhiteSpace(_selectedUnitTemplateBaseName))
                {
                    Message("Select a custom unit template file (CSV) first.");
                    return null;
                }

                game.UnitTemplateFileName = _selectedUnitTemplateBaseName;
            }
            else
            {
                game.UnitTemplateFileName = "DefaultUnits";
            }

            //commander placement 
            game.InitializeGame(loadedMap, p1, p2, player1StartsFirst: true);

            // TODO: apply _selectedMode to Game

            return game;
        }
}
