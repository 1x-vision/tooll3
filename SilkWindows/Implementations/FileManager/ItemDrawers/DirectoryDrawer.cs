using System.Numerics;
using ImGuiNET;

namespace SilkWindows.Implementations.FileManager.ItemDrawers;

internal sealed class DirectoryDrawer : FileSystemDrawer
{
    internal override string DisplayName { get; }
    internal override string Path => DirectoryInfo.FullName;
    internal override bool IsDirectory => true;
    
    internal Action? ToggleButtonPressed;
    public DirectoryInfo DirectoryInfo { get; }
    
    private readonly List<DirectoryDrawer> _directories = [];
    private readonly List<FileDrawer> _files = [];
    
    public override bool IsReadOnly { get; }
    
    private readonly record struct FileTableColumn(string Name, ImGuiTableColumnFlags Flags, Action<FileDrawer, ImFonts> DrawAction);
    
    private const ImGuiTableFlags FileTableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingFixedFit;
    private const ImGuiTableColumnFlags FileColumnFlags = ImGuiTableColumnFlags.None;
    private const ImGuiTableColumnFlags ColumnFlags = ImGuiTableColumnFlags.None;
    
    private static readonly FileTableColumn[] _fileTableColumns =
        [
            new FileTableColumn("File", FileColumnFlags, (file, fonts) => { file.Draw(fonts); }),
            new FileTableColumn("Extension", ColumnFlags, (file, fonts) => file.DrawFileFormat(fonts)),
            new FileTableColumn("Size", ColumnFlags, (file, fonts) => file.DrawSize(fonts)),
            new FileTableColumn("Modified Date", ColumnFlags, (file, fonts) => file.DrawLastModified(fonts))
        ];
    
    public DirectoryDrawer(IFileManager fileManager, DirectoryInfo directory, bool isReadOnly, DirectoryDrawer? parent, string? alias = null) :
        base(fileManager, parent)
    {
        DirectoryInfo = directory;
        _needsRescan = true;
        IsReadOnly = isReadOnly;
        IsRoot = parent == null;
        DisplayName = string.IsNullOrEmpty(alias) ? DirectoryInfo.Name : '/' + alias;
        
        var buttonIdSuffix = "##" + DisplayName;
        _expandedButtonLabel = (IsRoot ? "_" : IFileManager.ExpandedButtonLabel) + buttonIdSuffix;
        _collapsedButtonLabel = IFileManager.CollapsedButtonLabel + buttonIdSuffix;
        _newSubfolderLabel = "*New subfolder" + buttonIdSuffix;
        _fileTableLabel = "File table" + buttonIdSuffix;
        
        Expanded = IsRoot;
        
        var topParent = parent;
        
        while (topParent is { IsRoot: false })
        {
            topParent = topParent.ParentDirectoryDrawer;
        }
        
        if (topParent != null)
        {
            var relativePath = System.IO.Path.GetRelativePath(topParent.RootDirectory.FullName, directory.FullName);
            var relativeDirectory = System.IO.Path.Combine(topParent.DisplayName, relativePath);
            _relativeDirectory = fileManager.FormatPathForDisplay(relativeDirectory);
        }
        else
        {
            _relativeDirectory = DisplayName;
        }
    }
    
    public void MarkNeedsRescan() => _needsRescan = true;
    
    private void Rescan()
    {
        ClearChildren();
        
        var contents = FileManager.GetDirectoryContents(DirectoryInfo);
        foreach (var dir in contents)
        {
            switch (dir)
            {
                case DirectoryInfo di:
                    _directories.Add(new DirectoryDrawer(FileManager, di, IsReadOnly, this));
                    break;
                case FileInfo fi:
                    
                    // determine relative path of file for display
                    var topParent = this;
                    while (topParent.ParentDirectoryDrawer != null)
                        topParent = topParent.ParentDirectoryDrawer;
                    
                    var topParentDisplayName = topParent.DisplayName;
                    var topParentPath = topParent.Path;
                    var relativePath = System.IO.Path.GetRelativePath(topParentPath, fi.FullName);
                    
                    // only add the root folder name if it has an alias - should probably have a better way of determining this
                    relativePath = topParentDisplayName[0] == '/' ? System.IO.Path.Combine(topParentDisplayName, relativePath) : relativePath;
                    
                    // add new file drawer
                    _files.Add(new FileDrawer(FileManager, fi, this, relativePath));
                    break;
            }
        }
        
        return;
        
        void ClearChildren()
        {
            for (int i = _directories.Count - 1; i >= 0; i--)
            {
                FileManager.RemoveFromSelection(_directories[i]);
                _directories.RemoveAt(i);
            }
            
            for (int i = _files.Count - 1; i >= 0; i--)
            {
                FileManager.RemoveFromSelection(_files[i]);
                _files.RemoveAt(i);
            }
        }
    }
    
    protected override void DrawSelectable(ImFonts fonts, bool isSelected)
    {
        if (IsRoot)
        {
            DrawRootSelectable(fonts);
        }
        else
        {
            DrawStandardSelectable(fonts.Regular, fonts.Regular, isSelected, false);
        }
    }
    
    /// <summary>
    /// Returns true is button was pressed
    /// </summary>
    private bool DrawStandardSelectable(ImFontPtr buttonFont, ImFontPtr textFont, bool isSelected, bool expanded,
                                        ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
    {
        var clicked = false;
        
        // expand / collapse button
        var expandCollapseLabel = expanded ? _expandedButtonLabel : _collapsedButtonLabel;
        ImGui.PushFont(buttonFont);
        if (ImGui.Button(expandCollapseLabel))
        {
            Expanded = !Expanded;
            clicked = true;
        }
        
        ImGui.PopFont();
        
        // text
        ImGui.PushFont(textFont);
        ImGui.SameLine();
        DrawTouchPaddedSelectable(DisplayName, isSelected, expanded, flags);
        ImGui.PopFont();
        
        return clicked;
    }
    
    void DrawRootSelectable(ImFonts fonts)
    {
        // prevent selectable highlighting we don't want, blend foreground with background
        var transparent = ImGui.GetColorU32(Vector4.Zero);
        var bgColor = ImGui.GetColorU32(ImGuiCol.TableHeaderBg);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, transparent);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        ImGui.PushStyleColor(ImGuiCol.Button, bgColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, transparent);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, ImGui.GetColorU32(ImGuiCol.Text));
        
        var originalCursorPosition = ImGui.GetCursorScreenPos();
        
        // ensure button and label don't intersect with curved tab corners
        float tabCornerRadius = 8f; //ImGui.GetStyle().TabRounding;
        
        // -------- Split channel drawing --------
        
        // split channels, select top channel
        var drawList = ImGui.GetWindowDrawList();
        drawList.ChannelsSplit(2);
        drawList.ChannelsSetCurrent(1);
        
        var newCursorPos = new Vector2(x: originalCursorPosition.X + tabCornerRadius, // make sure curves of tab dont intersect with text 
                                       y: originalCursorPosition.Y + ImGui.GetStyle().FramePadding.Y); // vertically center (see offsets applied below)
        
        ImGui.SetCursorScreenPos(newCursorPos);
        // draw the standard selectable
        
        var expanded = Expanded;
        var flags = expanded ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled;
        
        var clicked = DrawStandardSelectable(fonts.Large, fonts.Large, false, expanded, flags);
        
        // capture where the selectable ends
        var max = ImGui.GetItemRectMax();
        if (!expanded)
        {
            // HACK - scale when dragged fills the full width and i cant figure out why - the only reason i can assume is because the function
            // is being called outside of the table ID stack in the file manager. I don't care to fix that right now because damn this is hard
            max.X = newCursorPos.X + ImguiUtils.GetButtonSize(DisplayName).X + ImguiUtils.GetButtonSize("_").X;
        }
        
        // change to bottom channel
        drawList.ChannelsSetCurrent(0);
        
        // draw a tab-like outline
        var style = ImGui.GetStyle();
        Vector2 tweakScaleToMatchStyle = new(x: -style.WindowPadding.X + style.CellPadding.X, // we are in a window inside a table cell
                                             y: style.FramePadding.Y + style.SeparatorTextPadding.Y); // buttons have a frame padding and we draw a separator beneath us
        
        drawList.AddRectFilled(originalCursorPosition, max + tweakScaleToMatchStyle, bgColor, tabCornerRadius, ImDrawFlags.RoundCornersTop);
        
        // merge channels
        drawList.ChannelsMerge();
        
        // -------- End split channel drawing --------
        
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
        
        if (clicked)
            ToggleButtonPressed?.Invoke();
    }
    
    private static void DrawTouchPaddedSelectable(string label, bool isSelected, bool expandHorizontally,
                                                  ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
    {
        // we need extra padding between the rows so there's no blank space between them
        // the intuitive thing would be to allow files to be dropped in between directories like many other file managers do, but this is much easier
        // for the time being.
        
        var style = ImGui.GetStyle();
        var currentPadding = style.TouchExtraPadding;
        
        style.TouchExtraPadding = currentPadding with { Y = style.ItemSpacing.Y + style.FramePadding.Y };
        
        
        if (expandHorizontally)
        {
            style.TouchExtraPadding.X = 0f;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        }
        
        ImGui.Selectable(label, isSelected, flags);
        
        style.TouchExtraPadding = currentPadding; // reset touch padding
    }
    
    protected override void DrawTooltipContents(ImFonts fonts)
    {
        ImGui.Text(_relativeDirectory);
        
        ImGui.PushFont(fonts.Small);
        ImGui.Text("Last modified: " + FileSystemInfo.LastWriteTime.ToShortDateString());
        ImGui.PopFont();
    }
    
    protected override FileSystemInfo FileSystemInfo => DirectoryInfo;
    
    protected override void CompleteDraw(ImFonts fonts, bool hovered, bool isSelected)
    {
        if (!Expanded)
        {
            return;
        }
        
        if (_needsRescan)
        {
            Rescan();
            _needsRescan = false;
        }
        
        // draw guide line for children
        var lineStart = Vector2.Zero;
        
        if (IsRoot)
        {
            ImGui.Separator();
        }
        else
        {
            lineStart = ImGui.GetCursorScreenPos();
            ImGui.Indent();
            lineStart = (lineStart + ImGui.GetCursorScreenPos()) * 0.5f;
        }
        
        ImGui.BeginGroup();
        
        if (_directories.Count + _files.Count > 0)
        {
            foreach (var dir in _directories)
            {
                dir.Draw(fonts);
            }
            
            var columnCount = _fileTableColumns.Length;
            if (_files.Count > 0 && ImGui.BeginTable(_fileTableLabel, columnCount, FileTableFlags))
            {
                ImGui.TableNextRow();
                for (var index = 0; index < columnCount; index++)
                {
                    var column = _fileTableColumns[index];
                    ImGui.TableSetupColumn(column.Name, column.Flags);
                }
                
                foreach (var file in _files)
                {
                    ImGui.TableNextRow();
                    for (int i = 0; i < columnCount; i++)
                    {
                        if (ImGui.TableNextColumn())
                            _fileTableColumns[i].DrawAction(file, fonts);
                    }
                }
                
                ImGui.EndTable();
            }
        }
        else
        {
            // draw empty directory
            ImGui.TextDisabled("Empty directory");
        }
        
        if (!IsReadOnly)
        {
            ImGui.PushFont(fonts.Small);
            if (ImGui.SmallButton(_newSubfolderLabel))
            {
                FileManager.Log(this, $"Requested new folder at {DirectoryInfo.FullName}");
                FileManager.CreateNewSubfolder(this);
            }
            
            ImGui.PopFont();
            
            if (ImGui.IsItemHovered() && FileManager.HasDroppedFiles)
            {
                FileManager.CreateNewSubfolder(this, true);
            }
        }
        
        ImGui.EndGroup();
        
        if (!IsRoot)
        {
            ImGui.Unindent();
            var style = ImGui.GetStyle();
            var lineShortenAmount = style.ItemSpacing.Y;
            var lineEnd = lineStart with { Y = ImGui.GetCursorScreenPos().Y - lineShortenAmount };
            
            // draw guide-line
            ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, ImGui.GetColorU32(ImGuiCol.ScrollbarGrab));
        }
    }
    
    protected override void DrawContextMenuContents(ImFonts fonts)
    {
        if (ImGui.MenuItem("Refresh"))
        {
            MarkNeedsRescan();
        }
    }
    
    protected override void OnDoubleClicked() => Expanded = !Expanded;
    
    private bool _needsRescan;
    private readonly string _expandedButtonLabel;
    private readonly string _collapsedButtonLabel;
    private readonly string _newSubfolderLabel;
    private readonly string _relativeDirectory;
    private readonly string _fileTableLabel;
    public readonly bool IsRoot;
}