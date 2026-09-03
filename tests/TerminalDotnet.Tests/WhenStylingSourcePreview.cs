using Terminal.Gui.Drawing;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenStylingSourcePreview
{
    [Fact]
    public void It_uses_the_neovim_tokyonight_palette()
    {
        // Act
        var colours = new Dictionary<VisualRole, Color>
        {
            [VisualRole.CodeComment] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeComment),
            [VisualRole.CodeKeyword] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeKeyword),
            [VisualRole.CodeString] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeString),
            [VisualRole.CodeNumber] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeNumber),
            [VisualRole.CodeOperator] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeOperator),
            [VisualRole.CodeType] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeType),
            [VisualRole.CodePreprocessor] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodePreprocessor),
            [VisualRole.CodeIdentifier] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeIdentifier),
            [VisualRole.CodeConstant] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeConstant),
            [VisualRole.CodePunctuation] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodePunctuation),
            [VisualRole.CodeFunctionName] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeFunctionName),
            [VisualRole.CodeAttribute] = PreviewCodeAppearance.ForegroundFor(VisualRole.CodeAttribute)
        };

        // Assert
        Assert.Equal(
            new Dictionary<VisualRole, Color>
            {
                [VisualRole.CodeComment] = new(0x56, 0x5f, 0x89),
                [VisualRole.CodeKeyword] = new(0x7d, 0xcf, 0xff),
                [VisualRole.CodeString] = new(0x9e, 0xce, 0x6a),
                [VisualRole.CodeNumber] = new(0xff, 0x9e, 0x64),
                [VisualRole.CodeOperator] = new(0x89, 0xdd, 0xff),
                [VisualRole.CodeType] = new(0x2a, 0xc3, 0xde),
                [VisualRole.CodePreprocessor] = new(0x7d, 0xcf, 0xff),
                [VisualRole.CodeIdentifier] = new(0xbb, 0x9a, 0xf7),
                [VisualRole.CodeConstant] = new(0xff, 0x9e, 0x64),
                [VisualRole.CodePunctuation] = new(0x2a, 0xc3, 0xde),
                [VisualRole.CodeFunctionName] = new(0x7a, 0xa2, 0xf7),
                [VisualRole.CodeAttribute] = new(0x2a, 0xc3, 0xde)
            },
            colours);
    }
}
