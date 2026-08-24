// This file is derived from the Microsoft PowerToys Command Palette sample,
// originally licensed under the MIT license.
// Modifications Copyright (c) QQSHI13, licensed under the GPL-3.0 license.
// See LICENSE for the full GPL-3.0 text.

using System.IO;
using Xunit;

namespace QuickNotes.Tests;

public class NoteTitleHelperTests
{
    [Fact]
    public void ExtractTitle_ReturnsFirstH1()
    {
        var path = WriteTempNote("# My Cool Title\n\nbody");
        try
        {
            Assert.Equal("My Cool Title", NoteTitleHelper.ExtractTitle(path));
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public void ExtractTitle_HandlesSubheadings()
    {
        var path = WriteTempNote("## Subsection Title\n");
        try
        {
            Assert.Equal("Subsection Title", NoteTitleHelper.ExtractTitle(path));
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public void ExtractTitle_ReturnsNullWhenNoHeading()
    {
        var path = WriteTempNote("just some text\nno heading here");
        try
        {
            Assert.Null(NoteTitleHelper.ExtractTitle(path));
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public void IsDefaultTitle_DetectsDefaultTimestampedTitle()
    {
        Assert.True(NoteTitleHelper.IsDefaultTitle("Note 2024-01-01 12:30:45"));
        Assert.True(NoteTitleHelper.IsDefaultTitle("note 2024-01-01"));
        Assert.False(NoteTitleHelper.IsDefaultTitle("Meeting notes"));
    }

    [Fact]
    public void SanitizeFileName_StripsInvalidCharsAndTruncates()
    {
        var name = "Hello/World: *best* ?day?.";
        var safe = NoteTitleHelper.SanitizeFileName(name);
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            Assert.DoesNotContain(c, safe);
        }
        Assert.True(safe.Length <= 50);
        Assert.False(safe.EndsWith('.'));
    }

    [Fact]
    public void SanitizeFileName_TruncatesLongTitles()
    {
        var longTitle = new string('a', 200);
        Assert.Equal(50, NoteTitleHelper.SanitizeFileName(longTitle).Length);
    }

    [Theory]
    [InlineData("NUL")]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("COM1")]
    [InlineData("LPT9")]
    [InlineData("nul")]
    [InlineData("Com1")]
    public void SanitizeFileName_SuffixesReservedDeviceNames(string reserved)
    {
        // Windows cannot create a file whose base name is a DOS device name.
        Assert.Equal(reserved + "_", NoteTitleHelper.SanitizeFileName(reserved));
    }

    [Theory]
    [InlineData("CONTACT")]
    [InlineData("Console Notes")]
    [InlineData("COM10")]
    [InlineData("NULL")]
    public void SanitizeFileName_LeavesNonReservedNamesAlone(string title)
    {
        // Only exact device names are reserved; names that merely start with one
        // must not be altered.
        Assert.Equal(title, NoteTitleHelper.SanitizeFileName(title));
    }

    [Fact]
    public void SanitizeFileName_ReservedNameWithInvalidCharsIsStillSuffixed()
    {
        // "N|U|L" sanitizes down to "NUL", which is reserved.
        Assert.Equal("NUL_", NoteTitleHelper.SanitizeFileName("N|U|L"));
    }

    [Fact]
    public void GetSyncedFileName_ReturnsSanitizedName()
    {
        var path = WriteTempNote("# Project Plan v2\n");
        try
        {
            Assert.Equal("Project Plan v2.md", NoteTitleHelper.GetSyncedFileName(path));
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public void GetSyncedFileName_ReturnsNullForDefaultTitle()
    {
        var path = WriteTempNote("# Note 2024-01-01 12:00:00\n");
        try
        {
            // Default titles are intentionally not turned into filenames to avoid
            // every freshly-created note renaming to "Note ...".
            Assert.Null(NoteTitleHelper.GetSyncedFileName(path));
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public void GetSyncedFileName_ReturnsNullWhenNoHeading()
    {
        var path = WriteTempNote("plain text only");
        try
        {
            Assert.Null(NoteTitleHelper.GetSyncedFileName(path));
        }
        finally { TryDelete(path); }
    }

    private static string WriteTempNote(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), "qn_note_" + Path.GetRandomFileName() + ".md");
        File.WriteAllText(path, content);
        return path;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* test cleanup */ }
    }
}
