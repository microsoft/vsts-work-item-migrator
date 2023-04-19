using System;
using System.IO;
using Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common.Config
{
    public class ConfigReaderJson : IConfigReader
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ConfigReaderJson>();

        private string FilePath;
        private string JsonText;

        public ConfigReaderJson(string filePath)
        {
            this.FilePath = filePath;
        }

        public ConfigJson Deserialize()
        {
            LoadFromFile(this.FilePath);
            return DeserializeText(this.JsonText);
        }

        public void LoadFromFile(string filePath)
        {
            try
            {
                this.JsonText = GetJsonFromFile(filePath);
            }
            catch (FileNotFoundException)
            {
                Logger.LogError("Required JSON configuration file was not found. Please ensure that this file is in the correct location.");
                throw;
            }
            catch (PathTooLongException)
            {
                Logger.LogError("Required JSON configuration file could not be accessed because the file path is too long. Please store your files for this WiMigrator application in a folder location with a shorter path name.");
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogError("Cannot read from the JSON configuration file because you are not authorized to access it. Please try running this application as administrator or moving it to a folder location that does not require special access.");
                throw;
            }
            catch (Exception)
            {
                Logger.LogError("Cannot read from the JSON configuration file. Please ensure it is formatted properly.");
                throw;
            }
        }

        public string GetJsonFromFile(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public ConfigJson DeserializeText(string input)
        {
            ConfigJson result = null;
            try
            {
                result = JsonConvert.DeserializeObject<ConfigJson>(input);
                // If not provided, default to # of processors on the computer
                if (result.Parallelism < 1)
                {
                    result.Parallelism = Environment.ProcessorCount;
                }

                // If not provided, default to # of processors on the computer
                if (result.LinkParallelism < 1)
                {
                    result.LinkParallelism = Environment.ProcessorCount;
                }
            }
            catch (Exception)
            {
                Logger.LogError("Cannot deserialize the JSON text from configuration file. Please ensure it is formatted properly.");
                throw;
            }

            return result;
        }
    }
}
