namespace AI3Tools;

public sealed class FileSource(string path) : IFileStreamSource, IStreamSource
{
    public record State(long Length, DateTime LastWriteTimeUtc);

    private readonly string backupPath = path + ".bak";

    private string ReadPath => File.Exists(backupPath) ? backupPath : path;
    public string FileName => Path.GetFileName(path);
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(path);
    public FileDestination Destination => new(path);
    public DateTime LastWriteTimeUtc => File.GetLastWriteTimeUtc(ReadPath);

    public FileStream OpenRead() => File.OpenRead(ReadPath);
    public bool CanUnroll() => File.Exists(backupPath);
    public FileTarget CreateTarget() => new(path, createBackupIfNotExists: true);

    public void Unroll()
    {
        if (File.Exists(backupPath))
        {
            File.Move(backupPath, path, overwrite: true);
        }
    }

    public static IEnumerable<FileSource> EnumerateFiles(string path, string searchPattern)
    {
        foreach (var filePath in Directory.EnumerateFiles(path, searchPattern))
        {
            yield return new FileSource(filePath);
        }
    }

    public static IEnumerable<FileSource> EnumerateFiles(string path, params string[] searchPatterns)
    {
        foreach (var searchPattern in searchPatterns)
        {
            foreach (var source in EnumerateFiles(path, searchPattern))
            {
                yield return source;
            }
        }
    }

    Stream IStreamSource.OpenRead() => OpenRead();
}
