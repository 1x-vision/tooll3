using System.Diagnostics;
using System.IO.Packaging;
using System.Threading.Tasks;
using T3.Editor.Gui.Graph;
using ImGuiNET;
using Operators.Utils.Recording;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.DataTypes.DataSet;
using T3.Core.IO;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Editor.Compilation;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Dialog;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Graph.Rendering;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Templates;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.UiHelpers.Wiki;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.Gui.Windows.RenderExport;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor.Gui;

public static class T3Ui
{
    internal static void InitializeEnvironment()
    {
        //WindowManager.TryToInitialize();
        ExampleSymbolLinking.UpdateExampleLinks();
        VariationHandling.Init();

        Playback.Current = DefaultTimelinePlayback;
        ThemeHandling.Initialize();
    }

    internal static readonly Playback DefaultTimelinePlayback = new();
    internal static readonly BeatTimingPlayback DefaultBeatTimingPlayback = new();

    private static void InitializeAfterAppWindowReady()
    {
        if (_initialed || ImGui.GetWindowSize() == Vector2.Zero)
            return;

        ActiveMidiRecording.ActiveRecordingSet = MidiDataRecording.DataSet;
        _initialed = true;
    }

    private static bool _initialed;

    internal static void ProcessFrame()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);

        CustomComponents.BeginFrame();
        FormInputs.BeginFrame();
        InitializeAfterAppWindowReady();

        // Prepare the current frame 
        RenderStatsCollector.StartNewFrame();
            
        if (Playback.Current.IsLive && GraphWindow.Focused != null)
        {
            PlaybackUtils.UpdatePlaybackAndSyncing();
            //_bpmDetection.AddFftSample(AudioAnalysis.FftGainBuffer);
            AudioEngine.CompleteFrame(Playback.Current, Playback.LastFrameDuration);    // Update
        }
        TextureReadAccess.Update();

        AutoBackup.AutoBackup.IsEnabled = UserSettings.Config.EnableAutoBackup;

        VariationHandling.Update();
        MouseWheelFieldWasHoveredLastFrame = MouseWheelFieldHovered;
        MouseWheelFieldHovered = false;

        FitViewToSelectionHandling.ProcessNewFrame();
        SrvManager.FreeUnusedTextures();
        KeyboardBinding.InitFrame();
        ConnectionSnapEndHelper.PrepareNewFrame();

        var nodeSelection = GraphWindow.Focused?.GraphCanvas.NodeSelection;

        // Set selected id so operator can check if they are selected or not  
        var selectedInstance = nodeSelection?.GetSelectedInstanceWithoutComposition();
        MouseInput.SelectedChildId = selectedInstance?.SymbolChildId ?? Guid.Empty;

        if (nodeSelection != null)
        {
            InvalidateSelectedOpsForTransormGizmo(nodeSelection);
        }

        // Draw everything!
        ImGui.DockSpaceOverViewport();

        WindowManager.Draw();

        // Complete frame
        SingleValueEdit.StartNextFrame();
        SelectableNodeMovement.CompleteFrame();


        FrameStats.CompleteFrame();
        TriggerGlobalActionsFromKeyBindings();

        if (UserSettings.Config.ShowMainMenu || ImGui.GetMousePos().Y < 20)
        {
            DrawAppMenuBar();
        }
            
        _searchDialog.Draw();
        _importDialog.Draw();
        _newProjectDialog.Draw();
        _createFromTemplateDialog.Draw();
        _userNameDialog.Draw();

        if (IsWindowLayoutComplete())
        {
            if (!UserSettings.IsUserNameDefined() )
            {
                UserSettings.Config.UserName = Environment.UserName;
                _userNameDialog.ShowNextFrame();
            }
        }

        KeyboardAndMouseOverlay.Draw();

        Playback.OpNotReady = false;
        AutoBackup.AutoBackup.CheckForSave();
    }

    private static void InvalidateSelectedOpsForTransormGizmo(NodeSelection nodeSelection)
    {
        // Keep invalidating selected op to enforce rendering of Transform gizmo  
        foreach (var si in nodeSelection.GetSelectedInstances().ToList())
        {
            if (si is not ITransformable)
                continue;

            foreach (var i in si.Inputs)
            {
                // Skip string inputs to prevent potential interference with resource file paths hooks
                // I.e. Invalidating these every frame breaks shader recompiling if Shader-op is selected
                if (i.ValueType == typeof(string))
                {
                    continue;
                }

                i.DirtyFlag.Invalidate();
            }
        }
    }

    /// <summary>
    /// This a bad work around to defer some ui actions until we have completed all
    /// window initialization so they are not discarded by the setup process.
    /// </summary>
    private static bool IsWindowLayoutComplete() => ImGui.GetFrameCount() > 2;

    private static void TriggerGlobalActionsFromKeyBindings()
    {
        if (KeyboardBinding.Triggered(UserActions.Undo))
        {
            UndoRedoStack.Undo();
        }
        else if (KeyboardBinding.Triggered(UserActions.Redo))
        {
            UndoRedoStack.Redo();
        }
        else if (KeyboardBinding.Triggered(UserActions.Save))
        {
            SaveInBackground(saveAll: false);
        }
        else if (KeyboardBinding.Triggered(UserActions.ToggleAllUiElements))
        {
            ToggleAllUiElements();
        }
        else if (KeyboardBinding.Triggered(UserActions.SearchGraph))
        {
            _searchDialog.ShowNextFrame();
        }
        else if (KeyboardBinding.Triggered(UserActions.ToggleFullscreen))
        {
            UserSettings.Config.FullScreen = !UserSettings.Config.FullScreen;
        }
        else if (KeyboardBinding.Triggered(UserActions.ToggleFocusMode)) ToggleFocusMode();
    }

    private static void ToggleFocusMode()
    {
        var shouldBeFocusMode = !UserSettings.Config.FocusMode;

        var outputWindow = OutputWindow.GetPrimaryOutputWindow();
        var primaryGraphWindow = GraphWindow.Focused;

        if (shouldBeFocusMode && outputWindow != null && primaryGraphWindow != null)
        {
            outputWindow.Pinning.TryGetPinnedOrSelectedInstance(out var instance, out _);
            primaryGraphWindow.GraphImageBackground.OutputInstance = instance;
        }

        UserSettings.Config.FocusMode = shouldBeFocusMode;
        UserSettings.Config.ShowToolbar = shouldBeFocusMode;
        ToggleAllUiElements();
        LayoutHandling.LoadAndApplyLayoutOrFocusMode(shouldBeFocusMode ? 11 : UserSettings.Config.WindowLayoutIndex);

        outputWindow = OutputWindow.GetPrimaryOutputWindow();
        if (!shouldBeFocusMode && outputWindow != null && primaryGraphWindow != null)
        {
            outputWindow.Pinning.PinInstance(primaryGraphWindow.GraphImageBackground.OutputInstance, primaryGraphWindow.GraphCanvas);
            primaryGraphWindow.GraphImageBackground.ClearBackground();
        }
    }

    private static void DrawAppMenuBar()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6) * T3Ui.UiScaleFactor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, T3Style.WindowChildPadding * T3Ui.UiScaleFactor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                UserSettings.Config.ShowMainMenu = true;
            }

            ImGui.SetCursorPos(new Vector2(0, -1)); // Shift to make menu items selected when hitting top of screen

            if (ImGui.BeginMenu("Project"))
            {
                UserSettings.Config.ShowMainMenu = true;

                if (ImGui.MenuItem("New...", KeyboardBinding.ListKeyboardShortcuts(UserActions.New, false), false, !IsCurrentlySaving))
                {
                    _createFromTemplateDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("New Project"))
                {
                    _newProjectDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Import Operators", null, false, !IsCurrentlySaving))
                {
                    _importDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Fix File references", ""))
                {
                    FileReferenceOperations.FixOperatorFilepathsCommand_Executed();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Save", KeyboardBinding.ListKeyboardShortcuts(UserActions.Save, false), false, !IsCurrentlySaving))
                {
                    SaveInBackground(saveAll: false);
                }
                
                if (ImGui.MenuItem("Save All", !IsCurrentlySaving))
                {
                    Task.Run(() =>
                             {
                                 Save(true);
                             });
                }
                
                ImGui.Separator();
                
                if(ImGui.BeginMenu("Load Project", !IsCurrentlySaving))
                {
                    foreach (var package in SymbolPackage.AllPackages.Cast<EditorSymbolPackage>())
                    {
                        if (!package.HasHome)
                            continue;

                        var name = package.DisplayName;
                        
                        if (ImGui.MenuItem(name))
                        {
                            bool replaceFocusedWindow = false;

                            if (GraphWindow.GraphWindowInstances.Count > 0)
                            {
                                var choice = BlockingWindow.Instance.Show("Would you like to create a new window?", "Opening " + name, "Yes", "No");
                                replaceFocusedWindow = choice == "Yes";
                            }
                            
                            if (!GraphWindow.TryOpenPackage(package, replaceFocusedWindow))
                            {
                                Log.Error("Failed to open package " + name);
                            };
                        }
                    }
                    
                    ImGui.EndMenu();
                }
                
                ImGui.Separator();
                
                if(ImGui.BeginMenu("Clear shader cache"))
                {
                    if (ImGui.MenuItem("Editor only"))
                    {
                        ShaderCompiler.DeleteShaderCache(all: false);
                    }

                    if (ImGui.MenuItem("All editor and player versions"))
                    {
                        ShaderCompiler.DeleteShaderCache(all: true);
                    }
                    
                    ImGui.EndMenu();
                }
                
                ImGui.Separator();

                if (ImGui.MenuItem("Quit", !IsCurrentlySaving))
                {
                    EditorUi.Instance.ExitApplication();
                }

                if (ImGui.IsItemHovered() && IsCurrentlySaving)
                {
                    ImGui.SetTooltip("Can't exit while saving is in progress");
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                UserSettings.Config.ShowMainMenu = true;
                if (ImGui.MenuItem("Undo", "CTRL+Z", false, UndoRedoStack.CanUndo))
                {
                    UndoRedoStack.Undo();
                }

                if (ImGui.MenuItem("Redo", "CTRL+Y", false, UndoRedoStack.CanRedo))
                {
                    UndoRedoStack.Redo();
                }

                ImGui.Separator();

                if (ImGui.BeginMenu("Bookmarks"))
                {
                    GraphBookmarkNavigation.DrawBookmarksMenu();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Tools"))
                {
                    if (ImGui.MenuItem("Export Operator Descriptions"))
                    {
                        ExportWikiDocumentation.ExportWiki();
                    }

                    if (ImGui.MenuItem("Export Documentation to JSON"))
                    {
                        ExportDocumentationStrings.ExportDocumentationAsJson();
                    }

                    if (ImGui.MenuItem("Import documentation from JSON"))
                    {
                        ExportDocumentationStrings.ImportDocumentationAsJson();
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Add"))
            {
                UserSettings.Config.ShowMainMenu = true;
                SymbolTreeMenu.Draw();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                UserSettings.Config.ShowMainMenu = true;

                ImGui.Separator();
                ImGui.MenuItem("Show Main Menu", "", ref UserSettings.Config.ShowMainMenu);
                ImGui.MenuItem("Show Title", "", ref UserSettings.Config.ShowTitleAndDescription);
                ImGui.MenuItem("Show Timeline", "", ref UserSettings.Config.ShowTimeline);
                ImGui.MenuItem("Show Minimap", "", ref UserSettings.Config.ShowMiniMap);
                ImGui.MenuItem("Show Toolbar", "", ref UserSettings.Config.ShowToolbar);
                ImGui.MenuItem("Show Interaction Overlay", "", ref UserSettings.Config.ShowInteractionOverlay);
                if (ImGui.MenuItem("Toggle All UI Elements", KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleAllUiElements, false), false,
                                   !IsCurrentlySaving))
                {
                    ToggleAllUiElements();
                }

                ImGui.MenuItem("Fullscreen", KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleFullscreen, false), ref UserSettings.Config.FullScreen);
                if (ImGui.MenuItem("Focus Mode", KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleFocusMode, false), UserSettings.Config.FocusMode))
                {
                    ToggleFocusMode();
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Windows"))
            {
                WindowManager.DrawWindowMenuContent();
                ImGui.EndMenu();
            }
            
            #if DEBUG
            
            if (ImGui.BeginMenu("Debug"))
            {
                if (ImGui.MenuItem("Show Popup"))
                {
                    const string bodyText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis sagittis quis ligula sit amet ornare. " +
                                            "Donec auctor, nisl vel ultricies tincidunt, nisl nisl aliquam nisl, nec pulvinar nisl nisl vitae nisl. " +
                                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis sagittis quis ligula sit amet ornare. ";
                        
                    BlockingWindow.Instance.Show(bodyText, "Debug Popup", "Ok", "Maybe", "Idk", "Possibly", "Affirmative", "Negatory", "NO!");
                }
                ImGui.EndMenu();
                
            }
            
            #endif
            
            if (UserSettings.Config.FullScreen)
            {
                ImGui.Dummy(new Vector2(10, 10));
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1);

                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.ForegroundFull.Fade(0.2f).Rgba);
                ImGui.TextUnformatted(Program.VersionText);
                ImGui.PopStyleColor();
            }

            T3Metrics.DrawRenderPerformanceGraph();

            Program.StatusErrorLine.Draw();

            ImGui.EndMainMenuBar();
        }

        ImGui.PopStyleVar(3);
    }

    private static void ToggleAllUiElements()
    {
        //T3Ui.MaximalView = !T3Ui.MaximalView;
        if (UserSettings.Config.ShowToolbar)
        {
            UserSettings.Config.ShowMainMenu = false;
            UserSettings.Config.ShowTitleAndDescription = false;
            UserSettings.Config.ShowToolbar = false;
            UserSettings.Config.ShowTimeline = false;
        }
        else
        {
            UserSettings.Config.ShowMainMenu = true;
            UserSettings.Config.ShowTitleAndDescription = true;
            UserSettings.Config.ShowToolbar = true;
            UserSettings.Config.ShowTimeline = true;
        }
    }

    private static void SaveInBackground(bool saveAll)
    {
        Task.Run(() => Save(saveAll));
    }

    internal static void Save(bool saveAll)
    {
        if (SaveStopwatch.IsRunning)
        {
            Log.Debug("Can't save modified while saving is in progress");
            return;
        }

        SaveStopwatch.Restart();

        // Todo - parallelize? 
        foreach (var package in EditableSymbolProject.AllProjects)
        {
            if (saveAll)
                package.SaveAll();
            else
                package.SaveModifiedSymbols();
        }

        SaveStopwatch.Stop();
    }

    internal static void SelectAndCenterChildIdInView(Guid symbolChildId)
    {
        var primaryGraphWindow = GraphWindow.Focused;
        if (primaryGraphWindow == null)
            return;

        var compositionOp = primaryGraphWindow.CompositionOp;

        var symbolUi = compositionOp.GetSymbolUi();
        
        if(!symbolUi.ChildUis.TryGetValue(symbolChildId, out var sourceChildUi))
            return;
        
        if(!compositionOp.Children.TryGetValue(symbolChildId, out var selectionTargetInstance))
            return;
        
        primaryGraphWindow.GraphCanvas.NodeSelection.SetSelection(sourceChildUi, selectionTargetInstance);
        FitViewToSelectionHandling.FitViewToSelection();
    }

    // private static void SwapHoveringBuffers()
    // {
    //     (HoveredIdsLastFrame, _hoveredIdsForNextFrame) = (_hoveredIdsForNextFrame, HoveredIdsLastFrame);
    //     _hoveredIdsForNextFrame.Clear();
    //     
    //     (RenderedIdsLastFrame, _renderedIdsForNextFrame) = (_renderedIdsForNextFrame, RenderedIdsLastFrame);
    //     _renderedIdsForNextFrame.Clear();            
    // }

    /// <summary>
    /// Statistics method for debug purpose
    /// </summary>
    private static void CountSymbolUsage()
    {
        var counts = new Dictionary<Symbol, int>();
        foreach (var s in EditorSymbolPackage.AllSymbols)
        {
            foreach (var child in s.Children.Values)
            {
                counts.TryAdd(child.Symbol, 0);
                counts[child.Symbol]++;
            }
        }

        foreach (var (s, c) in counts.OrderBy(c => counts[c.Key]).Reverse())
        {
            Log.Debug($"{s.Name} - {s.Namespace}  {c}");
        }
    }

    internal static bool DraggingIsInProgress = false;
    internal static bool MouseWheelFieldHovered { private get; set; }
    internal static bool MouseWheelFieldWasHoveredLastFrame { get; private set; }
    internal static bool ShowSecondaryRenderWindow => WindowManager.ShowSecondaryRenderWindow;
    internal const string FloatNumberFormat = "{0:F2}";

    private static readonly Stopwatch SaveStopwatch = new();

    // ReSharper disable once InconsistentlySynchronizedField
    internal static bool IsCurrentlySaving => SaveStopwatch is { IsRunning: true };

    public static float UiScaleFactor { get; internal set; } = 1;
    internal static float DisplayScaleFactor { get; set; } = 1;
    internal static bool IsAnyPopupOpen => !string.IsNullOrEmpty(FrameStats.Last.OpenedPopUpName);
    internal static readonly MidiDataRecording MidiDataRecording = new();

    //private static readonly AutoBackup.AutoBackup _autoBackup = new();

    private static readonly CreateFromTemplateDialog _createFromTemplateDialog = new();
    private static readonly UserNameDialog _userNameDialog = new();
    private static readonly SearchDialog _searchDialog = new();
    private static readonly NewProjectDialog _newProjectDialog = new();
    
    private static readonly MigrateOperatorsDialog _importDialog = new();

    [Flags]
    public enum EditingFlags
    {
        None = 0,
        ExpandVertically = 1 << 1,
        PreventMouseInteractions = 1 << 2,
        PreventZoomWithMouseWheel = 1 << 3,
        PreventPanningWithMouse = 1 << 4,
        AllowHoveredChildWindows = 1 << 5,
    }

    internal static bool UseVSync = true;
    public static bool ItemRegionsVisible;
}