using System;
using System.Collections.Generic;

namespace MedecinPlanning.Model
{
    public class Intern
    {
        private readonly List<DateTime> _contraintes;

        public string Name { get; set; }

        public List<DateTime> Contraintes
        {
            get { return _contraintes; }
        }

        public int NbGardeWeekEnd { get; set; }

        public int NbGardeWeek { get; set; }

        public int NbWorkingDays { get; set; }

        public int NbWeekendDays { get; set; }

        public Intern()
        {
            _contraintes = new List<DateTime>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
