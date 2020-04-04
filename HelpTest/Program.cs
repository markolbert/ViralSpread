using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace HelpTest
{
    class Program
    {
        static void Main( string[] args )
        {
            var option = new Option<int>(
                new[] { "-p", "--population" },
                getDefaultValue: () => 1000,
                description: "Number of people to simulate" );

            option.Argument.AddValidator(
                x => x.GetValueOrDefault<int>() <= 0 ? "Population must be >= 1" : null );

            var _rootCommand = new RootCommand()
            {
                option
            };

            var population = -1;

            _rootCommand.Handler = CommandHandler.Create( ( int p ) =>
            {
                population = p;
            } );

            var parseResult = _rootCommand.Invoke( "-h" );

            Console.WriteLine( population < 0
                ? "population argument not parsed"
                : $"population argument parsed ({population})" );
        }
    }
}
