using HSCheckpoint;
using HSCheckpoint.Events;
using HSCheckpoint.GameFiles;
using HSCheckpoint.GameObjects;
using HSCheckpoint.Mem;
using HSCheckpoint.Offsets;
using System.Diagnostics;
using System.Reflection;

class Program
{
    private const string PROCESS_NAME           = "HalfSwordUE5-Win64-Shipping";
    private const string MODULE_NAME            = "HalfSwordUE5-Win64-Shipping.exe";
    private const string GAME_VERSION           = "5.4.4.0";

    private static Process? GetGameProces(string procName)
    {
        Process[] procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(procName));
        return procs.Length > 0 ? procs.First() : null;
    }

    public static void Main(string[] args)
    {
        Process? proc = GetGameProces(PROCESS_NAME);
        // Get game process
        if (proc == null)
        {
            Console.WriteLine("WARNING: Game is not running, make sure that Half sword has started and restart this application.");
            Console.WriteLine("\nPress any key to quit.");
            Console.ReadKey();
            return;
        }

        string? fileName = proc?.MainModule?.FileName ?? string.Empty;
        if (!string.IsNullOrEmpty(fileName))
        {
            var fileInfo = FileVersionInfo.GetVersionInfo(fileName);

            string fileVersion = $"{fileInfo.FileMajorPart}.{fileInfo.FileMinorPart}.{fileInfo.FileBuildPart}.{fileInfo.FilePrivatePart}";

            if (fileVersion != GAME_VERSION)
                Console.WriteLine("WARNING: This mod is created for game version {0}, current game version is: {1}", GAME_VERSION, fileVersion);
        }

        Console.WriteLine("{0} v{1} Started at {2}",
            AppDomain.CurrentDomain.FriendlyName,
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            DateTime.Now.ToString());

        new Program(proc!).Run();
        
        Console.WriteLine("Shutting down...");
    }


    private readonly Process proc;
    private readonly ProcessMemory procMem;
    private readonly Player player;
    private readonly GameState gameState;

    private List<IUpdatable> updatables = new();
    private IntPtr moduleBase;

    private int rank = 0;
    private int points = 0;

    public Program(Process proc)
    {
        this.proc = proc;
        
        // Monitor save files for changes
        //watcher = new SaveWatcher(saveFilePath);
        //watcher.EquipmentDeleted += Watcher_EquipmentDeleted;
        //watcher.GauntletChanged += Watcher_GauntletChanged;

        // Attach to process memory
        procMem = ProcessMemory.Attach(proc.Id);

        // Get module base address
        moduleBase = procMem.GetModuleBaseAddress(MODULE_NAME);

        // Watch rank (level)
        MemoryWatcher<int> rankWatcher = new("Points", procMem, new HalfSwordGameMode_Offsets(moduleBase).CurrentPoints);
        rankWatcher.ValueChanged += PointsChanged;

        // Watch prestige
        MemoryWatcher<int> prestigeWatcher = new("Rank", procMem, new HalfSwordGameMode_Offsets(moduleBase).AvailableRank);
        prestigeWatcher.ValueChanged += RankChanged;

        // Add to update list
        updatables.AddRange(rankWatcher, prestigeWatcher);

        player = new Player(procMem, moduleBase);
        gameState = new GameState(procMem, moduleBase);
    }

    private void RankChanged(object? sender, MemoryChangedEventArgs<int> e)
    {
        // Player ranked up, save
        if (e.NewValue > e.OldValue && !player.IsInAbyss())
        {
            Console.WriteLine("Rank up: Backing up save data {0}", DateTime.Now.ToString());
            
            try { SaveData.Instance.BackupSaveData(); }
            catch (IOException ex) { Console.WriteLine("ERROR: Failed to backup save data. {0}", ex.Message); }

            rank = e.NewValue;
        }
        if (e.NewValue < e.OldValue)
        {
            Console.WriteLine("Rank lost, loading backup... {0}", DateTime.Now.ToString());

            player.Rank = rank;
            player.Points = points;
            try { SaveData.Instance.LoadSaveProgress(2); }
            catch (IOException ex) { Console.WriteLine("ERROR: Failed to load backup save. {0}", ex.Message); }
        }
    }

    private void PointsChanged(object? sender, MemoryChangedEventArgs<int> e)
    {
        if (!player.GauntledModeEnabled || player.IsInAbyss())
        {
            Console.WriteLine("Gauntled mode disabled, skipping PointsChanged event");
            return;
        }

        int pointDiff = (e.OldValue - e.NewValue);
        // Player won and points up
        if (e.NewValue > e.OldValue)
        {
            Console.WriteLine("Level up {0} -> {1}: Backing up save data {2}", e.OldValue, e.NewValue, DateTime.Now.ToString());
            try { SaveData.Instance.BackupSaveData(); }
            catch (IOException ex) { Console.WriteLine("ERROR: Failed to backup save data. {0}", ex.Message); }
        }
        // Player lost 1 point, player gave up
        else if ((pointDiff >= 1 && pointDiff <= 2) && !player.IsDead) // avoid increasing points when player ranks up.
        {
            Console.WriteLine("Player gave up / lost, fixing points: {0}", DateTime.Now.ToString());
            player.Points = e.OldValue;
            e.ValueModified = true; // Mark value as modified so event doesnt trigger again for this change
        }
    }

    public void Run()
    {
        try
        {
            SaveData.Instance.BackupSaveData();
            rank = player.Rank;
            points = player.Points;
        }
        catch (IOException ex)
        {
            Console.WriteLine("Failed to save data, {0}", ex.ToString());
            return;
        }

        Console.WriteLine("Press q to quit");
        ConsoleKey keyPressed = ConsoleKey.None;
        do
        {
            if (proc == null || proc.HasExited) break;

            GameUpdate();

            if (Console.KeyAvailable)
                keyPressed = Console.ReadKey().Key;

            Task.Delay(1).Wait();
        } while (keyPressed != ConsoleKey.Q);
    }

    private void GameUpdate()
    {
        updatables.ForEach(u => u.Update());
    }

    /// <summary>
    /// This gets triggered when the player equipment file is deleted.
    /// The game does this when the player dies.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Empty args</param>
    //private void Watcher_EquipmentDeleted(object? sender, EventArgs e)
    //{
    //    //await Task.Delay(1000); // Delay to ensure the game has finished writing
    //    Console.WriteLine("copying now! {0}", DateTime.UtcNow.ToString());
    //    // Replace the wiped saves with the backups

    //    watcher.EventsEnabled = false;
    //    SaveData.Instance.LoadSaveProgress(2);
    //    watcher.EventsEnabled = true;
    //}
}   