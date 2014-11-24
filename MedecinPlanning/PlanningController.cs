using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using MedecinPlanning.Common;
using MedecinPlanning.Model;
using MedecinPlanning.UI;

namespace MedecinPlanning
{
    public class PlanningController : ViewModelBase
    {
        #region Fields and Properties

        private readonly ObservableCollection<InternGardePerDayViewModel> _internGardePerDay;

        private readonly List<Intern> _internes;
        private List<DayAssignment> _dayAssignments;
        private string _computationMessage;

        public string ComputationMessage
        {
            get { return _computationMessage; }
            set
            {
                _computationMessage = value;
                RaisePropertyChanged("ComputationMessage");
            }
        }

        public ObservableCollection<InternGardePerDayViewModel> InternGardePerDay
        {
            get { return _internGardePerDay; }
        }

        #endregion

        public PlanningController()
        {
            _internes = new List<Intern>();
            _dayAssignments = new List<DayAssignment>();

            _internGardePerDay = new ObservableCollection<InternGardePerDayViewModel>
            {
                new InternGardePerDayViewModel(WorkingDayType.Garde24H, 1, true),
                new InternGardePerDayViewModel(WorkingDayType.Journee_8A18H, 1, true),

                new InternGardePerDayViewModel(WorkingDayType.Garde24H, 1, false),
                new InternGardePerDayViewModel(WorkingDayType.Journee_8A18H, 1, false),
                new InternGardePerDayViewModel(WorkingDayType.Journee_9A20H, 2, false)
            };
        }

        public bool ComputePlanning(DateTime startDate, DateTime endDate, AssignmentType assignmentType)
        {
            _internes.Clear();
            _dayAssignments.Clear();
            ComputationMessage = string.Empty;

            // 1 - Read Contraintes.csv
            string[] lines;
            try
            {
                lines = File.ReadAllLines("./Resources/Contraintes.csv");
            }
            catch (Exception)
            {
                ComputationMessage +=
                    "\nImpossible d'ouvrir Contraintes.csv. Le fichier est ouvert par une autre application.";
                return false;
            }

            foreach (var lineValues in lines.Select(line => line.Split(';')))
            {
                if (!_internes.Any())
                {
                    foreach (var interneName in lineValues)
                    {
                        _internes.Add(new Intern { Name = interneName });
                    }
                }
                else
                {
                    for (int i = 0; i < lineValues.Count(); i++)
                    {
                        if (!string.IsNullOrWhiteSpace(lineValues[i]))
                        {
                            _internes[i].Contraintes.Add(DateTime.Parse(lineValues[i], CultureInfo.CurrentCulture));
                        }
                    }
                }
            }

            // 2 - Assign garde
            _dayAssignments = AssignGardes(EachDay(startDate, endDate));

            // 3 - Assign working days
            if (assignmentType == AssignmentType.DaysAndGardes)
            {
                AssignWorkingDays(_dayAssignments);
            }
            else
            {
                foreach (var dayAssignment in _dayAssignments)
                {
                    foreach (var restInterne in _internes.Except(dayAssignment.Assignment.Keys))
                    {
                        dayAssignment.Assignment.Add(restInterne, WorkingDayType.Repos);
                    }
                }
            }

            try
            {
                using (var file = new StreamWriter("./Result.csv"))
                {
                    var header = "Date";
                    var interneIndexes = new Dictionary<Intern, int>();
                    for (int i = 0; i < _internes.Count; i++)
                    {
                        var interne = _internes[i];
                        interneIndexes.Add(interne, i);
                        header += ";" + interne.Name;
                    }
                    file.WriteLine(header);

                    foreach (var dayAssignment in _dayAssignments)
                    {
                        string line = dayAssignment.Date.ToShortDateString();
                        var assignmentIndexes = dayAssignment.Assignment.ToDictionary(assignement => interneIndexes[assignement.Key], assignement => assignement.Value);
                        line = assignmentIndexes.OrderBy(e => e.Key).Aggregate(line, (current, garde) => current + (";" + GardeTypeToString(garde.Value, dayAssignment.Date)));
                        file.WriteLine(line + ";" + String.Join(" - ", dayAssignment.Errors));
                    }
                    file.WriteLine(_internes.OrderBy(e => e.Name).Aggregate("Nb GU - Nb GUW - Nb jour semaine - Nb jour weekend", (current, interne) => current + (";" + interne.NbGardeWeek + "-" + interne.NbGardeWeekEnd + "-" + interne.NbWorkingDays + "-" + interne.NbWeekendDays)));
                }
            }
            catch (Exception)
            {
                ComputationMessage +=
                    "\nImpossible d'enregistrer le résultat dans Result.csv. Le fichier est ouvert par une autre application.";
                return false;
            }

            return true;
        }

        #region Private methods

        private List<DayAssignment> AssignGardes(IEnumerable<DateTime> days)
        {
            var assignments = new List<DayAssignment>();
            var random = new Random(Guid.NewGuid().GetHashCode());

            var gardeWeekendHoliday =
                _internGardePerDay.FirstOrDefault(
                    e => e.IsWeekendOrHoliday && e.WorkingDayType == WorkingDayType.Garde24H);
            var gardeDays =
                _internGardePerDay.FirstOrDefault(
                    e => !e.IsWeekendOrHoliday && e.WorkingDayType == WorkingDayType.Garde24H);

            if (gardeWeekendHoliday != null && gardeDays != null)
            {
                int nbGardeWeekendHoliday = gardeWeekendHoliday.NbInterns;
                int nbGardeDays = gardeDays.NbInterns;

                foreach (var day in days)
                {
                    DayAssignment currentAssignment = new DayAssignment(day);
                    DayAssignment previousAssignment = null;
                    DateTime nextDay = day.AddDays(1);

                    if (assignments.Any())
                    {
                        previousAssignment = assignments[assignments.Count - 1];
                    }

                    // Intern cannot worked before 48h
                    var internsAvailable = _internes.Where(intern => !intern.Contraintes.Contains(day)
                        && !intern.Contraintes.Contains(nextDay)).ToList();
                    if (previousAssignment != null && previousAssignment.Assignment.Count(e => e.Value == WorkingDayType.Garde24H) > 0)
                    {
                        var lastWorkingInterns = previousAssignment.Assignment.Where(e => e.Value == WorkingDayType.Garde24H);
                        foreach (var lastWorkingIntern in lastWorkingInterns)
                        {
                            internsAvailable.Remove(lastWorkingIntern.Key);
                            currentAssignment.Assignment.Add(lastWorkingIntern.Key, WorkingDayType.ReposGarde);
                        }

                        if (assignments.Count > 1)
                        {
                            var previousLastAssignement = assignments[assignments.Count - 2];
                            var previousWorkingInterns =
                                previousLastAssignement.Assignment.Where(e => e.Value == WorkingDayType.Garde24H);
                            foreach (var previousWorkingIntern in previousWorkingInterns)
                            {
                                internsAvailable.Remove(previousWorkingIntern.Key);
                            }
                        }
                    }

                    if (internsAvailable.Count == 0)
                    {
                        currentAssignment.Errors.Add("Aucun interne n'est disponible pour la garde de 24H.");
                        ComputationMessage += "\nAucun interne n'est disponible pour la garde de 24H le " + day.ToShortDateString();
                    }
                    else
                    {
                        // Assign duty for weekend and holiday
                        if (IsHolliday(day))
                        {
                            for (int i = 0; i < nbGardeWeekendHoliday; i++)
                            {
                                int maxGardeWeekEndAssigned = internsAvailable.Select(e => e.NbGardeWeekEnd).Max();
                                int minGardeWeekEndAssigned = internsAvailable.Select(e => e.NbGardeWeekEnd).Min();
                                if (internsAvailable.Any(e => e.NbGardeWeekEnd != maxGardeWeekEndAssigned))
                                {
                                    var internsToRemove = internsAvailable.Where(e => e.NbGardeWeekEnd == maxGardeWeekEndAssigned).ToList();
                                    if ((internsAvailable.Count - internsToRemove.Count) > 0)
                                    {
                                        foreach (var interne in internsToRemove)
                                        {
                                            internsAvailable.Remove(interne);
                                        }
                                    }
                                }

                                var interneMinimumGarde = internsAvailable.Where(e => e.NbGardeWeekEnd == minGardeWeekEndAssigned).ToList();
                                if (interneMinimumGarde.Count > 0 && interneMinimumGarde.Count != internsAvailable.Count)
                                {
                                    var internGarde = interneMinimumGarde[random.Next(interneMinimumGarde.Count)];
                                    internGarde.NbGardeWeekEnd++;
                                    currentAssignment.Assignment.Add(internGarde, WorkingDayType.Garde24H);
                                    internsAvailable.Remove(internGarde);
                                }
                                else
                                {
                                    var internGarde = internsAvailable[random.Next(internsAvailable.Count)];
                                    internGarde.NbGardeWeekEnd++;
                                    currentAssignment.Assignment.Add(internGarde, WorkingDayType.Garde24H);
                                    internsAvailable.Remove(internGarde);
                                }
                            }
                        }
                        else
                        {
                            // Assign duty for week days
                            for (int i = 0; i < nbGardeDays; i++)
                            {
                                int maxGardeWeekAssigned = internsAvailable.Select(e => e.NbGardeWeek).Max();
                                int minGardeWeekAssigned = internsAvailable.Select(e => e.NbGardeWeek).Min();
                                if (internsAvailable.Any(e => e.NbGardeWeek != maxGardeWeekAssigned))
                                {
                                    var internsToRemove =
                                        internsAvailable.Where(e => e.NbGardeWeek == maxGardeWeekAssigned).ToList();
                                    if ((internsAvailable.Count - internsToRemove.Count) > 0)
                                    {
                                        foreach (var interne in internsToRemove)
                                        {
                                            internsAvailable.Remove(interne);
                                        }
                                    }
                                }

                                var interneMinimumGarde =
                                    internsAvailable.Where(e => e.NbGardeWeek == minGardeWeekAssigned).ToList();
                                if (interneMinimumGarde.Count > 0 && interneMinimumGarde.Count != internsAvailable.Count)
                                {
                                    var internGarde = interneMinimumGarde[random.Next(interneMinimumGarde.Count)];
                                    internGarde.NbGardeWeek++;
                                    currentAssignment.Assignment.Add(internGarde, WorkingDayType.Garde24H);
                                    internsAvailable.Remove(internGarde);
                                }
                                else
                                {
                                    var internGarde = internsAvailable[random.Next(internsAvailable.Count)];
                                    internGarde.NbGardeWeek++;
                                    currentAssignment.Assignment.Add(internGarde, WorkingDayType.Garde24H);
                                    internsAvailable.Remove(internGarde);
                                }
                            }
                        }
                    }
                    assignments.Add(currentAssignment);
                }
            }
            
            return assignments;
        }

        private void AssignWorkingDays(List<DayAssignment> assignments)
        {
            var random = new Random(Guid.NewGuid().GetHashCode());

            foreach (var dayAssignment in assignments)
            {
                // 1 - Working days per intern
                var gardesPerDay = new List<WorkingDayType>();
                if (IsHolliday(dayAssignment.Date))
                {
                    foreach (var internGarde in InternGardePerDay.Where(e => e.IsWeekendOrHoliday
                        && e.WorkingDayType != WorkingDayType.Garde24H))
                    {
                        for (int i = 0; i < internGarde.NbInterns; i++)
                        {
                            gardesPerDay.Add(internGarde.WorkingDayType);
                        }
                    }
                }
                else
                {
                    foreach (var internGarde in InternGardePerDay.Where(e => !e.IsWeekendOrHoliday
                        && e.WorkingDayType != WorkingDayType.Garde24H))
                    {
                        for (int i = 0; i < internGarde.NbInterns; i++)
                        {
                            gardesPerDay.Add(internGarde.WorkingDayType);
                        }
                    }
                }

                var internsAvailable = _internes.Where(interne => !interne.Contraintes.Contains(dayAssignment.Date)
                                                                  && !dayAssignment.Assignment.ContainsKey(interne)).ToList();

                if (gardesPerDay.Count > internsAvailable.Count)
                {
                    dayAssignment.Errors.Add("Il manque des internes pour la journee.");
                    ComputationMessage += "\nIl manque des internes pour la journée du " + dayAssignment.Date.ToShortDateString();
                }

                if (internsAvailable.Count > 0)
                {
                    if (IsHolliday(dayAssignment.Date))
                    {
                        int maxWeekendDaysAssigned = internsAvailable.Select(e => e.NbWeekendDays).Max();
                        if (internsAvailable.Count(e => e.NbWeekendDays != maxWeekendDaysAssigned) >= gardesPerDay.Count)
                        {
                            var internsToRemove = internsAvailable.Where(e => e.NbWeekendDays == maxWeekendDaysAssigned).ToList();
                            foreach (var interne in internsToRemove)
                            {
                                internsAvailable.Remove(interne);
                            }
                        }
                    }
                    else
                    {
                        int maxWorkingDaysAssigned = internsAvailable.Select(e => e.NbWorkingDays).Max();
                        if (internsAvailable.Count(e => e.NbWorkingDays != maxWorkingDaysAssigned) >= gardesPerDay.Count)
                        {
                            var internsToRemove = internsAvailable.Where(e => e.NbWorkingDays == maxWorkingDaysAssigned).ToList();
                            foreach (var interne in internsToRemove)
                            {
                                internsAvailable.Remove(interne);
                            }
                        }
                    }
                }

                // Assign all working days
                while (gardesPerDay.Count > 0 && internsAvailable.Count > 0)
                {
                    var gardeType = gardesPerDay[random.Next(gardesPerDay.Count)];
                    var intern = internsAvailable[random.Next(internsAvailable.Count)];

                    dayAssignment.Assignment.Add(intern, gardeType);

                    if (IsHolliday(dayAssignment.Date))
                    {
                        intern.NbWeekendDays++;
                    }
                    else
                    {
                        intern.NbWorkingDays++;
                    }

                    gardesPerDay.Remove(gardeType);
                    internsAvailable.Remove(intern);
                }

                foreach (var interne in _internes)
                {
                    if (!dayAssignment.Assignment.ContainsKey(interne))
                    {
                        dayAssignment.Assignment.Add(interne, WorkingDayType.Repos);
                    }
                }
            }
        }

        private static bool IsHolliday(DateTime date)
        {
            return (date.DayOfWeek == DayOfWeek.Saturday
                    || date.DayOfWeek == DayOfWeek.Sunday
                    || (date.Day == 1 && date.Month == 1)
                    || (date.Day == 6 && date.Month == 4 && date.Year == 2015)
                    || (date.Day == 1 && date.Month == 5)
                    || (date.Day == 8 && date.Month == 5)
                    || (date.Day == 14 && date.Month == 5 && date.Year == 2015)
                    || (date.Day == 25 && date.Month == 5 && date.Year == 2015)
                    || (date.Day == 14 && date.Month == 7)
                    || (date.Day == 15 && date.Month == 8)
                    || (date.Day == 1 && date.Month == 11)
                    || (date.Day == 11 && date.Month == 11)
                    || (date.Day == 25 && date.Month == 12));
        }

        private IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }

        private string GardeTypeToString(WorkingDayType workingDayType, DateTime date)
        {
            switch (workingDayType)
            {
                case WorkingDayType.Repos:
                case WorkingDayType.ReposGarde:
                    return String.Empty;
                case WorkingDayType.Journee_8A18H:
                    return "8H-18H";
                case WorkingDayType.Journee_9A20H:
                    return "9H-20H";
                case WorkingDayType.Garde24H:
                    return IsHolliday(date) ? "GUW" : "GU";
            }
            return String.Empty;
        }

        #endregion
    }
}
