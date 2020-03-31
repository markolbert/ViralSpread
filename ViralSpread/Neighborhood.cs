using System;
using System.Collections.Generic;
using System.Linq;

namespace ViralSpread
{
    public class Neighborhood
    {
        private readonly Random _random = new Random();
        private readonly List<Person> _neighbors = new List<Person>();

        private Person[] _fixed;

        public Neighborhood( Person root, int maxNeighbors )
        {
            Root = root ?? throw new NullReferenceException( nameof(root) );
            
            if( maxNeighbors <= 0)
                throw new ArgumentOutOfRangeException($"{nameof(maxNeighbors)} must be >= 1");

            TargetSize = maxNeighbors;
        }

        public Person  Root { get; }
        public int TargetSize { get; }
        public Person[] Neighbors => _fixed ??= _neighbors.ToArray();

        public Person GetRandomNeighbor()
        {
            var idx = _random.Next( Neighbors.Length );
            return _fixed[ idx ];
        }

        public int NumNeighbors => _neighbors.Count();
        public bool Initialized => NumNeighbors >= TargetSize;

        public bool AddNeighbor( Person toAdd, bool force = false )
        {
            if( toAdd == null )
                return false;

            if( toAdd.ID == Root.ID )
                return false;

            if( force || NumNeighbors < TargetSize )
            {
                _neighbors.Add( toAdd );
                _fixed = null;
            }

            return true;
        }
    }
}