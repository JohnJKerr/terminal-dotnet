using Terminal.Gui.Drawing;

namespace TerminalDotnet.Terminal;

public static class PreviewCodeAppearance
{
    public static Color ForegroundFor(VisualRole role) => role switch
    {
        VisualRole.CodeComment => new(0x56, 0x5f, 0x89),
        VisualRole.CodeKeyword => new(0x7d, 0xcf, 0xff),
        VisualRole.CodeString => new(0x9e, 0xce, 0x6a),
        VisualRole.CodeNumber => new(0xff, 0x9e, 0x64),
        VisualRole.CodeOperator => new(0x89, 0xdd, 0xff),
        VisualRole.CodeType => new(0x2a, 0xc3, 0xde),
        VisualRole.CodePreprocessor => new(0x7d, 0xcf, 0xff),
        VisualRole.CodeIdentifier => new(0xbb, 0x9a, 0xf7),
        VisualRole.CodeConstant => new(0xff, 0x9e, 0x64),
        VisualRole.CodePunctuation => new(0x2a, 0xc3, 0xde),
        VisualRole.CodeFunctionName => new(0x7a, 0xa2, 0xf7),
        VisualRole.CodeAttribute => new(0x2a, 0xc3, 0xde),
        _ => new(0xc0, 0xca, 0xf5)
    };
}
