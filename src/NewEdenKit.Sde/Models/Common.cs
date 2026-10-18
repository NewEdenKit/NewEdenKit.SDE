namespace NewEdenKit.Sde.Models;

/// <summary>A position in space, in meters.</summary>
public readonly record struct SdePosition(double X, double Y, double Z);

/// <summary>A position on the 2D map projection.</summary>
public readonly record struct SdePosition2D(double X, double Y);

/// <summary>An RGB color with channels from 0 to 1.</summary>
public readonly record struct SdeColor(double R, double G, double B);
