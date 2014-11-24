using System.Windows;
using MedecinPlanning.UI;

namespace MedecinPlanning
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PlanningPresenter _planningPresenter;

        public MainWindow()
        {
            InitializeComponent();
            _planningPresenter = new PlanningPresenter();
            this.DataContext = _planningPresenter;
        }

        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            _planningPresenter.PopupOkIsOpen = false;
        }
    }
}
