using Cocona.Application;
using Cocona.Help;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocona.Command.Dispatcher.Middlewares
{
    public class CommandHelpMiddleware : CommandDispatcherMiddleware
    {
        private readonly ICoconaHelpRenderer _helpRenderer;
        private readonly ICoconaCommandHelpProvider _commandHelpProvider;
        private readonly ICoconaCommandProvider _commandProvider;
        private readonly ICoconaConsoleProvider _console;

        public CommandHelpMiddleware(CommandDispatchDelegate next, ICoconaHelpRenderer helpRenderer, ICoconaCommandHelpProvider commandHelpProvider, ICoconaCommandProvider commandProvider, ICoconaConsoleProvider console)
            : base(next)
        {
            _helpRenderer = helpRenderer;
            _commandHelpProvider = commandHelpProvider;
            _commandProvider = commandProvider;
            _console = console;
        }

        public override ValueTask<int> DispatchAsync(CommandDispatchContext ctx)
        {
            var unknownOption = ctx.ParsedCommandLine.UnknownOptions.FirstOrDefault();
            if (string.Equals(unknownOption, "h", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(unknownOption, "help", StringComparison.OrdinalIgnoreCase))
            {
                var help = (ctx.Command.IsPrimaryCommand)
                    ? _commandHelpProvider.CreateCommandsIndexHelp(_commandProvider.GetCommandCollection())
                    : _commandHelpProvider.CreateCommandHelp(ctx.Command);

                _console.Output.Write(_helpRenderer.Render(help));
                return new ValueTask<int>(129);
            }

            return Next(ctx);
        }
    }
}
