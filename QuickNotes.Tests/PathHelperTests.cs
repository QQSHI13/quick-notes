// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

using System.IO;
using Xunit;

namespace QuickNotes.Tests;

public class PathHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidPath_RejectsEmpty(string? path)
    {
        Assert.False(PathHelper.IsValidPath(path));
    }

    [Fact]
    public void IsValidPath_AcceptsSimpleAbsolutePath()
    {
        Assert.True(PathHelper.IsValidPath(Path.GetTempPath()));
    }

    [Fact]
    public void IsValidPath_RejectsInvalidChars()
    {
        // Pipe is not a valid path char on Windows.
        Assert.False(PathHelper.IsValidPath("C:\\bad\\path|with|pipes"));
    }

    [Fact]
    public void GetUniqueFilePath_ReturnsBaseWhenFree()
    {
        var dir = Path.Combine(Path.GetTempPath(), "qn_test_" + Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(dir);
            var p = PathHelper.GetUniqueFilePath(dir, "Note_2024-01-01_00-00-00", ".md");
            Assert.Equal(Path.Combine(dir, "Note_2024-01-01_00-00-00.md"), p);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetUniqueFilePath_AppendsCounterOnCollision()
    {
        var dir = Path.Combine(Path.GetTempPath(), "qn_test_" + Path.GetRandomFileName());
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Note_X.md"), "");
            File.WriteAllText(Path.Combine(dir, "Note_X (2).md"), "");

            var p = PathHelper.GetUniqueFilePath(dir, "Note_X", ".md");
            Assert.Equal(Path.Combine(dir, "Note_X (3).md"), p);
            Assert.False(File.Exists(p)); // must not overwrite either existing file
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
