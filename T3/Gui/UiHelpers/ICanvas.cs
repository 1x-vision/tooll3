using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.UiHelpers
{
    public interface INodeCanvas:ICanvas
    {
        IEnumerable<ISelectableCanvasObject> SelectableChildren { get; }
    }
    
    /// <summary>
    /// A zoomable canvas that can hold <see cref="ISelectableCanvasObject"/> elements.
    /// </summary>
    public interface ICanvas
    {
        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        Vector2 TransformPositionFloat(Vector2 posOnCanvas);

        /// <summary>
        /// Get integer-aligned screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        Vector2 TransformPosition(Vector2 posOnCanvas);

        /// <summary>
        /// Get a pixel aligned screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        Vector2 TransformPositionFloored(Vector2 posOnCanvas);

        
        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        Vector2 InverseTransformPositionFloat(Vector2 screenPos);

        float InverseTransformX(float x);
        float TransformX(float x);
        
        float InverseTransformY(float y);
        float TransformY(float y);
        
        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        Vector2 TransformDirection(Vector2 vectorInCanvas);

        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        Vector2 InverseTransformDirection(Vector2 vectorInScreen);

        ImRect TransformRect(ImRect canvasRect);

        ImRect InverseTransformRect(ImRect screenRect);

        
        
        Vector2 Scale { get; }
        Vector2 Scroll { get; }
        Vector2 WindowSize { get; }
        Vector2 WindowPos { get; }
    }
}
