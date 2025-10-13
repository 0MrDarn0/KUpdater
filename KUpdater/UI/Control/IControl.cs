// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using SkiaSharp;

namespace KUpdater.UI.Control;

public interface IControl : IDisposable {
    string Id { get; }
    public bool Visible { get; set; }
    Rectangle Bounds { get; }
    void Draw(Graphics g);
    void Draw(SKCanvas canvas);
    bool OnMouseMove(Point p);
    bool OnMouseDown(Point p);
    bool OnMouseUp(Point p);
    bool OnMouseWheel(int delta, Point p);
}
