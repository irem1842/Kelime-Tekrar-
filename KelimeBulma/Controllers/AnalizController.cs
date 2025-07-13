using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using NTextCat;
using System.IO;

namespace KelimeBulma.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalizController : ControllerBase
    {
        [HttpPost("yukle")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile? file, [FromForm] string keyword, [FromForm] string? metin)

        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest("Anahtar kelime eksik.");

            List<string> keywordMatches = new();
            Dictionary<string, int> wordCount = new(StringComparer.OrdinalIgnoreCase);
            string detectedLanguage = "Bilinmiyor";

            try
            {
                if (file != null)
                {
                    string fileExtension = Path.GetExtension(file.FileName);
                    string tempFilePath = Path.GetTempFileName();

                    using (var stream = System.IO.File.Create(tempFilePath))
                    {
                        await file.CopyToAsync(stream);
                    }

                    if (fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        using var pdf = PdfDocument.Open(tempFilePath);
                        int pageNumber = 1;
                        foreach (var page in pdf.GetPages())
                        {
                            var pageLines = page.Text.Split('\n');
                            for (int i = 0; i < pageLines.Length; i++)
                            {
                                if (pageLines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                                {
                                    keywordMatches.Add($"Sayfa {pageNumber}, Satır {i + 1}: {pageLines[i].Trim()}");
                                }
                                CountWords(pageLines[i], wordCount);
                            }
                            pageNumber++;
                        }
                    }
                    else if (fileExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        var lines = await System.IO.File.ReadAllLinesAsync(tempFilePath);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                keywordMatches.Add($"Satır {i + 1}: {lines[i].Trim()}");
                            }
                            CountWords(lines[i], wordCount);
                        }
                    }
                    else
                    {
                        return BadRequest("Sadece .pdf ve .txt dosyalar desteklenmektedir.");
                    }
                }
                else if (!string.IsNullOrEmpty(metin))
                {
                    if (metin.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        keywordMatches.Add($"Metin içinde: {metin}");
                    }
                    CountWords(metin, wordCount);
                }
                else
                {
                    return BadRequest("Lütfen bir dosya ya da metin girin.");
                }

               
                detectedLanguage = DetectLanguage(metin ?? string.Empty);

                var topWords = wordCount.OrderByDescending(kvp => kvp.Value).Take(5)
                    .Select(kvp => $"{kvp.Key}: {kvp.Value} kez").ToList();

                var sb = new StringBuilder();
                sb.AppendLine($"Tespit edilen dil: {detectedLanguage}");
                sb.AppendLine($"\nAnahtar kelime: '{keyword}'\nBulunan Yerler:");
                sb.AppendLine(keywordMatches.Any() ? string.Join("\n", keywordMatches) : "Hiçbir eşleşme bulunamadı.");
                sb.AppendLine("\nEn çok geçen 5 kelime:");
                sb.AppendLine(string.Join("\n", topWords));

                var resultBytes = Encoding.UTF8.GetBytes(sb.ToString());
                var resultFile = new FileContentResult(resultBytes, "text/plain")
                {
                    FileDownloadName = "analiz_sonucu.txt"
                };

                return resultFile;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"İşlem sırasında bir hata oluştu: {ex.Message}");
            }
        }

        private void CountWords(string text, Dictionary<string, int> wordCount)
        {
            var words = Regex.Matches(text.ToLowerInvariant(), "\\b[a-zçğıöşüA-ZÇĞİÖŞÜ]+\\b");
            foreach (Match match in words)
            {
                string word = match.Value;
                if (wordCount.ContainsKey(word))
                    wordCount[word]++;
                else
                    wordCount[word] = 1;
            }
        }

        private string DetectLanguage(string input)
        {
            try
            {
                var factory = new RankedLanguageIdentifierFactory();
                var identifier = factory.Load("Core14.profile.xml"); 
                var languages = identifier.Identify(input);
                return languages.FirstOrDefault()?.Item1?.ToString() ?? "Bilinmiyor";
            }
            catch (Exception ex)
            {
                return $"Dil algılama hatası: {ex.Message}";
            }
        }
    }
}
