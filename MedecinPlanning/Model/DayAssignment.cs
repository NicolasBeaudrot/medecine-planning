using System;
using System.Collections.Generic;

namespace MedecinPlanning.Model
{
    public enum WorkingDayType
    {
        Repos, ReposGarde, Journee_9A20H, Journee_8A18H, Garde24H
    }

    public enum AssignmentType
    {
        Gardes, DaysAndGardes
    }

    public class DayAssignment
    {
        private readonly Dictionary<Intern, WorkingDayType> _assignment;
        private readonly List<string> _errors;
        private readonly DateTime _date;

        public DateTime Date
        {
            get { return _date; }
            
        }

        public Dictionary<Intern, WorkingDayType> Assignment
        {
            get { return _assignment; }
        }

        public List<string> Errors
        {
            get { return _errors; }
        }

        public DayAssignment(DateTime date)
        {
            _date = date;
            _assignment = new Dictionary<Intern, WorkingDayType>();
            _errors = new List<string>();
        }

        public override string ToString()
        {
            return _date.ToShortDateString() + " Assignments:" + _assignment.Count;
        }
    }
}
