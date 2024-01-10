using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Windows;

namespace T3.Editor.UiModel;

internal sealed partial class EditableSymbolProject : EditorSymbolPackage
{
    protected override AssemblyInformation AssemblyInformation => CsProjectFile.Assembly;

    private static readonly Queue<Action> PendingUpdateActions = new();

    public EditableSymbolProject(CsProjectFile csProjectFile)
        : base(csProjectFile.Assembly)
    {
        CsProjectFile = csProjectFile;
        csProjectFile.Recompiled += project => PendingUpdateActions.Enqueue(() => UpdateSymbols(project));
        AllProjectsRw.Add(this);
        _fileSystemWatcher = new EditablePackageFsWatcher(this, OnFileChanged, OnFileRenamed);
    }

    public bool TryCreateHome()
    {
        if (!CsProjectFile.Assembly.HasHome)
            return false;

        var homeGuid = CsProjectFile.Assembly.HomeGuid;
        var symbol = Symbols[homeGuid];
        RootInstance = symbol.CreateInstance(HomeInstanceId);
        return true;
    }

    public bool TryCompile(string sourceCode, string newSymbolName, Guid newSymbolId, string nameSpace, out Symbol newSymbol)
    {
        throw new NotImplementedException();

        newSymbol = new Symbol(type, newSymbolId);
        newSymbol.PendingSource = sourceCode;
        newSymbol.Namespace = @namespace;
        return true;
    }

    /// <returns>
    /// Returns true if the project does not need to be recompiled or if it successfully recompiled.
    /// </returns>
    public bool RecompileIfNecessary()
    {
        if (!_needsCompilation)
            return true;

        return TryRecompile();
    }

    public bool TryRecompileWithNewSource(Symbol symbol, string newSource)
    {
        string currentSource;
        try
        {
            currentSource = File.ReadAllText(symbol.SymbolFilePath);
        }
        catch
        {
            Log.Error($"Could not read original source code at \"{symbol.SymbolFilePath}\"");
            currentSource = string.Empty;
        }

        symbol.PendingSource = newSource;
        MarkAsSaving();

        var filePathFmt = BuildFilepathFmt(symbol);
        WriteSymbolSourceToFile(symbol, filePathFmt);

        var success = TryRecompile();

        if (!success && currentSource != string.Empty)
        {
            symbol.PendingSource = currentSource;
            WriteSymbolSourceToFile(symbol, filePathFmt);
        }

        UnmarkAsSaving();

        return success;
    }

    private bool TryRecompile() => CsProjectFile.TryRecompile(Compiler.BuildMode.Debug);

    private void UpdateSymbols(CsProjectFile project)
    {
        LocateSourceCodeFiles();
        var operatorTypes = project.Assembly.OperatorTypes;
        Dictionary<Guid, Symbol> foundSymbols = new();

        Dictionary<Guid, Type> newTypes = new();
        foreach (var (guid, type) in operatorTypes)
        {
            if (Symbols.Count > 0 && Symbols.Remove(guid, out var symbol))
            {
                SymbolRegistry.EntriesEditable.Remove(guid);
                foundSymbols.Add(guid, symbol);
                symbol.UpdateInstanceType(type);
                symbol.CreateAnimationUpdateActionsForSymbolInstances();
                //UpdateUiEntriesForSymbol(symbol);
            }
            else
            {
                // it's a new type!!
                newTypes.Add(guid, type);
            }

            //UpdateUiEntriesForSymbol(symbol);
        }

        foreach (var (guid, symbol) in foundSymbols)
        {
            Symbols.Add(guid, symbol);
            SymbolRegistry.EntriesEditable.Add(guid, symbol);
        }
    }

    private bool RemoveSymbol(Guid guid)
    {
        if (!Symbols.Remove(guid, out _))
            return false;

        SymbolRegistry.EntriesEditable.Remove(guid);
        SymbolUiRegistry.EntriesEditable.Remove(guid, out var symbolUi);

        // todo - are connections still valid?
        return true;
    }

    private void UpdateUiEntriesForSymbol(Symbol symbol)
    {
        if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
        {
            symbolUi.UpdateConsistencyWithSymbol();
        }
        else
        {
            symbolUi = new SymbolUi(symbol);
            SymbolUiRegistry.EntriesEditable.Add(symbol.Id, symbolUi);
            SymbolUis.TryAdd(symbol.Id, symbolUi);
        }
    }

    public void ReplaceSymbolUi(Symbol newSymbol, SymbolUi symbolUi)
    {
        SymbolRegistry.EntriesEditable.Add(newSymbol.Id, newSymbol);
        SymbolUiRegistry.EntriesEditable[newSymbol.Id] = symbolUi;
        Symbols.Add(newSymbol.Id, newSymbol);
        SymbolUis.TryAdd(newSymbol.Id, symbolUi); // todo - are connections still valid?
        UpdateUiEntriesForSymbol(newSymbol);
        RegisterCustomChildUi(newSymbol);
    }

    public void RenameNameSpace(NamespaceTreeNode node, string nameSpace, EditableSymbolProject newDestinationProject)
    {
        var movingToAnotherPackage = newDestinationProject != this;

        var ogNameSpace = node.GetAsString();
        foreach (var symbol in Symbols.Values)
        {
            if (!symbol.Namespace.StartsWith(ogNameSpace))
                continue;

            //var newNameSpace = parent + "."
            var newNameSpace = Regex.Replace(symbol.Namespace, ogNameSpace, nameSpace);
            Log.Debug($" Changing namespace of {symbol.Name}: {symbol.Namespace} -> {newNameSpace}");
            symbol.Namespace = newNameSpace;

            if (!movingToAnotherPackage)
                continue;

            GiveSymbolToPackage(symbol, newDestinationProject);
        }
    }

    private void GiveSymbolToPackage(Symbol symbol, EditableSymbolProject newDestinationProject)
    {
        throw new NotImplementedException();
    }

    private void MarkAsModified()
    {
        _needsCompilation = true;
    }

    private static readonly Guid HomeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
    public override string Folder => CsProjectFile.Directory;

    public readonly CsProjectFile CsProjectFile;

    public static Instance RootInstance { get; private set; }

    public static EditableSymbolProject ActiveProject { get; private set; }
    private static readonly List<EditableSymbolProject> AllProjectsRw = new();
    public static readonly IReadOnlyList<EditableSymbolProject> AllProjects = AllProjectsRw;

    public override bool IsModifiable => true;

    private readonly EditablePackageFsWatcher _fileSystemWatcher;

    private bool _needsCompilation;

    public void ExecutePendingUpdates()
    {
        while (PendingUpdateActions.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }
}