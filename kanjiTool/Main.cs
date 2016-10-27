using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kanziTool
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == string.Empty)
            {
                MessageBox.Show("警告:请输入字符");
                return;
            }
            string[] info = getKanjiInfo(textBox1.Text);
            if (info[0] == "ERROR")
            {
                MessageBox.Show("获取时出现错误:"+info[1]);
                return;
            }
            string[] translate = getTranslate(textBox1.Text);
            string mesg = string.Format("漢字[{0}]\r\n假名: {1}\r\n罗马拼音: {2}",textBox1.Text,info[0],(info[1]==string.Empty?"获取失败":info[1]));
            MessageBox.Show(string.Format("{0}\r\n{1}\r\n警告:以上内容可能不准确,请根据实际情况分析", mesg,translate[0]=="ERROR"?"翻译: 获取失败":translate[0]== "jp"?"翻译: "+translate[1]:"翻译: 获取失败"));

        }

        string backup = string.Empty;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                try
                {
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData.GetDataPresent(DataFormats.Text))
                    {
                        if (backup == string.Empty || backup!= (string)iData.GetData(DataFormats.UnicodeText))
                        {
                            backup = (string)iData.GetData(DataFormats.UnicodeText);
                            if (backup.Length > 50)
                            {
                                return;
                            }
                            string[] info = getKanjiInfo((string)iData.GetData(DataFormats.UnicodeText));
                            string[] translate = getTranslate((string)iData.GetData(DataFormats.UnicodeText));
                            string mesg = string.Empty;
                            if (info[0] == "ERROR")
                            {
                                if(translate[0] == "ERROR" || translate[0]!="jp")
                                {
                                    return;
                                }
                                mesg = "假名: 获取失败  罗马拼音: 获取失败  翻译: "+translate[1];
                            }
                            else
                            {
                                translate = getTranslate((string)iData.GetData(DataFormats.UnicodeText),"jp");
                                mesg = string.Format("假名: {1}  罗马拼音: {2}  {3}", textBox1.Text, info[0], (info[1] == string.Empty ? "获取失败" : info[1]), (translate[0] == "ERROR" ? "翻译: 获取失败" : translate[0] == "jp" ? "翻译: " + translate[1] : "翻译: 获取失败"));
                            }
                            this.notifyIcon1.ShowBalloonTip(1000, "关于["+ (string)iData.GetData(DataFormats.UnicodeText)+"]:", mesg, ToolTipIcon.None);
                            this.textBox2.Text += "关于[" + (string)iData.GetData(DataFormats.UnicodeText) + "]:"+mesg + Environment.NewLine;
                            this.textBox2.Focus();
                            this.textBox2.Select(this.textBox2.TextLength, 0);
                            this.textBox2.ScrollToCaret();
                        }
                    }
                }
                catch(Exception ex)
                {
                    this.notifyIcon1.ShowBalloonTip(1000, "出现错误:",ex.ToString(), ToolTipIcon.Info);
                }

            }
        }

        string[] getTranslate(string text,string from=null)
        {
            try
            {
                Random ran = new Random();
                int RandKey = ran.Next(1000, 9999999);
                JObject jobj = JObject.Parse(Tool.GetHtml(string.Format("http://api.fanyi.baidu.com/api/trans/vip/translate?q={0}&from={3}&to=zh&appid=20160122000009222&salt={1}&sign={2}",text,RandKey,Tool.StrToMD5("20160122000009222"+text+RandKey+ "EzHTN1B778iYXbKucyAL"),from==null?"auto":from)));
                JObject jo2 = JObject.Parse(jobj["trans_result"].ToString().Replace("[", string.Empty).Replace("]", string.Empty));
                return new string[] { (string)jobj["from"], (string)jo2["dst"] };
            }
            catch
            {
                return new string[] { "ERROR" };
            }
        }

        string[] getKanjiInfo(string kanji)
        {
            try
            {
                string[] array = Tool.GetHtml(string.Format("http://yomikatawa.com/kanji/{0}?search=1", kanji)).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (array[0] == "ERROR")
                {
                    return new string[] { "ERROR", array[1] };
                }
                bool flag = false;
                string[] ret = new string[] { string.Empty,string.Empty};
                int num = 0;
                foreach (string line in array)
                {
                    if (num == 1)
                    {
                        string lm = line.Replace("	", "");
                        lm = lm.Replace("<p>", "");
                        lm = lm.Replace("</p>", "");
                        ret[1] = (lm == "" ? string.Empty : lm);
                        return ret;
                    }
                    if (flag)
                    {
                        string hiragana = line.Replace("	", "");
                        hiragana = hiragana.Replace("<p>", "");
                        hiragana = hiragana.Replace("</p>", "");
                        ret[0] = hiragana;
                        num++;
                    }
                    if (line.Contains("<h1>") && line.Contains("/h1") && line.Contains("<strong>") && line.Contains("</strong>"))
                    {
                        flag = true;
                    }
                }
            }
            catch (Exception ex)
            {
                return new string[] { "ERROR", ex.ToString() };
            }
            return new string[] { "ERROR", "未知错误" };
        }

        private void Main_Load(object sender, EventArgs e)
        {
            this.notifyIcon1.Visible = true;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
            }
        }
    }
}
