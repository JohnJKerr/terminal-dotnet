using Terminal.Gui.Drawing;

namespace TerminalDotnet.Terminal;

public static class PreviewCodeAppearance
{
    public static Color ForegroundFor(VisualRole role) => role switch
    {
        VisualRole.CodeComment => Color.Green,
        VisualRole.CodeKeyword => Color.BrightMagenta,
        VisualRole.CodeString => Color.BrightYellow,
        VisualRole.CodeNumber => Color.BrightCyan,
        VisualRole.CodeOperator => Color.White,
        VisualRole.CodeType => Color.BrightBlue,
        VisualRole.CodePreprocessor => Color.BrightMagenta,
        VisualRole.CodeIdentifier => Color.BrightCyan,
        VisualRole.CodeConstant => Color.BrightCyan,
        VisualRole.CodePunctuation => Color.Gray,
        VisualRole.CodeFunctionName => Color.BrightBlue,
        VisualRole.CodeAttribute => Color.BrightYellow,
        _ => Color.White
    };
}
