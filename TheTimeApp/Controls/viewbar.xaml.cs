using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TheTimeApp.Controls
{
    public delegate void ViewdDel(ViewBar view);

    
    /// <summary>
    /// Interaction logic for ViewBar.xaml
    /// </summary>
    public partial class ViewBar
    {
        private Brush _brushSelected;

        private Brush _brushUnSelected;

        public ViewdDel SelectedEvent;

        public ViewdDel DeleteEvent;

        public bool Deletable { get; set; } = true;

        public bool ReadOnly { get; set; }
        
        public ViewBar()
        {
            InitializeComponent();
            btn_Delete.Visibility = Visibility.Hidden;
        }

        public string Text
        {
            get => (string) txt_Box.Content;
            set => txt_Box.Content = value;
        }

        public Brush BrushUnselected
        {
            get => _brushUnSelected;
            set{
                _brushUnSelected = value;
                Background = _brushUnSelected;
            }
        }

        public Brush BrushSelected
        {
            get => _brushSelected;
            set => _brushSelected = value;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            Background = _brushSelected;
            
            if(Deletable)
                btn_Delete.Visibility = Visibility.Visible;
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
            if(btn_Delete.IsMouseOver || ReadOnly)
                return;
            
            SelectedEvent?.Invoke((ViewBar)sender);
            e.Handled = true;
        }

        private void OnDeleteButtonDown(object sender, MouseButtonEventArgs e)
        {
            DeleteEvent?.Invoke(this);
            e.Handled = true;
        }
    }
}
