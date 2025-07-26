using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HSCheckpoint.GameFiles
{

    internal class SaveWatcher
    {
        public event EventHandler? GauntletDeleted, GauntletChanged;
        public event EventHandler? EquipmentDeleted, EquipmentChanged;

        private const string PLAYER_EQUIPMENT_FILE = "SG Player Equipment.sav";
        private const string PLAYER_GAUNTLET_FILE = "SG Gauntlet Progress.sav";


        private readonly FileSystemWatcher watcher;

        private readonly Dictionary<string, DateTime> lastSavesAccessed = new Dictionary<string, DateTime>()
        {
            { PLAYER_EQUIPMENT_FILE, DateTime.MinValue },
            { PLAYER_GAUNTLET_FILE, DateTime.MinValue }
        };
        public SaveWatcher(string saveDataPath)
        {
            watcher = new FileSystemWatcher(saveDataPath);
            watcher.NotifyFilter = NotifyFilters.CreationTime
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Error += OnError;

            watcher.Filters.Add(PLAYER_EQUIPMENT_FILE);
            watcher.Filters.Add(PLAYER_GAUNTLET_FILE);
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Progress watcher watching on {0}", saveDataPath);
        }

        public bool EventsEnabled
        {
            get => watcher.EnableRaisingEvents;
            set => watcher.EnableRaisingEvents = value;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed || string.IsNullOrEmpty(e.Name))
            {
                return;
            }

            DateTime currentTime = DateTime.UtcNow;
            if (!lastSavesAccessed.TryGetValue(e.Name!, out DateTime lastSaved))
            {
                // Error, not a valid save file
                return;
            }

            TimeSpan timeDiff = currentTime - lastSaved;

            // Avoid multiple triggers for same save file
            if (timeDiff.TotalMilliseconds < 3000)
            {
                return;
            }

            lastSavesAccessed[e.Name] = currentTime;

            Console.WriteLine($"Changed: {e.FullPath}");

            GetChangedHandler(e.Name)?.Invoke(this, EventArgs.Empty);
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            string value = $"Created: {e.FullPath}";
            Console.WriteLine(value);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Name)) return;

            // Copy gauntlet save file and equipment again
            Console.WriteLine($"Deleted: {e.FullPath}");
            GetDeletedHandler(e.Name)?.Invoke(this, EventArgs.Empty);   
        }

        private void OnError(object sender, ErrorEventArgs ex)
        {
            if(ex != null)
            {
                Console.WriteLine($"Message: {ex.GetException().Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.GetException().StackTrace);
                Console.WriteLine();
            }
        }

        private EventHandler? GetChangedHandler(string filename) => filename switch
        {
            PLAYER_EQUIPMENT_FILE => EquipmentChanged,
            PLAYER_GAUNTLET_FILE => GauntletChanged,
            _ => null
        };

        private EventHandler? GetDeletedHandler(string filename) => filename switch
        {
            PLAYER_EQUIPMENT_FILE => EquipmentDeleted,
            PLAYER_GAUNTLET_FILE => GauntletDeleted,
            _ => null
        };
    }
}
