using HSCheckpoint;
using HSCheckpoint.Events;
using HSCheckpoint.GameFiles;
using HSCheckpoint.GameObjects;
using HSCheckpoint.Mem;
using System.Diagnostics;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

// Check of de speler in freemode zit, dan mag er niks gedaan worden
// Wanneer de speler de eindbaas verslaat dan wordt het nu niet gesaved.
// Dit kan opgelost worden door te kijken of het hoofdmenu geopend is, als die van 0 naar 1 is gegaan en de speler komt niet vanuit de abyss dan heeft hij m ook behaald
class Program
{
    private const string PROCESS_NAME           = "HalfSwordUE5-Win64-Shipping";
    private const string MODULE_NAME            = "HalfSwordUE5-Win64-Shipping.exe";

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

        Console.WriteLine("{0} v{1} Started at {2}",
            AppDomain.CurrentDomain.FriendlyName,
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            DateTime.Now.ToString());

        new Program(proc).Run();
        
        Console.WriteLine("Shutting down...");
    }

    private readonly string saveFilePath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData")!, "HalfSwordUE5\\Saved\\SaveGames\\");
    private readonly Process proc;
    private readonly SaveWatcher watcher;
    private readonly ProcessMemory procMem;
    private readonly Player player;

    private List<IUpdatable> updatables = new();
    private IntPtr moduleBase;
    private int prevRank = -1;

    public Program(Process proc)
    {
        this.proc = proc;
        
        // Monitor save files for changes
        watcher = new SaveWatcher(saveFilePath);
        watcher.EquipmentDeleted += Watcher_EquipmentDeleted;
        watcher.GauntletChanged += Watcher_GauntletChanged;

        // Attach to process memory
        procMem = ProcessMemory.Attach(proc.Id);

        // Get module base address
        moduleBase = procMem.GetModuleBaseAddress(MODULE_NAME);

        // Add memory watcher to check for changes
        MemoryWatcher<bool> abyssWatcher = new("IsInAbyss", procMem, new PlayerOffsets(moduleBase).IsInAbyss);
        abyssWatcher.ValueChanged += IsInAbyss_ValueChanged;

        // Add to update list
        updatables.Add(abyssWatcher);

        player = new Player(procMem, moduleBase);
    }
 
    public void Run()
    {
        prevRank = player.Rank;

        // Set initial save
        SaveData.Instance.BackupSaveData();

        Console.WriteLine("Press q to quit");
        ConsoleKey keyPressed = ConsoleKey.None;
        do
        {
            if (proc == null || proc.HasExited) break;

            GameUpdate();

            if (Console.KeyAvailable)
                keyPressed = Console.ReadKey().Key;

            Task.Delay(100).Wait();
        } while (keyPressed != ConsoleKey.Q);
    }
    private void GameUpdate()
    {
        updatables.ForEach(u => u.Update());
       
        //
    }

    private void IsInAbyss_ValueChanged(object? sender, MemoryChangedEventArgs<bool> e)
    {
        if (e.NewValue)
        {
            Console.WriteLine("Player is in abyss");
            // Replace the wiped saves with the backups
            try
            {
                watcher.EventsEnabled = false;
                SaveData.Instance.LoadSaveProgress(2);
                watcher.EventsEnabled = true;
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        else Console.WriteLine( "Player is no longer in abyss");
    }

  
    private void Watcher_GauntletChanged(object? sender, EventArgs e)
    {
        Console.WriteLine("Gauntlet progress changed {0}", DateTime.UtcNow.ToString());

        if (player.IsInAbyss) return; // Never save equipment and progress when in abyss

        // Sla laatste 5 saves op. Kijk vervolgens bij het vervangen naar de save die tenminste 10 secoonden geleden verwijderd is zodat we geen lege save pakken

        int rank = player.Rank;
        if (rank <= prevRank)
        {
            Console.WriteLine("Player gave up or died, do not use save");
        }
        else
        {
            Console.WriteLine("Player won, save data");
            SaveData.Instance.BackupSaveData();
        }

        prevRank = rank;
        Console.WriteLine("Player rank = {0}", player.Rank);
    }

    /// <summary>
    /// This gets triggered when the player equipment file is deleted.
    /// The game does this when the player dies.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Empty args</param>
    private void Watcher_EquipmentDeleted(object? sender, EventArgs e)
    {
        //await Task.Delay(1000); // Delay to ensure the game has finished writing
        Console.WriteLine("copying now! {0}", DateTime.UtcNow.ToString());
        // Replace the wiped saves with the backups

        watcher.EventsEnabled = false;
        SaveData.Instance.LoadSaveProgress(2);
        watcher.EventsEnabled = true;
    }
}   