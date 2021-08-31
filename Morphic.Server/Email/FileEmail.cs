#if DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Email
{
    /// <summary>
    /// For development: reads an email template, fills in the attributes, and writes the output somewhere.
    /// </summary>
    public class FileEmail : EmailLogger
    {
        public FileEmail(EmailSettings emailSettings, ILogger logger) : base(emailSettings, logger)
        {
        }

        public override async Task<string> SendTemplate(EmailConstants.EmailTypes emailType, Dictionary<string, string> emailAttributes)
        {
            Task<string> result = base.SendTemplate(emailType, emailAttributes);

            string templateFile = $"{Path.Combine(this.emailSettings.FileTemplatePath, emailType.ToString())}.html";
            string outputPath = this.emailSettings.FileOutputPath ?? Path.Combine(Path.GetTempPath(), "morphic-emails");

            Directory.CreateDirectory(outputPath);
            
            string saveAs = Path.Combine(outputPath, this.emailSettings.FileOverwrite
                ? $"{emailType}-output.html"
                : $"{DateTime.Now:yyyyMMddTHHmmss}.{emailType}-output.html");

            // Some generate some debug information.
            StringBuilder debug = new StringBuilder();
            debug.AppendLine($"\n\n<!-- {emailType} {DateTime.Now:R}")
                .AppendLine($"Template: {Path.GetFileName(templateFile)}")
                .AppendLine($"Output: {Path.GetFileName(saveAs)}")
                .AppendLine()
                .AppendLine($"{string.Join("\n", emailAttributes)}")
                .AppendLine();

            HashSet<string> usedKeys = new HashSet<string>();

            string template;
            try
            {
                template = await File.ReadAllTextAsync(templateFile);
            }
            catch (Exception e)
            {
                // Create a basic template, using the email attributes dictionary.
                StringBuilder templateBuilder = new StringBuilder($"<html><body><pre>Email sent {DateTime.Now:R}\n\n");

                templateBuilder.AppendLine(e.Message).AppendLine();

                emailAttributes.Select(attr => $"{attr.Key}: {{{{ {attr.Key} }}}}\n")
                    .ToList()
                    .ForEach(s => templateBuilder.AppendLine(s));
                templateBuilder.AppendLine("</pre></body></html>");

                template = templateBuilder.ToString();
            }

            // Matches simple "handlebars" expressions, containing a key-name.
            Regex fillEmail = new Regex(@"\{\{\s*([^}\s]+)\s*}}");

            // Resolve the key-name with the value in the dictionary
            string content = fillEmail.Replace(template, match =>
            {
                string key = match.Groups[1].Value;
                if (emailAttributes.TryGetValue(key, out string? value))
                {
                    usedKeys.Add(key);
                }
                else
                {
                    debug.AppendLine($"Unknown: {key}");
                }

                return value ?? match.Value;
            });

            debug.AppendLine($"Used: {string.Join(", ", usedKeys)}")
                .AppendLine($"Unused: {string.Join(", ", emailAttributes.Keys.Except(usedKeys))}")
                .AppendLine("-->");

            logger.LogWarning("Debug Email Output {File}", new Uri(saveAs));

            await File.WriteAllTextAsync(saveAs, content + debug, Encoding.UTF8);

            return await result;
        }
    }
}
#endif