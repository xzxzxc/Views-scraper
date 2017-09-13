using System.Windows.Forms;

namespace SocialParser
{
    public class NewSiteDBChangeForm:Form
    {
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn Adress;
        private DataGridViewTextBoxColumn Selector;
        private DataGridViewTextBoxColumn Number;
        private System.ComponentModel.IContainer components;

        public NewSiteDBChangeForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewSiteDBChangeForm));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Adress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Selector = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Number = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Adress,
            this.Selector,
            this.Number});
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(293, 360);
            this.dataGridView1.TabIndex = 0;
            // 
            // Adress
            // 
            this.Adress.DataPropertyName = "Adress";
            this.Adress.HeaderText = "Adress";
            this.Adress.Name = "Adress";
            // 
            // Selector
            // 
            this.Selector.DataPropertyName = "Selector";
            this.Selector.HeaderText = "Selector";
            this.Selector.Name = "Selector";
            // 
            // Number
            // 
            this.Number.DataPropertyName = "Num";
            this.Number.HeaderText = "Num";
            this.Number.Name = "Number";
            this.Number.Width = 50;
            // 
            // NewSiteDBChangeForm
            // 
            this.ClientSize = new System.Drawing.Size(293, 360);
            this.Controls.Add(this.dataGridView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "NewSiteDBChangeForm";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }
    }
}