using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LocalizationServiceIntegration;

public static class CyrillicCounter
{
    private static readonly Regex FileRegex = new("\\.(cs|tsx|ts|cshtml|js|py|sql)$", RegexOptions.Compiled);
    private static readonly Regex CyrillicRegex = new("[а-яА-ЯЁё]", RegexOptions.Compiled);

    public static Dictionary<string, int> CountLinesWithCyrillicInFolder(string workingDirectory)
        => Directory.EnumerateFiles(workingDirectory, "*.*", SearchOption.AllDirectories)
            .Where(file => FileRegex.IsMatch(file))
            .AsParallel()
            .Select(
                file => (RelativePath: Path.GetRelativePath(workingDirectory, file),
                    Count: CountLinesWithCyrillicInFile(file))
            )
            .Where(dto => dto.Count != 0)
            .ToDictionary(dto => dto.RelativePath, dto => dto.Count);

    private static int CountLinesWithCyrillicInFile(string filePath)
        => File
            .ReadLines(filePath)
            .Count(line => CyrillicRegex.IsMatch(line));
}