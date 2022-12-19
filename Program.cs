using System.Xml;
using System.CommandLine;
using System.Text;
using System.Text.RegularExpressions;

var sourceArgument = new Option<string>(
    new[] { "--source", "--s" },
    "The path to the source Advanced Installer project file.")
{
    Arity = ArgumentArity.ExactlyOne,
    IsRequired = true
};
var destinationOption = new Option<string>(
    new[] { "--destination", "--d" },
    "The path to write the Advanced Installer custom actions to.")
{
    Arity = ArgumentArity.ExactlyOne,
    IsRequired = false
};
var extractCustomActionsCommand = new Command("custom-actions") { sourceArgument, destinationOption };
var extractCommand = new Command("extract") { extractCustomActionsCommand };
var rootCommand = new RootCommand("AIP CLI") { extractCommand };

extractCustomActionsCommand.SetHandler(
    (sourceValue, destinationValue) =>
    {
        if (string.IsNullOrEmpty(destinationValue))
        {
            destinationValue = AppContext.BaseDirectory;
        }

        using (var reader = new StreamReader(sourceValue))
        {
            var doc = new XmlDocument();
            doc.Load(reader);

            var nodes = doc.SelectNodes("//*[@Source='CustomActionData']");
            if (nodes != null)
            {
                foreach (XmlNode scriptNode in nodes)
                {
                    try
                    {
                        var nodeAction = scriptNode?.Attributes?["Action"]?.Value;
                        if (nodeAction == null) { continue; }
                        var base64Target = scriptNode?.Attributes?["Target"]?.Value;
                        if (base64Target == null) { continue; }

                        var nameNode = doc.SelectSingleNode($"//*[@AdditionalSeq='{nodeAction}' and @Target='RunPowerShellScript']");
                        var name = nameNode?.Attributes?["Action"]?.Value;
                        if (name == null) { continue; }

                        byte[] targetData = Convert.FromBase64String(base64Target);
                        var value = Encoding.BigEndianUnicode.GetString(targetData);

                        string scriptHeader = "Script\u0002";
                        var scriptText = value.Substring(value.LastIndexOf(scriptHeader) + scriptHeader.Length);

                        string escapePattern = @"\[\\(.{1})\]";
                        var unescapedScript = Regex.Replace(scriptText, escapePattern, "$1");

                        var destinationPath = Path.Combine(destinationValue, name + ".ps1");
                        File.WriteAllText(destinationPath, unescapedScript);
                    }
                    catch (FormatException)
                    {
                        // Could not base64 decode
                        continue;
                    }
                }
            }
        }
    },
    sourceArgument, destinationOption
);

return await rootCommand.InvokeAsync(args);