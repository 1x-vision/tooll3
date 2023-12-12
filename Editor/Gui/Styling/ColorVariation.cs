﻿using System;
using ImGuiNET;
using T3.Core.DataTypes.Vector;

// ReSharper disable MemberCanBePrivate.Global

namespace T3.Editor.Gui.Styling;

/// <summary>
/// Defines how a color hue should be modified (e.g. muted for disabled fields)
/// </summary>
public struct ColorVariation : IEquatable<ColorVariation>
{

    public float Brightness = 1;
    public float Saturation = 1;
    public float Opacity = 1;

    public ColorVariation()
    {
    }

    internal ColorVariation(float brightnessFactor = 1, float saturationFactor = 1, float opacityFactor = 1)
    {
        Saturation = saturationFactor;
        Brightness = brightnessFactor;
        Opacity = opacityFactor;
    }

    public Color Apply(Color originalColor)
    {
        originalColor.GetHSV(out var h, out var s, out var v);
        return Color.FromHSV(
                             h,
                             s * Saturation,
                             v * Brightness,
                             originalColor.Rgba.W * Opacity);
    }

    public ColorVariation Clone()
    {
        return new ColorVariation()
                   {
                       Saturation = Saturation,
                       Brightness = Brightness,
                       Opacity = Opacity,
                   };
    }

    public bool Equals(ColorVariation other)
    {
        return Math.Abs(Brightness - other.Brightness) < 0.001f
               && Math.Abs(Saturation - other.Saturation) < 0.001f
               && Math.Abs(Opacity - other.Opacity) < 0.001f;
    }
    
    public static bool operator ==(ColorVariation left, ColorVariation right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(ColorVariation left, ColorVariation right)
    {
        return !left.Equals(right);
    }
    
    public override bool Equals(object obj)
    {
        return obj is ColorVariation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Brightness, Saturation, Opacity);
    }

}