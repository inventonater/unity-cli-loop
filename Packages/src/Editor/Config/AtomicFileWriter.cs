using System.IO;
using UnityEngine;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Writes files atomically via temp file + rename to prevent
    /// external processes (e.g. CLI) from reading partially-written data.
    /// </summary>
    internal static class AtomicFileWriter
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Writes content atomically: .tmp → .bak → target.
        /// Serialized to prevent concurrent callers from racing on the
        /// same temp/backup/target file set.
        /// </summary>
        public static void Write(string filePath, string content)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath), "filePath must not be null or empty");
            Debug.Assert(content != null, "content must not be null");

            lock (_lock)
            {
                string tempFilePath = filePath + ".tmp";
                string backupFilePath = filePath + ".bak";
                File.WriteAllText(tempFilePath, content);

                // .NET Framework 4.7.1 lacks File.Move(src, dst, overwrite), so we
                // rotate old → .bak before moving .tmp → target to minimize the window
                // where the target file is absent for external readers (CLI).
                if (File.Exists(filePath))
                {
                    if (File.Exists(backupFilePath))
                    {
                        File.Delete(backupFilePath);
                    }
                    File.Move(filePath, backupFilePath);
                }
                File.Move(tempFilePath, filePath);
            }
        }

        public static void CleanupBackup(string backupFilePath)
        {
            if (File.Exists(backupFilePath))
            {
                File.Delete(backupFilePath);
            }
        }
    }
}
