using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Alba.CsConsoleFormat;
using static System.ConsoleColor;
using Document = Alba.CsConsoleFormat.Document;

namespace ViralSpread
{
    public class Program
    {
        private static readonly Random _random = new Random();
        private static readonly LineThickness _hdrThickness = new LineThickness( LineWidth.Single, LineWidth.Single );

        private static Person[] _population;
        private static readonly Arguments _arguments = new Arguments();

        static async Task Main( string[] args )
        {
            if( !_arguments.ParseCommandLine( args ) )
            {
                Environment.ExitCode = 1;

                Console.WriteLine("Press any key to exit");
                Console.ReadKey();

                return;
            }

            OutputAssumptions();

            ConfigurePopulation();
            ConfigureNeighborhoods();

            var simResults = RunSimulations();
            OutputAverageOfAllSimulations( simResults );

            Console.Write("\n\nPress any key to end program");
            Console.ReadKey();
        }

        private static void ConfigurePopulation()
        {
            Console.WriteLine( "Configuring population..." );

            _population = new Person[ _arguments.Population ];

            for( var idx = 0; idx < _arguments.Population; idx++ )
            {
                _population[ idx ] = new Person( idx, _arguments.Contagious, _arguments.Neighbors );
            }
        }

        private static void ConfigureNeighborhoods()
        {
            Console.WriteLine( "Configuring neighborhoods..." );

            while( _population.Any( p => !p.Neighborhood.Initialized ) )
            {
                var person = _population.First( p => !p.Neighborhood.Initialized );

                while( !person.Neighborhood.Initialized )
                {
                    var neighbor = _population[ _random.Next( _arguments.Population ) ];

                    // if we can add the neighbor to the neighborhood, also
                    // add the reciprocal relationship
                    if( !person.Neighborhood.AddNeighbor( neighbor ) )
                        continue;

                    neighbor.Neighborhood.AddNeighbor( person, true );
                }
            }
        }

        private static List<SimulationResult> RunSimulations()
        {
            Console.Write( "\n\nAbout to run simulations. Press any key to continue:" );
            Console.ReadKey();

            Console.Clear();

            var retVal = new List<SimulationResult>();

            for( var idx = 0; idx < _arguments.Simulations; idx++ )
            {
                int dayNum = 0;

                foreach( var result in RunSingleSimulation() )
                {
                    if( idx == 0 )
                        retVal.Add( result );
                    else
                    {
                        retVal[ dayNum ].Contagious += result.Contagious;
                        retVal[ dayNum ].Died += result.Died;
                        retVal[ dayNum ].Infected += result.Infected;
                    }

                    dayNum++;
                }

                OutputSimulationResult( idx, retVal.Last() );
            }

            return retVal;
        }

        private static IEnumerable<SimulationResult> RunSingleSimulation()
        {
            // initialize the population
            foreach( var person in _population )
            {
                person.DayInfected = -1;
                person.DayDied = -1;
            }

            for( var day = 0; day < _arguments.Days; day++ )
            {
                // infect one person to start
                if( day == 0)
                    _population[ _random.Next( _arguments.Population ) ].DayInfected = 0;

                // select people contacted this day and see if they get infected
                foreach( var person in _population.Where(p=>p.IsContagious(day) && p.IsAlive  ) )
                {
                    foreach( var contact in GetPeopleContacted( person ).Where(p=>!p.IsInfected) )
                    {
                        if( _random.NextDouble() <= _arguments.Transmission )
                            contact.DayInfected = day;
                    }
                }

                // remove infected people who die from the population
                // we assume if you are no longer contagious then you survived
                foreach( var infected in _population.Where( p => p.IsContagious(day) && p.IsAlive ) )
                {
                    var mortalityToDate = _arguments.Mortality / _arguments.Contagious;

                    if( _random.NextDouble() <= mortalityToDate )
                        infected.DayDied = day;
                }

                var retVal= new SimulationResult()
                {
                    Contagious = _population.Count( p => p.IsContagious(day) ),
                    Infected = _population.Count( p => p.IsInfected ),
                    Died = _population.Count( p => !p.IsAlive ),
                };

                yield return retVal;
            }
        }

        private static IEnumerable<Person> GetPeopleContacted( Person source )
        {
            var retVal = new List<Person>();

            for( var idx = 0; idx < _arguments.Interactions; idx++ )
            {
                // find a random neighbor
                yield return source.Neighborhood.GetRandomNeighbor();
            }
        }

        private static void OutputAssumptions()
        {
            Console.SetCursorPosition( 0, 0 );

            var doc = new Alba.CsConsoleFormat.Document(
                new Span( "Viral Propagation Simulation" ) { Color = Yellow },
                new Grid()
                {
                    Color = ConsoleColor.Gray,
                    Columns = { GridLength.Auto, GridLength.Auto },
                    Children =
                    {
                        new Cell( "Iterations to run" ) { Stroke = _hdrThickness, Align = Align.Left },
                        new Cell( $"{_arguments.Simulations:n0}" ) { Stroke = _hdrThickness, Align = Align.Right },
                        new Cell( "Population" ) { Stroke = _hdrThickness, Align = Align.Left },
                        new Cell( $"{_arguments.Population:n0}" ) { Stroke = _hdrThickness, Align = Align.Right },
                        new Cell( "Neighborhood size" ) { Stroke = _hdrThickness, Align = Align.Left },
                        new Cell( $"{_arguments.Neighbors:n0}" ) { Stroke = _hdrThickness, Align = Align.Right },
                        new Cell( "Infectious period, days" ) { Stroke = _hdrThickness, Align = Align.Left },
                        new Cell( $"{_arguments.Contagious:n0}" ) { Stroke = _hdrThickness, Align = Align.Right },
                        new Cell( "Contacts per day" ) { Stroke = _hdrThickness, Align = Align.Left },
                        new Cell( $"{_arguments.Interactions:n0}" ) { Stroke = _hdrThickness, Align = Align.Right },
                        new Cell( "Chance of passing on infection per contact" )
                            { Stroke = _hdrThickness, Align = Align.Left },
                        new Cell( $"{100 * _arguments.Transmission:n1}%" )
                            { Stroke = _hdrThickness, Align = Align.Right },
                        new Cell( "Mortality rate" ) { Stroke = _hdrThickness, Align = Align.Left },
                        new Cell( $"{100 * _arguments.Mortality:n1}%" ) { Stroke = _hdrThickness, Align = Align.Right },
                    }
                } );

            ConsoleRenderer.RenderDocument( doc );
        }

        private static void OutputSimulationResult( int simNum, SimulationResult lastDay )
        {
            Console.SetCursorPosition( 0, 0 );

            var simDoc = new Alba.CsConsoleFormat.Document(
                new Grid()
                {
                    Color = ConsoleColor.Gray,
                    Columns = { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto },
                    Children =
                    {
                        new Cell( "Run #" ) { Stroke = _hdrThickness, Align = Align.Center },
                        new Cell( "Infected" ) { Stroke = _hdrThickness, Align = Align.Center },
                        new Cell( "Contagious" ) { Stroke = _hdrThickness, Align = Align.Center },
                        new Cell( "Died" ) { Stroke = _hdrThickness, Align = Align.Center },
                        new Cell( simNum + 1 ) { Align = Align.Center, Color = ConsoleColor.DarkRed },
                        new Cell( $"{lastDay.Infected / _arguments.Simulations:n0}" )
                            { Align = Align.Center, Color = ConsoleColor.Yellow },
                        new Cell( $"{lastDay.Contagious / _arguments.Simulations:n0}" )
                            { Align = Align.Center, Color = ConsoleColor.DarkCyan },
                        new Cell( $"{lastDay.Died / _arguments.Simulations}" )
                            { Align = Align.Center, Color = ConsoleColor.Red }
                    }
                } );

            ConsoleRenderer.RenderDocument( simDoc );
        }

        private static void OutputAverageOfAllSimulations( List<SimulationResult> simResults )
        {
            Console.Write( "\n\nAbout to display average result of all simulations. Press any key to continue:" );
            Console.ReadKey();

            Console.Clear();

            var byDayDoc = new Alba.CsConsoleFormat.Document(
                new Grid()
                {
                    Color = ConsoleColor.Gray,
                    Columns = { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto },
                    Children =
                    {
                        new Cell("Day #"){Stroke = _hdrThickness, Align = Align.Center},
                        new Cell("Infected"){Stroke = _hdrThickness, Align = Align.Center},
                        new Cell("Contagious"){Stroke = _hdrThickness, Align = Align.Center},
                        new Cell("Died"){Stroke = _hdrThickness, Align = Align.Center},
                        simResults.Select((r,i)=>new[]
                        {
                            new Cell(i + 1){Align = Align.Center, Color=ConsoleColor.DarkRed},
                            new Cell($"{r.Infected / _arguments.Simulations:n0}"){Align = Align.Center, Color=ConsoleColor.Yellow},
                            new Cell($"{r.Contagious / _arguments.Simulations:n0}"){Align = Align.Center, Color=ConsoleColor.DarkCyan},
                            new Cell($"{r.Died / _arguments.Simulations:n0}"){Align = Align.Center, Color=ConsoleColor.Red}
                        })
                    }
                } );

            ConsoleRenderer.RenderDocument( byDayDoc );
        }
    }
}
