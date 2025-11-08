using CommandLine;

namespace DSRomEncryptor;

/// <summary>
/// Class containing the command line options of DSRomEncryptor.
/// </summary>
sealed class CommandLineOptions
{
    [Option("dsidev", Required = false, HelpText = "Use DSi dev blowfish.")]
    public required bool UseDsiDevBlowfish { get; init; }

    [Value(0, MetaName = "input path", Required = true, HelpText = "The path of the input nds file.")]
    public required string InputPath { get; init; }

    [Value(1, MetaName = "output path", Required = true, HelpText = "The path of the output nds file.")]
    public required string OutputPath { get; init; }
}