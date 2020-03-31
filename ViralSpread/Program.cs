using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Alba.CsConsoleFormat;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
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

        static void Main( string[] args )
        {
            if( !_arguments.ParseCommandLine( args ) )
            {
                Environment.ExitCode = 1;

                Console.WriteLine( "Couldn't parse the command line; press any key to exit" );
                Console.ReadKey();

                return;
            }

            // if help was requested just exit as it will already have been displayed
            if( _arguments.Help )
                return;

            Console.Clear();

            OutputAssumptions();

            ConfigurePopulation();
            ConfigureNeighborhoods();

            var simResults = RunSimulations();
            OutputAverageOfAllSimulations( simResults );

            OutputToExcel(simResults);

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

        private static void OutputToExcel( List<SimulationResult> simResults )
        {
            FileStream excelFS = null;

            while( true )
            {
                Console.Write( "\n\nEnter file name, press return (blank to skip): " );
                var fileName = Console.ReadLine();

                if( string.IsNullOrEmpty( fileName ) )
                    break;

                fileName += ".xlsx";
                var path = Path.Combine( Environment.CurrentDirectory, fileName );

                try
                {
                    excelFS = File.Create( path );
                    break;
                }
                catch( Exception e )
                {
                    Console.WriteLine($"Invalid valid name '{path}'");
                }
            }

            if( excelFS == null )
                return;

            var workbook = new XSSFWorkbook();

            var summarySheet = workbook.CreateSheet( "summary" );

            var rowNum = 0;
            AddNameValueRow<int>(summarySheet, "Population", _arguments.Population, ref rowNum );
            AddNameValueRow<int>( summarySheet, "Number of Neighbors", _arguments.Neighbors, ref rowNum );
            AddNameValueRow<int>( summarySheet, "Days Contagious", _arguments.Contagious, ref rowNum );
            AddNameValueRow<int>( summarySheet, "Contacts per day", _arguments.Interactions, ref rowNum );
            AddNameValueRow<double>( summarySheet, "Chance of transmitting infection per contact", _arguments.Transmission, ref rowNum );
            AddNameValueRow<double>( summarySheet, "Mortality Rate", _arguments.Mortality, ref rowNum );
            AddNameValueRow<int>( summarySheet, "Days to simulate", _arguments.Days, ref rowNum );
            AddNameValueRow<int>( summarySheet, "Simulations to run", _arguments.Simulations, ref rowNum );

            summarySheet.AutoSizeColumn(0);

            var resultsSheet = workbook.CreateSheet( "results" );

            var titleRow = resultsSheet.CreateRow( 0 );
            titleRow.CreateCell(0).SetCellValue("Day");
            titleRow.CreateCell( 1 ).SetCellValue( "Infected" );
            titleRow.CreateCell( 2 ).SetCellValue( "Contagious" );
            titleRow.CreateCell( 3 ).SetCellValue( "Died" );

            rowNum = 1;

            for( var idx = 0; idx < simResults.Count; idx++ )
            {
                var row = resultsSheet.CreateRow( rowNum );

                row.CreateCell( 0 ).SetCellValue( idx+1 );
                row.CreateCell( 1 ).SetCellValue( simResults[ idx ].Infected );
                row.CreateCell( 2 ).SetCellValue( simResults[ idx ].Contagious );
                row.CreateCell( 3 ).SetCellValue( simResults[ idx ].Died );

                rowNum++;
            }

            for( var idx = 0; idx < 4; idx++ )
            {
                resultsSheet.AutoSizeColumn(idx);
            }

            workbook.Write(excelFS);
        }

        private static void AddNameValueRow<T>(ISheet sheet, string name, T value, ref int rowNum )
        {
            var row = sheet.CreateRow( rowNum );
            row.CreateCell( 0 ).SetCellValue( name );

            switch( value )
            {
                case bool xVal:
                    row.CreateCell( 1 ).SetCellValue( xVal );
                    break;

                case string xVal:
                    row.CreateCell( 1 ).SetCellValue( xVal );
                    break;

                case int xVal:
                    row.CreateCell( 1 ).SetCellValue( xVal );
                    break;

                case double xVal:
                    row.CreateCell( 1 ).SetCellValue( xVal );
                    break;

                case IRichTextString xVal:
                    row.CreateCell( 1 ).SetCellValue( xVal );
                    break;

                case DateTime xVal:
                    row.CreateCell( 1 ).SetCellValue( xVal );
                    break;

                default:
                    return;
            }

            rowNum++;
        }
    }
}
