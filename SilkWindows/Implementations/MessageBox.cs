using System.Numerics;
using ImGuiNET;

namespace SilkWindows.Implementations;

internal sealed class MessageBox<T> : IImguiDrawer<T>
{
    public MessageBox(string message, T[]? buttons, Func<T, string>? toString)
    {
        if (buttons == null || buttons.Length == 0)
        {
            buttons = [];
        }
        
        toString ??= item => item!.ToString()!;
        _message = message;
        _buttons = buttons;
        _toString = toString;
    }
    
    public void OnRender(string windowName, double deltaSeconds, ImFonts? fonts)
    {
        var contentRegion = ImGui.GetContentRegionAvail();
        var padding = contentRegion.X * 0.1f;
        var widthAvailable = contentRegion.X - padding;
        var hasFonts = fonts != null;
        
        ImGui.NewLine();
        ImGui.PushTextWrapPos(widthAvailable);
        ImGui.SetCursorPosX(padding);
        ImGui.TextWrapped(_message);
        ImGui.PopTextWrapPos();
        
        DrawSpacing(fonts, hasFonts);
        
        if (hasFonts)
            ImGui.PushFont(fonts.Small);
        
        ImGui.SetCursorPosX(padding);
        if (ImGui.Button("Copy to clipboard"))
        {
            ImGui.SetClipboardText(_message);
        }
        
        var style = ImGui.GetStyle();
        var originalHoverFlags = style.HoverFlagsForTooltipMouse;
        style.HoverFlagsForTooltipMouse = ImGuiHoveredFlags.DelayNone;
        if (ImGui.BeginItemTooltip())
        {
            ImGui.Text("Make sure to paste somewhere before closing the window,\nas some events copy text to the clipboard and can overwrite it.");
            ImGui.EndTooltip();
        }
        
        style.HoverFlagsForTooltipMouse = originalHoverFlags;
        
        if (hasFonts)
            ImGui.PopFont();
        
        DrawSpacing(fonts, hasFonts);
        ImGui.Separator();
        DrawSpacing(fonts, hasFonts);
        
        if (hasFonts)
        {
            ImGui.PushFont(fonts.Regular);
        }
        
        var width = ImGui.GetContentRegionAvail().X;
        var size = new Vector2(width, 0);
        foreach (var button in _buttons)
        {
            var name = _toString.Invoke(button);
            
            if (ImGui.Button(name, size))
            {
                _result ??= button;
            }
            
            ImGui.Spacing();
        }
        
        if (hasFonts)
            ImGui.PopFont();
        
        DrawSpacing(fonts, hasFonts);
        
        return;
        
        static void DrawSpacing(ImFonts? fonts, bool hasFonts)
        {
            if (hasFonts)
            {
                ImGui.PushFont(fonts.Small);
                ImGui.NewLine();
                ImGui.PopFont();
            }
            else
            {
                const int spacingAmount = 4;
                for (int i = 0; i < spacingAmount; i++)
                    ImGui.Spacing();
            }
        }
    }
    
    public void OnWindowUpdate(double deltaSeconds, out bool shouldClose)
    {
        shouldClose = _result != null;
    }
    
    public void OnClose()
    {
    }
    
    public void OnFileDrop(string[] filePaths)
    {
        // do nothing - drag and drop could be supported by another window!
    }
    
    public void OnWindowFocusChanged(bool changedTo)
    {
        // do nothing
    }
    
    public T Result => _result!;
    
    private readonly Func<T, string> _toString;
    private T? _result;
    private readonly T[] _buttons;
    private readonly string _message;
}