using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;

namespace ViralSpread
{
    public class Arguments
    {
        private readonly RootCommand _rootCommand;

        public Arguments()
        {
            var population = new Option<int>(
                new[] { "-p", "--population" },
                description : "Number of people to simulate" )
            {
                Argument = new Argument<int>(()=>1000)
            };

            population.Argument.AddValidator(
                x => x.GetValueOrDefault<int>() < 100 ? "Population must be >= 100" : null );

            var neighbors = new Option<int>(
                new[] { "-n", "--neighbors" },
                description : "Maximum size of a neighborhood" )
            {
                Argument = new Argument<int>( () => 40 )
            };

            neighbors.Argument.AddValidator( x =>
                x.GetValueOrDefault<int>() < 1 ? "Neighborhood size must be >= 1" : null );

            var contagious = new Option<int>(
                new[] { "-c", "--contagious" },
                description : "Days contagious" )
            {
                Argument = new Argument<int>( () => 10 )
            };

            contagious.Argument.AddValidator( x =>
                x.GetValueOrDefault<int>() < 1 ? "Contagious period must be >= 1 day" : null );

            var interactions = new Option<int>(
                new[] { "-i", "--interactions" },
                description : "Interactions per day" )
            {
                Argument = new Argument<int>( () => 10 )
            };

            interactions.Argument.AddValidator( x =>
                x.GetValueOrDefault<int>() < 0 ? "Interactions per day must be >= 0" : null );

            var transmission = new Option<double>(
                new[] { "-t", "--transmission" },
                description : "Chance of transmitting virus per contact (decimal %; 0.1 = 10%)" )
            {
                Argument = new Argument<double>( () => 0.05 )
            };

            transmission.Argument.AddValidator( x =>
                x.GetValueOrDefault<double>() < 0 || x.GetValueOrDefault<double>() > 1
                    ? "Chance of transmitting virus per contact must be >=0 and <= 1"
                    : null );

            var mortality = new Option<double>(
                new[] { "-m", "--mortality" },
                description : "Mortality rate (decimal %; 0.01 = 1%)" )
            {
                Argument = new Argument<double>( () => 0.01 )
            };

            mortality.Argument.AddValidator( x =>
                x.GetValueOrDefault<double>() < 0 || x.GetValueOrDefault<double>() > 1
                    ? "Mortality rate must be >= 0 and <= 1"
                    : null );

            var days = new Option<int>(
                new[] { "-d", "--days" },
                description : "Days to simulate" )
            {
                Argument = new Argument<int>( () => 60 )
            };

            days.Argument.AddValidator( x => x.GetValueOrDefault<int>() <= 0 ? "Days to simulate must be >= 1" : null );

            var simulations = new Option<int>(
                new[] { "-s", "--simulations" },
                description : "Simulations to run" )
            {
                Argument = new Argument<int>( () => 10 )
            };

            simulations.Argument.AddValidator( x =>
                x.GetValueOrDefault<int>() <= 0 ? "Number of simulations must be >= 1" : null );

            _rootCommand = new RootCommand( "Viral propagation simulator" )
            {
                population,
                neighbors,
                contagious,
                interactions,
                transmission,
                mortality,
                days,
                simulations
            };

            _rootCommand.Handler = CommandHandler.Create( ( Arguments x ) =>
            {
                Population = x.Population;
                Neighbors = x.Neighbors;
                Contagious = x.Contagious;
                Interactions = x.Interactions;
                Transmission = x.Transmission;
                Mortality = x.Mortality;
                Days = x.Days;
                Simulations = x.Simulations;
            } );
        }

        public int Population { get; set; }
        public int Neighbors { get; set; }
        public int Contagious { get; set; }
        public int Interactions { get; set; }
        public double Transmission { get; set; }
        public double Mortality { get; set; }
        public int Days { get; set; }
        public int Simulations { get; set; }

        public bool ParseCommandLine( string[] args ) =>_rootCommand.Invoke( args ) == 0;
    }
}