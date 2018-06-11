using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TheTimeApp.Controls
{
    /// <summary>
    /// Interaction logic for ViewBar.xaml
    /// </summary>
    public partial class ViewBar : UserControl
    {
        private Brush _brushSelected;

        private Brush _brushUnSelected;

        public delegate void SelectedDel();

        public delegate void DeleteDel(ViewBar viewBar);

        public SelectedDel SelectedEvent;

        public DeleteDel DeleteEvent;

        public ViewBar()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => (string) txt_Box.Content;
            set => txt_Box.Content = value;
        }

        public Brush BrushUnselected
        {
            get => _brushUnSelected;
            set => _brushUnSelected = value;
        }

        public Brush BackBrushSelected
        {
            get => _brushSelected;
            set => _brushSelected = value;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            btn_Delete.Visibility = Visibility.Visible;
            Background = _brushSelected;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (IsMouseOver)
                return;

            btn_Delete.Visibility = Visibility.Hidden;
            Background = BrushUnselected;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedEvent?.Invoke();
        }

        private void OnDeleteButtonDown(object sender, MouseButtonEventArgs e)
        {
            DeleteEvent?.Invoke(this);
        }
    }
}
