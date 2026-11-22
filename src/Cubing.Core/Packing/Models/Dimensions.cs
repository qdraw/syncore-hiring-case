namespace Cubing.Core.Packing.Models;

/// <summary>Length, width and height of an article or box, in a single consistent unit.</summary>
public sealed class Dimensions(int length, int width, int height)
{
    public int Length { get; } = length;
    public int Width { get; } = width;
    public int Height { get; } = height;

    public bool IsPositive => Length > 0 && Width > 0 && Height > 0;

    public bool IsCubic => Length == Width && Width == Height;
}
