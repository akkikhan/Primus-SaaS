using System.IO.Compression;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Targets;

/// <summary>
/// File output target with rotation and compression support
/// </summary>
public class FileTarget : ITarget
{
    private readonly string _filePath;
    private readonly long _maxFileSize;
    private readonly int _maxRetainedFiles;
    private readonly bool _compress;
    private StreamWriter? _writer;
    private readonly object _lock = new();
    private long _currentFileSize;

    public FileTarget(string filePath, long maxFileSize = 10485760, int maxRetainedFiles = 5, bool compress = false)
    {
        _filePath = filePath;
        _maxFileSize = maxFileSize;
        _maxRetainedFiles = maxRetainedFiles;
        _compress = compress;
        
        InitializeFile();
    }

    private void InitializeFile()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var fileInfo = new FileInfo(_filePath);
        _currentFileSize = fileInfo.Exists ? fileInfo.Length : 0;

        _writer = new StreamWriter(_filePath, append: true)
        {
            AutoFlush = true
        };
    }

    public void Write(LogEntry logEntry)
    {
        lock (_lock)
        {
            if (_writer == null) return;

            var json = logEntry.ToJson();
            
            // Check for rotation
            if (_currentFileSize + json.Length + 2 > _maxFileSize) // +2 for newline
            {
                RotateFiles();
            }

            _writer.WriteLine(json);
            _currentFileSize += json.Length + 2; // Approximate size increase
        }
    }

    private void RotateFiles()
    {
        // Close current writer
        _writer?.Flush();
        _writer?.Close();
        _writer?.Dispose();
        _writer = null;

        try
        {
            // Delete oldest file if it exists
            var oldestFile = GetRotatedFilePath(_maxRetainedFiles);
            if (File.Exists(oldestFile)) File.Delete(oldestFile);

            // Shift existing rotated files
            for (int i = _maxRetainedFiles - 1; i >= 1; i--)
            {
                var source = GetRotatedFilePath(i);
                var dest = GetRotatedFilePath(i + 1);
                
                if (File.Exists(source))
                {
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(source, dest);
                }
            }

            // Rotate current file to .1
            var firstRotation = _filePath + ".1";
            if (File.Exists(_filePath))
            {
                File.Move(_filePath, firstRotation);

                // Compress if enabled
                if (_compress)
                {
                    CompressFile(firstRotation);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to rotate logs: {ex.Message}");
        }
        finally
        {
            // Re-open main file
            InitializeFile();
        }
    }

    private string GetRotatedFilePath(int index)
    {
        var path = $"{_filePath}.{index}";
        return _compress ? path + ".gz" : path;
    }

    private void CompressFile(string filePath)
    {
        try
        {
            var compressedPath = filePath + ".gz";
            using (var originalStream = File.OpenRead(filePath))
            using (var compressedStream = File.Create(compressedPath))
            using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                originalStream.CopyTo(compressor);
            }
            
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to compress log file: {ex.Message}");
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Close();
            _writer?.Dispose();
            _writer = null;
        }
    }
}
