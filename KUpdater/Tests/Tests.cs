// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

namespace KUpdater.Tests;


//[ExposeToLua("Point")]
public class Point2D {
    public int X { get; }
    public int Y { get; }

    public Point2D(int x, int y) { X = x; Y = y; }
    public override string ToString() => $"({X}, {Y})";
}


//[ExposeToLua]
public enum Direction {
    Up,
    Down,
    Left,
    Right
}

/*
  pt = Point(3, 4)
  print(pt)          -- Ausgabe: (3, 4)


  dir = Direction.Up
  print(dir)         -- Ausgabe: Up
 */
