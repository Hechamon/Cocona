using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cocona.Application;
using Cocona.Command;
using Cocona.CommandLine;
using Cocona.ShellCompletion.Candidate;

namespace Cocona.ShellCompletion
{
    public class BashCoconaShellCompletionCodeProvider : ICoconaShellCompletionCodeProvider
    {
        private readonly string _appName;
        private readonly string _appCommandName;
        private readonly ICoconaCompletionCandidates _completionCandidates;

        public IReadOnlyList<string> Targets { get; } = new[] {"bash"};

        public BashCoconaShellCompletionCodeProvider(
            ICoconaApplicationMetadataProvider applicationMetadataProvider,
            ICoconaCompletionCandidates completionCandidates
        )
        {
            _appName = Regex.Replace(applicationMetadataProvider.GetProductName(), "[^a-zA-Z0-9_]", "__");
            _appCommandName = applicationMetadataProvider.GetExecutableName();
            _completionCandidates = completionCandidates;
        }

        public void Generate(TextWriter writer, CommandCollection commandCollection)
        {
            writer.WriteLine($"#!/bin/bash");
            writer.WriteLine($"# Generated by Cocona {nameof(BashCoconaShellCompletionCodeProvider)}");

            // root
            WriteRootCommandDefinition(writer, commandCollection);

            // Write common.sh
            using (var reader = new StreamReader(typeof(BashCoconaShellCompletionCodeProvider).Assembly.GetManifestResourceStream("Cocona.ShellCompletion.Resources.bash_common.sh")))
            {
                writer.Write(reader.ReadToEnd().Replace("APPCOMMANDNAMEPLACEHOLDER", _appCommandName).Replace("APPNAMEPLACEHOLDER", _appName));
            }
        }

        public void GenerateOnTheFlyCandidates(TextWriter writer, IReadOnlyList<CompletionCandidateValue> values)
        {
            writer.Write(string.Join(" ", values.Select(x => x.Value)));
        }

        private void WriteRootCommandDefinition(TextWriter writer, CommandCollection commandCollection)
        {
            var subCommands = commandCollection.All.Where(x => !x.IsHidden && !x.IsPrimaryCommand).ToArray();

            writer.WriteLine($"__cocona_{_appName}_commands_root() {{");
            foreach (var command in subCommands)
            {
                writer.WriteLine($"    __cocona_{_appName}_completion_define_command \"{command.Name}\"");
            }
            if (commandCollection.Primary != null)
            {
                WriteCommandDefineOptionAndArguments(writer, commandCollection.Primary);
            }
            writer.WriteLine($"    __cocona_{_appName}_completion_handle");
            writer.WriteLine("}");
            writer.WriteLine();

            // sub-commands
            foreach (var subCommand in subCommands)
            {
                WriteCommandDefinition(writer, $"root_{subCommand.Name}", subCommand);
            }
        }

        private void WriteCommandDefinition(TextWriter writer, string commandName, CommandDescriptor command)
        {
            var subCommands = command.SubCommands?.All.Where(x => !x.IsHidden && !x.IsPrimaryCommand).ToArray() ?? Array.Empty<CommandDescriptor>();

            writer.WriteLine($"__cocona_{_appName}_commands_{commandName}() {{");
            foreach (var subCommand in subCommands)
            {
                writer.WriteLine($"    __cocona_{_appName}_completion_define_command \"{subCommand.Name}\"");
            }
            
            WriteCommandDefineOptionAndArguments(writer, command);

            writer.WriteLine($"    __cocona_{_appName}_completion_handle");
            writer.WriteLine("}");
            writer.WriteLine();

            foreach (var subCommand in subCommands)
            {
                WriteCommandDefinition(writer, $"{commandName}_{subCommand.Name}", subCommand);
            }
        }

        private void WriteCommandDefineOptionAndArguments(TextWriter writer, CommandDescriptor command)
        {
            foreach (var option in command.Options.Where(x => !x.IsHidden))
            {
                writer.WriteLine($"    __cocona_{_appName}_completion_define_option \"--{option.Name}\" \"{FromOptionToCandidatesType(option)}\"");
            }
            foreach (var arg in command.Arguments)
            {
                writer.WriteLine($"    __cocona_{_appName}_completion_define_argument \"--{arg.Name}\" \"{FromArgumentToCandidatesType(arg)}\"");
            }

            string FromOptionToCandidatesType(CommandOptionDescriptor option)
            {
                if (option.OptionType == typeof(bool))
                {
                    return "bool";
                }
                else
                {
                    var candidates = _completionCandidates.GetStaticCandidatesFromOption(option);
                    if (candidates.IsOnTheFly)
                    {
                        return $"onthefly:{option.Name}";
                    }
                    else
                    {
                        return candidates.Result!.ResultType switch
                        {
                            CompletionCandidateResultType.Default
                            => "default",
                            CompletionCandidateResultType.File
                            => "file",
                            CompletionCandidateResultType.Directory
                            => "directory",
                            CompletionCandidateResultType.Keywords
                            => $"keywords:{string.Join(":", candidates.Result!.Values.Select(x => x.Value))}",
                            _
                            => "default",
                        };
                    }
                }
            }

            string FromArgumentToCandidatesType(CommandArgumentDescriptor argument)
            {
                var candidates = _completionCandidates.GetStaticCandidatesFromArgument(argument);
                if (candidates.IsOnTheFly)
                {
                    return $"onthefly:{argument.Name}";
                }
                else
                {
                    return candidates.Result!.ResultType switch
                    {
                        CompletionCandidateResultType.Default
                        => "default",
                        CompletionCandidateResultType.File
                        => "file",
                        CompletionCandidateResultType.Directory
                        => "directory",
                        CompletionCandidateResultType.Keywords
                        => $"keywords:{string.Join(":", candidates.Result!.Values.Select(x => x.Value))}",
                        _
                        => "default",
                    };
                }
            }
        }
    }

}
