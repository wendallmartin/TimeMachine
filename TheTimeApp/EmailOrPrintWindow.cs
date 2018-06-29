using System;
using System.Windows.Forms;

namespace TheTimeApp
{
    public partial class EmailOrPrintWindow : Form
    {
        public EmailOrPrintWindow()
        {
            InitializeComponent();
        }

        private void btn_Email_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void btn_Print_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }

        private void btn_Preview_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
