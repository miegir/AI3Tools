namespace AI3Tools;

public class FileTargetCollector : IFileTargetCollector
{
    private readonly List<FileTarget> targets = [];

    public void AddTarget(FileTarget target)
    {
        targets.Add(target);
    }

    public void Commit()
    {
        foreach (var target in targets)
        {
            target.Commit();
            target.Dispose();
        }

        targets.Clear();
    }
}
