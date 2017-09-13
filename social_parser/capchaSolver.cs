using System.Drawing;
using System.Windows.Forms;
using VkNet.Utils.AntiCaptcha;

namespace SocialParser
{
    public class CapchaSolver:ICaptchaSolver
    {
        private Form showImageForm;
        private PictureBox pictureBox;
        private TextBox textBox;

        public string Solve(string url)
        {
            //webClient.DownloadFile(url, "capcha.png");
            pictureBox.Load(url);
            var dilaogResult = showImageForm.ShowDialog();
            if (dilaogResult == DialogResult.OK)
                return textBox.Text;
            return Solve(url);
        }

        public void CaptchaIsFalse()
        {
            ErrorManager.ShowError("Last capcha was false.");
        }

        private void Initilize()
        {
            showImageForm = new Form();
            showImageForm.FormBorderStyle = FormBorderStyle.FixedSingle;
            showImageForm.MaximizeBox = false;
            showImageForm.MinimizeBox = false;
            pictureBox = new PictureBox();
            pictureBox.Location = new Point(10, 10);
            pictureBox.Size = new Size(150, 100);
            Label label = new Label();
            label.Location = new Point(10, 110);
            label.Size = new Size(150, 15);
            label.Text = @"Please, insert capcha";
            textBox = new TextBox();
            textBox.Location = new Point(10, 130);
            Button okButton = new Button();
            okButton.Location = new Point(10, 150);
            okButton.Text = @"Ok";
            okButton.Click += (sender, args) =>
            {
                showImageForm.DialogResult = DialogResult.OK;
                showImageForm.Close();
            };
            showImageForm.AcceptButton = okButton;
            showImageForm.Controls.Add(pictureBox);
            showImageForm.Controls.Add(textBox);
            showImageForm.Controls.Add(label);
            showImageForm.Controls.Add(okButton);
        }

        public CapchaSolver()
        {
            Initilize();
        }
    }
}