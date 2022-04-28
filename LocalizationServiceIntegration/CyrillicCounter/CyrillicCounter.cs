using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LocalizationServiceIntegration
{
    public static class CyrillicCounter
    {
        private static readonly Regex FileRegex = new Regex("\\.(cs|tsx|ts|cshtml|js|py|sql)$");
        private static readonly Regex CyrillicRegex = new Regex("[а-яА-ЯЁё]");
			
        public static Dictionary<string, int> CountLinesWithCyrillicInFolder(string workingDirectory)
            => Directory.EnumerateFiles(workingDirectory, "*.*", SearchOption.AllDirectories)
                .Where(file => FileRegex.IsMatch(file))
                .Select(file => (FileName: Path.GetRelativePath(workingDirectory, file), Count: CountLinesWithCyrillicInFile(file)))
                .Where(dto => dto.Count != 0)
                .ToDictionary(dto => dto.FileName, dto => dto.Count);

        private static int CountLinesWithCyrillicInFile(string filePath)
            => File
                .ReadLines(filePath)
                .Count(line => CyrillicRegex.IsMatch(line));
    }
}