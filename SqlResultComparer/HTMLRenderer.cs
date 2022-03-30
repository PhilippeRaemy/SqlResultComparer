using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mannex.Text.RegularExpressions;
using MoreLinq;

namespace SqlResultComparer
{
    internal class HtmlRenderer
    {
        public bool AutoOpen { get; set; }

        public void Render(IEnumerable<string> lines, string[] parameternames, string[] parametervalues)
        {
            // using 1st parm name and value in the file name
            var parm = parameternames.Length > 0
                ? Regex.Replace($"{parameternames.First()}_{parametervalues.FirstOrDefault()}", @"[ :\\\/\&\?\*]", "_")
                : string.Empty;
            var filename = Path.Combine(Environment.GetEnvironmentVariable("TMP") ?? Environment.GetEnvironmentVariable("temp") ?? @"c:\temp",
                $"{Assembly.GetExecutingAssembly().GetName().Name}.{DateTime.Now:yyyyMMdd_HHmmss}.{parm}.html");
            var inTable = false;
            var inParagraph = false;
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine("<html><style>" +
                                 ".default{color:white; background-color:black;}" +
                                 ".same   {color:white; background-color:green;}" +
                                 ".diff   {color:white; background-color:red  ;}" +
                                 ".new    {color:yellow;background-color:black;}" +
                                 ".new    {color:yellow;background-color:black;}" +
                                 "</style><body>");
                foreach (var line in lines
                    .Where(li => li.Match(@"[^ \-|]").Success))
                {
                    var split = line.Split('|');
                    if (split.Length ==2 && Regex.Match(split[0], "=").Success)
                    {
                        if (inTable)
                        {
                            writer.WriteLine("</table>");
                            inTable = false;
                        }
                        writer.WriteLine($"<span class=\"same\">{line}</span>");
                    }
                    else if (split.Length > 2)
                    {
                        var trClass = Regex.Match(line, "<>").Success
                            ? "diff"
                            : Regex.Match(line, "[<>]").Success
                            ? "new"
                            : Regex.Match(line, "=").Success
                            ? "same"
                            : "default";
                        if (inParagraph)
                        {
                            writer.WriteLine("</p>");
                            inParagraph = false;
                        }
                        if (!inTable)
                        {
                            writer.WriteLine("<table border=1 cellspacing=0>");
                            inTable = true;
                        }
                        writer.WriteLine($"<tr class=\"{trClass}\">");
                        writer.WriteLine(split
                            .Select(fi => $"<td class=\"{TdClass(trClass, fi)}\">{fi}</td>")
                            .ToDelimitedString(string.Empty)
                            );
                        writer.WriteLine("</tr>");
                    }
                    else
                    {
                        if (inTable)
                        {
                            writer.WriteLine("</table>");
                            inTable = false;
                        }
                        writer.WriteLine($"<p class=\"title\">{line}");
                        inParagraph = true;
                    }
                }
                if(inTable) writer.WriteLine("</table>");
                writer.WriteLine("</body></html>");

            }
            if(AutoOpen) Process.Start(filename);
            Console.WriteLine($"Created file {filename}.");
        }

        private static string TdClass(string trClass, string fi)
        {
            return trClass == "new"             ? trClass
                : trClass == "default"          ? trClass
                : Regex.Match(fi, "<>").Success ? "diff" : "same";
        }

    }
}
