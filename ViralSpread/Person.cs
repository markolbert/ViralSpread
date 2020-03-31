using System;
using System.Collections.Generic;

namespace ViralSpread
{
    public class Person
    {
        public Person( int id, int daysInfectious, int maxNeighbors )
        {
            if( id < 0 )
                throw new ArgumentOutOfRangeException( $"{nameof(ID)} cannot be less than 0" );

            ID = id;

            if( daysInfectious <= 0 )
                throw new ArgumentOutOfRangeException( $"{nameof(daysInfectious)} cannot be less than 1" );

            DaysInfectious = daysInfectious;

            Neighborhood = new Neighborhood(this, maxNeighbors);
        }

        public int ID { get; }
        public int DaysInfectious { get; }

        public Neighborhood Neighborhood { get; }

        public int DayInfected { get; set; }
        public int LastDayContagious => DayInfected < 0 ? DayInfected : DayInfected + DaysInfectious;
        public int DayDied { get; set; } = -1;

        public bool IsContagious( int dayNum ) => ( DayInfected >= 0 ) && ( LastDayContagious >= dayNum );
        public bool IsInfected => DayInfected >= 0;
        public bool IsAlive => DayDied < 0;
    }
}