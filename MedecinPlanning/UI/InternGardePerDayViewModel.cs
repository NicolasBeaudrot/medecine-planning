using MedecinPlanning.Model;

namespace MedecinPlanning.UI
{
    public class InternGardePerDayViewModel
    {
        public WorkingDayType WorkingDayType { get; set; }

        public int NbInterns { get; set; }

        public bool IsWeekendOrHoliday { get; set; }

        public InternGardePerDayViewModel(WorkingDayType workingDayType, int nbInterns, bool isWeekendOrHoliday)
        {
            WorkingDayType = workingDayType;
            NbInterns = nbInterns;
            IsWeekendOrHoliday = isWeekendOrHoliday;
        }
    }
}
