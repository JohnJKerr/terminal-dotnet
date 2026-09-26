using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests.Files;

public sealed class WhenTheWorkingTreeIsEditedOutside
{
    [Fact]
    public void An_edited_source_file_is_worth_reloading_for()
    {
        // Act
        var matters = WorkspaceEdits.Matters("src/TerminalDotnet/Program.cs");

        // Assert
        Assert.True(matters);
    }

    [Fact]
    public void Build_output_is_not()
    {
        // Act
        var matters = WorkspaceEdits.Matters("src/TerminalDotnet/obj/Debug/Program.g.cs");

        // Assert
        Assert.False(matters);
    }

    [Fact]
    public void Compiled_assemblies_are_not()
    {
        // Act
        var matters = WorkspaceEdits.Matters("src/TerminalDotnet/bin/Debug/TerminalDotnet.dll");

        // Assert
        Assert.False(matters);
    }

    [Fact]
    public void Gits_own_bookkeeping_is_not()
    {
        // Act
        var matters = WorkspaceEdits.Matters(".git/index.lock");

        // Assert
        Assert.False(matters);
    }

    [Fact]
    public void The_scratch_file_an_editor_leaves_behind_is_not()
    {
        // Act
        var matters = WorkspaceEdits.Matters("src/TerminalDotnet/.Program.cs.swp");

        // Assert
        Assert.False(matters);
    }

    [Fact]
    public void Nor_is_the_backup_copy_it_keeps()
    {
        // Act
        var matters = WorkspaceEdits.Matters("src/TerminalDotnet/Program.cs~");

        // Assert
        Assert.False(matters);
    }

    [Fact]
    public void A_folder_whose_name_merely_starts_with_an_ignored_one_still_counts()
    {
        // Act
        var matters = WorkspaceEdits.Matters("src/binding/Order.cs");

        // Assert
        Assert.True(matters);
    }

    [Fact]
    public void A_windows_separator_marks_out_the_same_folders()
    {
        // Act
        var matters = WorkspaceEdits.Matters(@"src\TerminalDotnet\obj\Program.g.cs");

        // Assert
        Assert.False(matters);
    }
}
