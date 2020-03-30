using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace ViralSpread
{
    public class Person
    {
        public Person( int id, int daysInfectious )
        {
            if( id < 0 )
                throw new ArgumentOutOfRangeException( $"{nameof(ID)} cannot be less than 0" );

            ID = id;

            if( daysInfectious <= 0 )
                throw new ArgumentOutOfRangeException( $"{nameof(daysInfectious)} cannot be less than 1" );

            DaysInfectious = daysInfectious;
        }

        public int ID { get; }
        public int DaysInfectious { get; }
        public int LastDayContagious => DayInfected < 0 ? DayInfected : DayInfected + DaysInfectious;
        public int DayInfected { get; set; } = -1;
        public bool IsContagious => LastDayContagious >= 0;
        public bool IsInfected => DayInfected >= 0;
    }

    public class SimulationResult
    {
        public int Infected { get; set; }
        public int Contagious { get; set; }
        public int Died { get; set; }
    }

    class Program
    {
        private static Random _randomizer = new Random();
        private static int _popSize = 10000;
        private static int _infectiousDays = 10;
        private static int _contactsPerDay = 10;
        private static double _chanceOfInfectionPerContact = .05;
        private static int _daysToSimulate = 60;
        private static int _simulationsToRun = 500;
        private static double _mortalityRate = .01;
        private static readonly List<Person> _population = new List<Person>();

        static void Main( string[] args )
        {
            Console.WriteLine("Viral propagation simulation\n");

            Console.WriteLine( $"Iterations: {_simulationsToRun:n0}\n" );
            Console.WriteLine($"Population size: {_popSize:n0}");
            Console.WriteLine( $"Days infectious: {_infectiousDays:n0}" );
            Console.WriteLine( $"Contacts per day: {_contactsPerDay:n0}" );
            Console.WriteLine( $"Chance of infection per contact: {_chanceOfInfectionPerContact*100:n1}%" );
            Console.WriteLine( $"Mortality rate: {_mortalityRate* 100:n1}%\n\n" );

            var daily = new SimulationResult[_daysToSimulate];

            for( var idx = 0; idx < _simulationsToRun; idx++ )
            {
                int dayNum = 0;

                foreach( var result in Simulate() )
                {
                    if( daily[ dayNum ] == null ) daily[ dayNum ] = result;
                    else
                    {
                        daily[ dayNum ].Contagious += result.Contagious;
                        daily[ dayNum ].Died += result.Died;
                        daily[ dayNum ].Infected += result.Infected;
                    }

                    dayNum++;
                }

                Console.Write( "." );
            }

            Console.WriteLine("\n");

            // normalize results
            foreach( var result in daily )
            {
                result.Contagious /= _simulationsToRun;
                result.Died /= _simulationsToRun;
                result.Infected /= _simulationsToRun;
            }

            // display results
            for( int idx = 0; idx < _daysToSimulate; idx++ )
            {
                Console.WriteLine($"Day {idx:n0}:\t{daily[idx].Infected:n0}\t{daily[ idx ].Contagious:n0}\t{daily[ idx ].Died:n0}" );
            }
        }

        private static IEnumerable<SimulationResult> Simulate()
        {
            var retVal = new List<SimulationResult>();

            // initialize the population
            _population.Clear();

            for( var idx = 0; idx < _popSize; idx++ )
            {
                _population.Add( new Person( idx, _infectiousDays ) );
            }

            // infect one person to start
            _population[ _randomizer.Next( _popSize ) ].DayInfected = 0;

            for( var day = 0; day < _daysToSimulate; day++ )
            {
                // select people contacted this day and see if they get infected
                foreach( var person in _population.Where(p=>p.IsInfected  ) )
                {
                    foreach( var contact in GetPeopleContacted( person ).Where(p=>!p.IsInfected) )
                    {
                        if( _randomizer.NextDouble() <= _chanceOfInfectionPerContact )
                            contact.DayInfected = day;
                    }
                }

                // remove infected people who die from the population
                // we assume if you are no longer contagious then you survived
                var died = new List<Person>();

                foreach( var infected in _population.Where( p => p.IsInfected && p.IsContagious ) )
                {
                    if( _randomizer.NextDouble() <= _mortalityRate )
                        died.Add( infected );
                }

                _population.RemoveAll( p => died.Any( d => d.ID == p.ID ) );

                yield return new SimulationResult()
                {
                    Contagious = _population.Count( p => p.IsContagious ),
                    Infected = _population.Count( p => p.IsInfected ),
                    Died = died.Count
                };
            }
        }

        private static IEnumerable<Person> GetPeopleContacted( Person source )
        {
            var retVal = new List<Person>();

            for( var idx = 0; idx < _contactsPerDay; idx++ )
            {
                // find a random contact
                var contactID = _randomizer.Next( _population.Count );

                // ensure the contact isn't the person we're picking contacts for
                while( contactID == source.ID )
                {
                    contactID = _randomizer.Next( _population.Count );
                }

                yield return _population[ contactID ];
            }
        }
    }
}
