using System.Drawing;
using System.Windows.Forms;
using SocialParser.Properties;

namespace SocialParser
{
    public class LoginForm
    {
        private TextBox loginTextBox;
        private TextBox passwordTextBox;
        private Form form;
        public bool Visible => form.Visible;

        public LoginForm(string text)
        {
            form = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(190, 180),
                Icon = Resources.Social_Parser
            };
            Label label = new Label
            {
                Location = new Point(50, 10),
                Size = new Size(150, 15),
                Text = text
            };
            Label loginLabel = new Label
            {
                Location = new Point(10, 25),
                Size = new Size(150, 15),
                Text = @"Please, enter login below"
            };
            loginTextBox = new TextBox {Location = new Point(10, 40), Size = new Size(150, 15)};
            Label passwordLabel = new Label
            {
                Location = new Point(10, 65),
                Size = new Size(150, 15),
                Text = @"Please, enter password below"
                
            };
            passwordTextBox = new TextBox
            {
                Location = new Point(10, 80),
                Size = new Size(150, 15),
                PasswordChar = '*'
            };
            Button okButton = new Button
            {
                Location = new Point(10, 110),
                Text = @"Ok"
            };
            okButton.Click += (sender, args) =>
            {
                form.DialogResult = DialogResult.OK;
                form.Close();
            };
            Button cancelButton = new Button
            {
                Location = new Point(90, 110),
                Text = @"Cancel"
            };
            cancelButton.Click += (sender, args) =>
            {
                form.DialogResult = DialogResult.Cancel;
                form.Close();
            };
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;
            form.Controls.Add(loginLabel);
            form.Controls.Add(loginTextBox);
            form.Controls.Add(passwordLabel);
            form.Controls.Add(passwordTextBox);
            form.Controls.Add(label);
            form.Controls.Add(okButton);
            form.Controls.Add(cancelButton);
        }
        public bool IsLogining(out string login, out string password)
        {
            var result = form.ShowDialog();
            login = loginTextBox.Text;
            password = passwordTextBox.Text;
            return result == DialogResult.OK;
        }
    }
}