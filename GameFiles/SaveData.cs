using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.GameFiles
{
    public class SaveData
    {
        private static SaveData? instance;
        private static readonly object lockObj = new object();
        private readonly string saveFilePath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData")!, "HalfSwordUE5\\Saved\\SaveGames\\");
        private readonly string backupFilePath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData")!, "HalfSwordUE5\\Mods\\HSCheckpoint\\");
        private SaveData()
        {

        }

        public static SaveData Instance
        {
            get
            {
                lock (lockObj)
                {
                    return instance ?? (instance = new SaveData());
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="IOException"></exception>
        public void BackupSaveData()
        {
            if (!Directory.Exists(backupFilePath))
            {
                Directory.CreateDirectory(backupFilePath);
            }

            Directory.GetFiles(saveFilePath, "*.sav").ToList().ForEach(file =>
            {
                // Exclude settings
                if (file.Contains("Settings")) return;

                string targetFile = Path.Combine(backupFilePath + Path.GetFileName(file));
                File.Copy(file, targetFile, true);

                Console.WriteLine("Backing up: {0}", targetFile);
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxAttempts"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public bool LoadSaveProgress(int maxAttempts)
        {
            // Voeg een .lock bestand toe
            if (!Directory.Exists(backupFilePath))
            {
                Directory.CreateDirectory(backupFilePath);
            }

            var files = Directory.GetFiles(backupFilePath, "*.sav");
            if (files.Length == 0)
            {
                Console.WriteLine("No save data found!");
                return false;
            }

            
            foreach (string file in files)
            {
                int failures = 0;
                do
                {
                    try
                    {
                        string targetFile = Path.Combine(saveFilePath + Path.GetFileName(file));
                        File.Copy(file, targetFile, true);

                        Console.WriteLine("Loading save: {0}", targetFile);
                        break;
                    }
                    catch (IOException)
                    {
                        if (failures++ >= maxAttempts)
                        {
                            Console.WriteLine("ERROR: Failed to load savefile(s)");
                            throw;
                        }
                    }
                } while (failures++ < maxAttempts);
            }

            return true;
        }
    }
}
