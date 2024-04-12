using SilkWindows.Implementations.FileManager.ItemDrawers;

namespace SilkWindows.Implementations.FileManager;

internal interface IFileManager
{
    public bool HasDroppedFiles { get; }
    bool IsDraggingPaths { get; }
    public void ConsumeDroppedFiles(FileSystemDrawer directoryDrawer);
    
    public void ItemClicked(FileSystemDrawer fileSystemInfo);
    public void RemoveFromSelection(FileSystemDrawer fileSystemInfo);
    
    public bool IsSelected(FileSystemDrawer fileSystemInfo);
    public IEnumerable<FileSystemInfo> GetDirectoryContents(DirectoryInfo directory);
    public void DoubleClicked(FileDrawer file, bool inExternalEditor);
    
    public void ShowInSystemFileManager(FileSystemDrawer drawer);
    public void Log(FileSystemDrawer drawer, string log);
    void CreateNewSubfolder(DirectoryDrawer directoryDrawer, bool consumeDroppedFiles = false);
    public bool IsDropTarget(FileSystemDrawer directoryDrawer);
    
    public string FormatPathForDisplay(string path);
}