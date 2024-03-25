using System.IO;
using Newtonsoft.Json;
using FlagsmithEngine.Environment.Models;

namespace OfflineHandler
{
    public interface IFileManager
    {
        StreamReader StreamReader(string path);
    }

    public class FileManager : IFileManager
    {
        public StreamReader StreamReader(string path)
        {
            return new StreamReader(path);
        }
    }

    public abstract class BaseOfflineHandler
    {
        public abstract EnvironmentModel GetEnvironment();
    }

    public class LocalFileHandler : BaseOfflineHandler
    {
        private readonly EnvironmentModel Environment;
        private readonly IFileManager _fileManager;

        public LocalFileHandler(string environmentDocumentPath, IFileManager fileManager = null)
        {
            _fileManager = fileManager ?? new FileManager();
            using (StreamReader r = _fileManager.StreamReader(environmentDocumentPath))
            {
                string json = r.ReadToEnd();
                Environment = JsonConvert.DeserializeObject<EnvironmentModel>(json);
            }
        }

        public override EnvironmentModel GetEnvironment()
        {
            return Environment;
        }
    }
}

