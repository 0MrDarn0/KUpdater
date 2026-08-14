// Copyright (c) 2026 Christian Schnuck
// Licensed under the GPL-3.0 (see LICENSE.txt)

using Kx.Core.Extensions;
using Kx.Sdk.Rendering;
using Kx.Sdk.UI;
using Kx.Sdk.UI.Elements;
using Kx.UI.Layout;

using SkiaSharp;

namespace Kx.UI.Elements;

public sealed class ProgressBar(IVisualContext context, string id) : UIElement(context, id) {
    private KxColor _fillColor = SKColors.Goldenrod.ToKxColor();
    private KxColor _backgroundColor = SKColors.Transparent.ToKxColor();
    private KxColor _borderColor = SKColors.Black.ToKxColor();
    private float _progress;
    private float _borderThickness = 1f;
    private bool _showPercent = true;
    private bool _useAutoFontSize = false;
    private float _fixedFontSize = 16f;
    private TextAlignmentHorizontal _textAlignHorizontal = TextAlignmentHorizontal.Center;
    private TextAlignmentVertical _textAlignVertical = TextAlignmentVertical.Top;
    private string? _percentFontFamily = null;
    private bool _percentBold = true;
    private bool _percentItalic = false;
    private float _percentBaseFontSize = 18f;
    private float _percentMinFontSize = 6f;
    private float _textPadding = 4f;
    private KxColor _textShadowColor = SKColors.Black.WithAlpha(120).ToKxColor();
    private float _textShadowOffsetX = 1f;
    private float _textShadowOffsetY = 1f;
    private int _percentDecimals = 0;
    private bool _percentAsInteger = true;
    private KxColor? _explicitTextColor = null;

    public float Progress {
        get => _progress;
        set {
            _progress = Math.Clamp(value, 0f, 1f);
            Invalidate();
        }
    }

    public KxColor FillColor {
        get => _fillColor;
        set {
            _fillColor = value;
            Invalidate();
        }
    }

    public KxColor BackgroundColor {
        get => _backgroundColor;
        set {
            _backgroundColor = value;
            Invalidate();
        }
    }

    public KxColor BorderColor {
        get => _borderColor;
        set {
            _borderColor = value;
            Invalidate();
        }
    }

    public float BorderThickness {
        get => _borderThickness;
        set {
            _borderThickness = Math.Max(0f, value);
            Invalidate();
        }
    }

    public bool ShowPercent {
        get => _showPercent;
        set {
            _showPercent = value;
            Invalidate();
        }
    }

    public bool UseAutoFontSize {
        get => _useAutoFontSize;
        set {
            _useAutoFontSize = value;
            Invalidate();
        }
    }

    public float FixedFontSize {
        get => _fixedFontSize;
        set {
            _fixedFontSize = Math.Max(1f, value);
            Invalidate();
        }
    }

    public TextAlignmentHorizontal TextAlignHorizontal {
        get => _textAlignHorizontal;
        set {
            _textAlignHorizontal = value;
            Invalidate();
        }
    }
    public TextAlignmentVertical TextAlignVertical {
        get => _textAlignVertical;
        set {
            _textAlignVertical = value;
            Invalidate();
        }
    }

    public string? PercentFontFamily {
        get => _percentFontFamily;
        set {
            _percentFontFamily = value;
            Invalidate();
        }
    }

    public bool PercentBold {
        get => _percentBold;
        set {
            _percentBold = value;
            Invalidate();
        }
    }

    public bool PercentItalic {
        get => _percentItalic;
        set {
            _percentItalic = value;
            Invalidate();
        }
    }

    public float PercentBaseFontSize {
        get => _percentBaseFontSize;
        set {
            _percentBaseFontSize = Math.Max(6f, value);
            Invalidate();
        }
    }

    public float PercentMinFontSize {
        get => _percentMinFontSize;
        set {
            _percentMinFontSize = Math.Max(4f, value);
            Invalidate();
        }
    }

    public float TextPadding {
        get => _textPadding;
        set {
            _textPadding = Math.Max(0f, value);
            Invalidate();
        }
    }

    public KxColor TextShadowColor {
        get => _textShadowColor;
        set {
            _textShadowColor = value;
            Invalidate();
        }
    }

    public float TextShadowOffsetX {
        get => _textShadowOffsetX;
        set {
            _textShadowOffsetX = value;
            Invalidate();
        }
    }

    public float TextShadowOffsetY {
        get => _textShadowOffsetY;
        set {
            _textShadowOffsetY = value;
            Invalidate();
        }
    }

    public int PercentDecimals {
        get => _percentDecimals;
        set {
            _percentDecimals = Math.Max(0, Math.Min(3, value));
            Invalidate();
        }
    }

    public bool PercentAsInteger {
        get => _percentAsInteger;
        set {
            _percentAsInteger = value;
            Invalidate();
        }
    }

    public KxColor? ExplicitTextColor {
        get => _explicitTextColor;
        set {
            _explicitTextColor = value;
            Invalidate();
        }
    }

    public double DisplayedPercent => Math.Round(Progress * 100.0, PercentDecimals);

    public override void OnDpiChanged(float scale) {
        base.OnDpiChanged(scale);
    }

    public override void Measure(float dpi) {
        if (FixedBounds is Rectangle fixedBounds) {
            DesiredSize = new Size(
                fixedBounds.Width + (int)(Margin.Horizontal * dpi),
                fixedBounds.Height + (int)(Margin.Vertical * dpi));
            return;
        }

        DesiredSize = new Size((int)(240 * dpi), (int)(8 * dpi));
    }

    protected override void OnDraw(IKxCanvas canvas) {
        if (!Visible)
            return;

        Rectangle rect = LayoutRect;

        // Background
        canvas.DrawRect(rect.Left, rect.Top, rect.Right, rect.Bottom, _backgroundColor);

        // Fill
        float fillWidth = rect.Width * Progress;
        if (Progress > 0f && fillWidth > 0f)
            canvas.DrawRect(rect.Left, rect.Top, rect.Left + fillWidth, rect.Bottom, _fillColor);

        // Border
        if (BorderThickness > 0f)
            canvas.DrawRectStroke(rect.Left, rect.Top, rect.Right, rect.Bottom, _borderColor, _borderThickness * DpiScale);

        if (!_showPercent)
            return;

        double percentValue = Math.Round(Progress * 100.0, PercentDecimals);
        string text = _percentAsInteger ? $"{(int)Math.Round(percentValue)}%" : percentValue.ToString($"F{PercentDecimals}") + "%";

        float fontSize;

        if (_useAutoFontSize) {
            // Auto-fit
            float padding = _textPadding * DpiScale;
            float maxTextWidth = Math.Max(0f, rect.Width - padding * 2f);
            float baseFontSize = Math.Max(8f, rect.Height * 0.7f * DpiScale);

            fontSize = baseFontSize;
            canvas.MeasureText(text, fontSize, out float textWidth, out float textHeight, _percentFontFamily, _percentBold, _percentItalic);

            while (textWidth > maxTextWidth && fontSize > _percentMinFontSize * DpiScale) {
                fontSize -= 1f;
                canvas.MeasureText(text, fontSize, out textWidth, out textHeight, _percentFontFamily, _percentBold, _percentItalic);
            }
        } else {
            // Fixed size
            fontSize = _fixedFontSize * DpiScale;
            canvas.MeasureText(text, fontSize, out float _, out float _, _percentFontFamily, _percentBold, _percentItalic);
        }

        canvas.MeasureText(text, fontSize, out float tw, out float th, _percentFontFamily, _percentBold, _percentItalic);

        float textX = _textAlignHorizontal switch {
            TextAlignmentHorizontal.Left   => rect.Left,
            TextAlignmentHorizontal.Center => rect.Left + (rect.Width - tw) / 2f,
            TextAlignmentHorizontal.Right  => rect.Right - tw,
            _ => rect.Left
        };

        float textY = _textAlignVertical switch {
            TextAlignmentVertical.Top    => rect.Top + th,
            TextAlignmentVertical.Middle => rect.Top + (rect.Height + th) / 2f,
            TextAlignmentVertical.Bottom => rect.Bottom,
            _ => rect.Top + (rect.Height + th) / 2f
        };


        KxColor textColor = _explicitTextColor ?? ComputeContrastColor(textX, tw, rect, fillWidth);

        // Shadow
        canvas.DrawText(text,
            textX + _textShadowOffsetX * DpiScale,
            textY + _textShadowOffsetY * DpiScale,
            fontSize,
            _textShadowColor,
            _percentFontFamily,
            false,
            false);

        // Main text
        canvas.DrawText(text,
            textX,
            textY,
            fontSize,
            textColor,
            _percentFontFamily,
            _percentBold,
            _percentItalic);
    }

    private KxColor ComputeContrastColor(float textX, float textWidth, Rectangle rect, float fillWidth) {
        float textCenterX = textX + textWidth / 2f;
        bool overFill = textCenterX <= rect.Left + fillWidth;

        KxColor baseColor = overFill ? _fillColor : _backgroundColor;

        float r = baseColor.R / 255f;
        float g = baseColor.G / 255f;
        float b = baseColor.B / 255f;
        float luminance = 0.2126f * r + 0.7152f * g + 0.0722f * b;

        return luminance > 0.6f ? SKColors.LightSeaGreen.ToKxColor() : SKColors.Orange.ToKxColor();
    }
}
