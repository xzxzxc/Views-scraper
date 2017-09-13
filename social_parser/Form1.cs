using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using SocialParser.Sourcess;

namespace SocialParser
{
    public partial class Form1 : Form
    {
        private string fName;
        private Dictionary<string, MetricsSource> metricsSources;
        private Dictionary<string, List<NumerableHref>> hrefs;
        private XSSFWorkbook workbook;
        private ISheet sheet;
        private int? colNum;
        private Dictionary<string, string> hrefTemplates;
        private CancellationTokenSource cancellationTokenSource;
        private DateTime startTime;
        private Dictionary<string, ProgressBar> infoBars;
        private Dictionary<string, Label> infoLables;
        private CancellationToken cancellationToken;
        private NewSiteDBChangeForm newSiteDbChangeForm;
        private int LastCellNum;

        public Form1()
        {
            InitializeComponent();
            //ThreadPool.SetMaxThreads(10, 20);
            cancellationTokenSource = new CancellationTokenSource();
            NewSiteDB.LoadDB();
            newSiteDbChangeForm = new NewSiteDBChangeForm();
            //NewsSiteWithWiewCount.Initialize();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            if (!IsInternetConnection())
                MessageBox.Show("Connect to the internet and reastart", "No internet connection");
            ProxyManager.Initialize();
            hrefs = new Dictionary<string, List<NumerableHref>>();
            metricsSources = new Dictionary<string, MetricsSource>();
            hrefTemplates = new Dictionary<string, string>();
            InitializeInfoRegion();
#if !DEBUG
            button3.Hide();
            button6.Hide();
#endif
            hrefTemplates.Add("FB", "facebook.com");
            hrefTemplates.Add("VK", "vk.com");
            hrefTemplates.Add("Youtube", "youtube.com");
            hrefTemplates.Add("IG", "instagram.com");
            hrefTemplates.Add("Twitter", "twitter.com");
            hrefTemplates.Add("NewsSite", ".");
            metricsSources.Add("FB" , new FB());
            metricsSources.Add("VK", new VK());
            metricsSources.Add("Youtube", new Youtube());
            metricsSources.Add("IG", new IG());
            metricsSources.Add("Twitter", new Twitter());
            metricsSources.Add("NewsSite", new NewsSite());
            foreach (var key in metricsSources.Keys)
                hrefs.Add(key, new List<NumerableHref>());

            UpdateProxyLabel();
        }
        

        private void UpdateProxyLabel()
        {
            label15.Text = ProxyManager.IsDefined
                ? $@"Current proxy: {ProxyManager.CurrentProxy.Address.ToString().Split('/')[2]}"
                : "No proxy in use";
        }

        private void InitializeInfoRegion()
        {
            infoLables = new Dictionary<string, Label>();
            infoLables.Add("FB", label5);
            infoLables.Add("VK", label4);
            infoLables.Add("Youtube", label11);
            infoLables.Add("IG", label9);
            infoLables.Add("Twitter", label13);
            infoLables.Add("NewsSite", label7);
            infoBars = new Dictionary<string, ProgressBar>();
            infoBars.Add("FB", progressBar2);
            infoBars.Add("VK", progressBar1);
            infoBars.Add("Youtube", progressBar5);
            infoBars.Add("IG", progressBar4);
            infoBars.Add("Twitter", progressBar6);
            infoBars.Add("NewsSite", progressBar3);
        }

        private bool CheckInputData()
        {
            if (fName == null)
            {
                ErrorManager.ShowError("No file was selected");
                return true;
            }
            return false;
        }


        private Metrics GetMetrics(string href)
        {
            foreach (var key in hrefTemplates.Keys)
            {
                if (key == "NewsSite")
                {
                    if (StringManipulations.IsHref(href))
                        return metricsSources[key].GetMetrics(href);
                    ErrorManager.LogBadHref(href);
                }
                else if (href.Contains(hrefTemplates[key]))
                    return metricsSources[key].GetMetrics(href);
            }
            throw new ArgumentException("Bad href");
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (CheckInputData()) return;
            timer1.Start();
            startTime = DateTime.Now;
            sheet = (textBox1.Text == "") ? workbook.GetSheetAt(0) : workbook.GetSheet(textBox1.Text);
            if (!IsSheetReadable(sheet))
            {
                ErrorManager.ShowError("Error while reading xml, maybe sheet name is incorect.");
                return;
            }
            ErrorManager.ClearBedHrefs();
            toolStripProgressBar1.Maximum = sheet.LastRowNum;
            toolStripProgressBar1.Value = 1;
            LastCellNum = sheet.GetRow(0).LastCellNum;
            NameNewColumn();
            AddMetrics();
        }

        private void CallBack()
        {
            WriteBookInFile();
            timer1.Stop();
            MessageBox.Show($@"Done in {toolStripStatusLabel2.Text}");
            toolStripStatusLabel2.Text = @"0:00:00";
            ErrorManager.ShowBadHrefs();
        }

        private void AddMetrics()
        {
            if (colNum == null)
            {
                InitializeHrefDict(int.Parse(numericUpDown1.Text) - 1);
            }
            SetStartInfoValues();
            SetValues();
        }

        private void SetStartInfoValues()
        {
            foreach (var key in hrefs.Keys)
            {
                infoBars[key].Maximum = hrefs[key].Count;
                infoBars[key].Value = 0;
                infoLables[key].Text = $@"0/{hrefs[key].Count}";
            }
        }
        private void SetValues()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            cancellationToken.Register(CallBack);
            Task[] tasks = new Task[hrefs.Keys.Count];
            var hArr = hrefs.Keys.ToArray();
            for (int i = 0;i<hrefs.Keys.Count;++i)
            {
                var i1 = i;
                tasks[i]= new Task(()=> { SetValue(hArr[i1]); }, cancellationToken);
                tasks[i].Start();
            }
            Task.WhenAll(tasks).ContinueWith(task => { CallBack(); }, cancellationToken);
            //Task.WaitAll(tasks, cancellationToken);
        }

        private void SetValue(string key)
        {
            foreach (var href in hrefs[key])
            {
                SetValue(href.Href, sheet.GetRow(href.Row));
                cancellationToken.ThrowIfCancellationRequested();
                Increment(toolStripProgressBar1.ProgressBar); // General Bar
                Increment(infoBars[key]);
                Increment(infoLables[key]);
            }
        }

        delegate string StrGetDelegate();
        delegate void StrSetDelegate(string a);
        private void Increment(Label label)
        {
            if (label.InvokeRequired)
            {
                var textTmp0 = ((string) Invoke((StrGetDelegate) Delegate.CreateDelegate(typeof(StrGetDelegate),
                    label, label.GetType().GetProperty("Text").GetGetMethod()))).Split('/');
                Invoke((StrSetDelegate) Delegate.CreateDelegate(typeof(StrSetDelegate),
                    label, label.GetType().GetProperty("Text").GetSetMethod()), $@"{int.Parse(textTmp0[0]) + 1}/{textTmp0[1]}");
                return;
            }
            var textTmp = label.Text.Split('/');
            label.Text = $@"{int.Parse(textTmp[0]) + 1}/{textTmp[1]}";
        }

        private void Increment(ProgressBar bar)
        {
            if (bar.InvokeRequired)
                Invoke(new IntDelegate(bar.Increment), 1);
            else
                bar.Increment(1);
        }

        private void SetValue(string inp, IRow inputRow)
        {
            Metrics metrics = new Metrics(0, "Wasn`t parsed");
            try
            {
                metrics = GetMetrics(inp);
            }
            catch (ArgumentException e)
            {
                if (e.Message == "Bad href")
                    ErrorManager.LogBadHref(inp);
                else
                    throw;
            }
            catch (AggregateException e)
            {
                if (e.InnerException != null && e.InnerException.GetType() == typeof(ArgumentException)
                    && e.InnerException.Message == "Bad href")
                    ErrorManager.LogBadHref(inp);
                else
                    throw;
            }
            catch (Exception e)
            {
                ErrorManager.LogError(e);
                return;
            }
            inputRow.CreateCell(LastCellNum).SetCellValue(metrics.Value);
            inputRow.CreateCell(LastCellNum+1).SetCellValue(metrics.Source);
        }

        private void WriteBookInFile()
        {
            try
            {
                using (FileStream file = new FileStream(Path.GetDirectoryName(fName) +
                                                        @"\new " + Path.GetFileName(fName), FileMode.Create,
                    FileAccess.Write))
                {
                    workbook.Write(file);
                    file.Close();
                }
            }
            catch (IOException e)
            {
                ErrorManager.LogError(e);
            }
        }

        private bool IsSheetReadable(ISheet sheet)
        {
            if (sheet == null)
                return false;
            IRow input_row;
            try
            {
                input_row = sheet.GetRow(0);
                input_row.GetCell(0);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void NameNewColumn()
        {
            sheet.GetRow(0).CreateCell(LastCellNum).SetCellValue("Metrics");
            sheet.GetRow(0).CreateCell(LastCellNum + 1).SetCellValue("Source");
        }

        private void ReadBookFromXls()
        {
            using (FileStream fileStream = new FileStream(fName, FileMode.Open, FileAccess.ReadWrite))
            {
                workbook = new XSSFWorkbook(fileStream);
                fileStream.Close();
            }
            sheet = workbook.GetSheetAt(0);
            if (!IsSheetReadable(sheet))
            {
                ErrorManager.ShowError("Can`t read first sheet");
                return;
            }
            colNum = FindCol();
            if (colNum != null)
            {
                InitializeHrefDict();
                SetStartInfoValues();
                numericUpDown1.Text = (colNum + 1).ToString();
                ShowElapsedTime();
                return;
            }
            MessageBox.Show("Can`t find column with hrefs");
        }

        private void ShowElapsedTime()
        {
            TimeSpan time = TimeSpan.Zero;
            foreach (var key in hrefs.Keys)
            {
                time = TimeSpan.FromTicks(Math.Max(time.Ticks, metricsSources[key].ElapsedWhaitTime.Ticks * hrefs[key].Count));
            }
            MessageBox.Show($@"Operation takes about {time.Hours} hours, {time.Minutes} minutes.");
        }

        private void ClearHrefDict()
        {
            foreach (var key in hrefs.Keys)
                hrefs[key].Clear();
        }
        private void InitializeHrefDict(int ColNum = -1)
        {
            ClearHrefDict();
            for (int i = 0; i < sheet.LastRowNum + 1; i++)
            {
                var input_row = sheet.GetRow(i);
                var inp_cell = input_row.GetCell(colNum == null && ColNum != -1 ? ColNum : colNum.Value)?.ToString();
                foreach (var key in hrefs.Keys)
                {
                    if (inp_cell != null && inp_cell.Contains(hrefTemplates[key]))
                    {
                        hrefs[key].Add(new NumerableHref(i, inp_cell));
                        break;
                    }
                }
            }
        }


        private int? FindCol()
        {
            var maxRowToDetect = sheet.LastRowNum > 10 ? 5 : 0.55 * sheet.LastRowNum;
            for (int colnum = 0; colnum < sheet.GetRow(1).LastCellNum + 1; colnum++)
            {
                int count = 0;
                for (int currRow = 1; currRow < sheet.LastRowNum + 1; currRow++)
                {
                    var input_row = sheet.GetRow(currRow);

                    if (StringManipulations.IsHref(input_row.GetCell(colnum)?.ToString()))
                        count++;

                    if (count > maxRowToDetect)
                        return colnum; 
                }
            }
            return null;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Xls Files (xls, xlsx)|*.xls;*.xlsx";
            openFileDialog1.Title = "Select a Xls File";

            // Show the Dialog.
            // If the user clicked OK in the dialog and
            // a .CUR file was selected, open it.
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fName = openFileDialog1.FileName;
                string onlFname = Path.GetFileName(openFileDialog1.FileName);
                toolStripStatusLabel1.Text = onlFname.Substring(0, Math.Min(onlFname.Length-1, 45));
                try
                {
                    ReadBookFromXls();
                }
                catch (IOException)
                {
                    ErrorManager.ShowError("Cant read file\n");
                    fName = "";
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //string href = @"https://www.facebook.com/InvictusGamesTeamUkraine/videos/1337543446335363/";
            //string href = "https://www.facebook.com/permalink.php?story_fbid=1883055905262188&id=100006733904803";
            //string href = "https://www.instagram.com/p/BNfILFxBpap/";
            //string href = "http://www.youtube.com/watch?v=MQ23aSezOXw";
            //string href = "https://www.facebook.com/UA.EU.NATO/posts/1213446965436588";
            //string href = "https://vk.com/wall-51313372_7463";
            //string href = "https://ukurier.gov.ua/uk/articles/robit-te-sho-mozhete-zrobiti/";
            //string href = "https://vk.com/wall-10_1";
            //string href = "https://www.facebook.com/photo.php?fbid=1104236059611882&set=a.223908427644654.50562.100000764951273&type=3&theater";
            //string href = "https://twitter.com/LanaEzerskaya/status/766594095921893376";
            //href = "https://twitter.com/UaForces/status/766370164715716608";
            //string href = "https://www.facebook.com/groups/791743417610914/permalink/1011052565679997/";
            string href = "https://www.facebook.com/permalink.php?story_fbid=260589007746613&id=100013865470935";
            MessageBox.Show(GetMetrics(href).ToString());
        }

        public static bool IsInternetConnection()
        {
            try
            {
                new WebClient().DownloadString("http://google.com");
            }
            catch (WebException)
            {
                return false;
            }
            return true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            (metricsSources["FB"] as FB).AutorizeWeb();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            (metricsSources["VK"] as VK).Autorize();
        }


        private void button7_Click(object sender, EventArgs e)
        {
            ProxyManager.ChangeProxy();
            UpdateProxyLabel();
            //if (!ProxyManager.ChangeProxy())
            //    MessageBox.Show("It was last proxy, you can try Reset proxy");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel(true);
        }

        public delegate void IntDelegate(int d);

        private void timer1_Tick(object sender, EventArgs e)
        {
            var interval = DateTime.Now - startTime;
            toolStripStatusLabel2.Text = interval.ToString("g").Split(',')[0];
        }

        private void button6_Click(object sender, EventArgs e)
        {

            newSiteDbChangeForm.Show();
        }
    }

    
}
