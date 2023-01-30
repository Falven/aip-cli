# aip-cli

Currently AIP files house code in an encoded format within installer project files which makes it impossible to monitor such code through source control. This can lead to QOL issues.

aip-cli is a .NET 7 CLI utility that perform various tasks around Advanced Installer Project files that attempt to aleviate some of these issues.

## Syntax

`.\aip extract custom-actions --source 'source/file.aip' --destination '.'`

Will extract all custom-action scripts from the project file into separate powershell scripts.

## How it works

The following is an overview into the technical details of how this utility currently works.

### AIP Format

AIP files are UTF-8 encoded XML structured files.

### CLI Options

The utility currently uses Dotnet 7 features along with the `System.CommandLine` library and Dornet XML parsing utilities to extract information from an AIP installer file.

### Custom Actions

Custom Actions allow the installer to:

> - Run your own code: executable files, dynamic linked libraries, VBScript, JavaScript or > Windows PowerShell scripts.
> - Select a custom action by clicking on it, you will be able to set its properties in the right > side pane.
> - Drag and drop custom actions into different installation stages.
> - Right-click on the sequence stage items to access their context menu for more options.

Currently, this aip cli utility only allows you to extract Custom Action Data as Windows PowerShell script files, as this is the use case it is initially intended for. For simplicity, it assumes that all CustomActionData nodes it encounters contain PowerShell scripts. In the future, it can be modified to support inferring the custom action type and script type using some Node matching.

In order to get the name and other metadata of the custom action, You have to match the nodes containing `source="CustomActionData"` to nodes with the following schema:

```xml
<ROW Action="CustomActionName" Type="1" Source="PowerShellScriptLauncher.dll" Target="RunPowerShellScript" Options="1" AdditionalSeq="AI_DATA_SETTER_1"/>
```

This node will give us the CustomActionName which is the script name in the case of PowerShell Script Custom Action types. It also gives us the Action name of the node that contains the custom action data in the `AdditionalSeq` attribute. In the future, this node could also be used to determine the script type to select the appropriate source file extension.

The following is an example matching AIP Node that contains some Custom Action Data.

```xml
<ROW Action="AI_DATA_SETTER_1" Type="51" Source="CustomActionData" Target="SCRIPT_DATA"/>
```

The `Target` attribute will contain the PowerShell script data in Base64 BigEndianUnicode encoded format.

Once decoded, the script will also contain some header information that the utility strips as well as any number of escaped characters matching the regular expression `\[\\(.{1})\]`.

### Future Considerations

As mentioned above:

- Inferring custom action type (script, other).
- Extracting multiple different script types.
- Inferring and respecting installation sequence.
- Inferring and respecting custom-action execution order.
- Extracting custom action metadata such as execution options and dialog stage condition.
- Importing custom action scripts into an installer file.
