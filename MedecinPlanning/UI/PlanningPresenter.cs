using System;
using System.Windows.Input;
using MedecinPlanning.Common;
using MedecinPlanning.Model;

namespace MedecinPlanning.UI
{
    public class PlanningPresenter : PlanningController
    {
        #region Fields

        private DateTime _startDateTime;
        private DateTime _endDateTime;
        private AssignmentType _assignmentType;
        
        private bool _popupOkIsOpen;
        private string _computationSuccessMessage;

        private readonly ICommand _computePlanning;
        
        #endregion

        #region Properties

        public DateTime StartDateTime
        {
            get { return _startDateTime; }
            set
            {
                _startDateTime = value;
                RaisePropertyChanged("StartDateTime");
            }
        }

        public DateTime EndDateTime
        {
            get { return _endDateTime; }
            set
            {
                _endDateTime = value;
                RaisePropertyChanged("EndDateTime");
            }
        }

        public AssignmentType AssignmentType
        {
            get { return _assignmentType; }
            set
            {
                _assignmentType = value;
                RaisePropertyChanged("AssignmentType");
            }
        }

        public ICommand ComputePlanning
        {
            get { return _computePlanning; }
        }

        public bool PopupOkIsOpen
        {
            get { return _popupOkIsOpen; }
            set
            {
                _popupOkIsOpen = value;
                RaisePropertyChanged("PopupOkIsOpen");
            }
        }

        public string ComputationSuccessMessage
        {
            get { return _computationSuccessMessage; }
            set
            {
                _computationSuccessMessage = value;
                RaisePropertyChanged("ComputationSuccessMessage");
            }
        }

        #endregion

        public PlanningPresenter()
        {
            _startDateTime = DateTime.Now;
            _endDateTime = DateTime.Now.AddMonths(6);
            _assignmentType = AssignmentType.DaysAndGardes;
            
            _computePlanning = new RelayCommand(ComputePlanningExecute);
        }

        private void ComputePlanningExecute()
        {
            if (ComputePlanning(_startDateTime, _endDateTime, _assignmentType))
            {
                ComputationSuccessMessage = "Résultat : " + AppDomain.CurrentDomain.BaseDirectory + "Result.csv";
                PopupOkIsOpen = true;
            }
        }
    }
}
