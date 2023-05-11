using HandyControl.Controls;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using MessageBox = HandyControl.Controls.MessageBox;
using TabItem = System.Windows.Controls.TabItem;
using Window = System.Windows.Window;

namespace CronDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static double CurrentYear => DateTime.Now.Year;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtExpression.Text))
            {
                Clipboard.SetDataObject(txtExpression.Text);
            }
        }

        private void BtnReverse_Click(object sender, RoutedEventArgs e)
        {
            //ReverseUi(null);
            var cronStr = txtExpression.Text;
            if (string.IsNullOrEmpty(cronStr))
            {
                return;
            }
            ReloadRunDetail(cronStr);
            var crons = ReverseCron(cronStr);
            if (crons != null)
            {
                try
                {
                    ReverseUi(crons);
                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                }
            }
        }

        /// <summary>
        /// 反解析到ui
        /// </summary>
        /// <param name="crons"></param>
        private void ReverseUi(List<string> crons)
        {
            var tabPages = tab.Items.Cast<TabItem>().Select(x => (Panel)x.Content).ToList();
            for (var i = 0; i < crons.Count; i++)
            {
                var tabControls = tabPages[i].Children;
                //var tabControls = tabPages[i].Content;
                //按表达式特征设置控件状态
                if (crons[i] == "*")
                {
                    foreach (Control control in tabControls)
                    {
                        if (!control.Name.Contains("rdo")) continue;
                        var rdo = (RadioButton)control;
                        rdo.IsChecked = rdo.Name.Contains("Every");
                    }
                }
                if (crons[i].Contains(",") || Common.IsInt(crons[i]))
                {
                    var nums = crons[i].Split(',');
                    foreach (Control control in tabControls)
                    {
                        if (!control.Name.Contains("chk")) continue;
                        var chk = (CheckBox)control;
                        chk.IsChecked = false;
                        foreach (var num in nums)
                        {
                            if (chk.Content.ToString() == num)
                            {
                                chk.IsChecked = true;
                            }
                        }
                    }
                }
                if (crons[i].Contains("-"))
                {
                    var nums = crons[i].Split('-');
                    if (nums.Length != 2)
                    {
                        throw new Exception("表达式格式错误解析\"-\"时失败");
                    }
                    foreach (Control control in tabControls)
                    {
                        if (control.Name.Contains("nud") && control.Name.Contains("Cycle"))
                        {
                            var nud = (NumericUpDown)control;
                            if (nud.Name.Contains("Begin"))
                            {
                                nud.Value = Convert.ToDouble(nums[0]);
                            }
                            if (nud.Name.Contains("End"))
                            {
                                nud.Value = Convert.ToDouble(nums[1]);
                            }
                        }
                    }
                }
                if (crons[i].Contains("/"))
                {
                    var nums = crons[i].Split('/');
                    if (nums.Length != 2)
                    {
                        throw new Exception("表达式格式错误解析\"/\"时失败");
                    }
                    foreach (Control control in tabControls)
                    {
                        if (control.Name.Contains("nud") && control.Name.Contains("Frequency"))
                        {
                            var nud = (NumericUpDown)control;
                            if (nud.Name.Contains("BaseNum"))
                            {
                                nud.Value = Convert.ToDouble(nums[0]);
                            }
                            if (nud.Name.Contains("IntervalNum"))
                            {
                                nud.Value = Convert.ToDouble(nums[1]);
                            }
                        }
                    }
                }
                if (crons[i] == "?")
                {
                    foreach (Control control in tabControls)
                    {
                        if (!control.Name.Contains("rdo")) continue;
                        var rdo = (RadioButton)control;
                        rdo.IsChecked = rdo.Name.Contains("NotSpecify");
                    }
                }
                //特殊处理L C忽略
                if (crons[i].Contains("L"))
                {
                    var chars = crons[i].ToCharArray();
                    foreach (Control control in tabControls)
                    {
                        if (!control.Name.Contains("Last")) continue;
                        if (control.Name.Contains("nud"))
                        {
                            var nud = (NumericUpDown)control;
                            nud.Value = Common.IsInt(chars[0].ToString()) ? Convert.ToDouble(chars[0].ToString()) : 1;
                        }
                        else
                        {
                            if (control.Name.Contains("rdo"))
                            {
                                var rdo = (RadioButton)control;
                                rdo.IsChecked = rdo.Name.Contains("Last");
                            }
                        }
                    }
                }
                //特殊处理W
                if (crons[i].Contains("W"))
                {
                    var rencentNum = crons[i].Substring(0, crons[i].Length - 1);
                    if (rencentNum.Length > 0)
                    {
                        foreach (Control control in tabControls)
                        {
                            if (!(control.Name.Contains("Rencent") && control.Name.Contains("nud"))) continue;
                            var nud = (NumericUpDown)control;
                            nud.Value = Convert.ToDouble(rencentNum);
                        }
                    }
                }
                //特殊处理# 星期专用
                if (crons[i].Contains("#"))
                {
                    var nums = crons[i].Split('#');
                    if (nums.Length == 2)
                    {
                        foreach (Control control in tabControls)
                        {
                            if (!control.Name.Contains("undWeekSpecial")) continue;
                            var nud = (NumericUpDown)control;
                            if (nud.Name.Contains("BaseNum"))
                            {
                                nud.Value = Convert.ToDouble(nums[1]);
                            }
                            if (nud.Name.Contains("Day"))
                            {
                                nud.Value = Convert.ToDouble(nums[0]);
                            }
                        }
                    }
                }

            }
            //年可为空
            if (crons.Count < 7)
            {
                var tabControls = tabPages[6].Children;
                foreach (Control control in tabControls)
                {
                    if (!control.Name.Contains("rdo")) continue;
                    var rdo = (RadioButton)control;
                    rdo.IsChecked = rdo.Name.Contains("NotSpecify");
                }
            }

        }

        private List<string> ReverseCron(string cronStr)
        {
            var crons = cronStr.Split(' ').ToList();
            if (crons.Count >= 6 && crons.Count <= 7)
            {
                txtSecondCron.Text = crons[0];
                txtMinuteCron.Text = crons[1];
                txtHourCron.Text = crons[2];
                txtDayCron.Text = crons[3];
                txtMonthCron.Text = crons[4];
                txtWeekCron.Text = crons[5];
                txtYearCron.Text = string.Empty;
                if (crons.Count == 7)
                {
                    txtYearCron.Text = crons[6];
                }
            }
            else
            {
                return null;
            }
            return crons;
        }

        private void ReloadRunDetail(string cronStr)
        {
            lstBoxRunDetail.Items.Clear();
            try
            {
                var timeList = GetTaskeFireTime(cronStr);
                foreach (var time in timeList)
                {
                    var formatTime = Convert.ToDateTime(time).ToString("yyyy-MM-dd HH:mm:ss dddd");
                    lstBoxRunDetail.Items.Add(formatTime);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Error(ex.Message, "解析错误");
            }
        }

        /// <summary>
        /// 获取任务在未来周期内哪些时间会运行
        /// </summary>
        /// <param name="cronExpression">Cron表达式</param>
        /// <param name="numTimes">运行次数</param>
        /// <returns>运行时间段</returns>
        public static List<string> GetTaskeFireTime(string cronExpression, int numTimes = 50)
        {
            if (numTimes < 0)
            {
                throw new Exception("参数numTimes值大于等于0");
            }
            //时间表达式
            ITrigger trigger = TriggerBuilder.Create().WithCronSchedule(cronExpression).Build();
            IList<DateTimeOffset> dates = TriggerUtils.ComputeFireTimes(trigger as IOperableTrigger, null, numTimes);
            List<string> list = new List<string>();
            foreach (DateTimeOffset dtf in dates)
            {
                list.Add(TimeZoneInfo.ConvertTimeFromUtc(dtf.DateTime, TimeZoneInfo.Local).ToString());
            }
            return list;
        }
    }

    public class Common
    {
        public static bool IsInt(string value)
        {
            return Regex.IsMatch(value, @"^[+-]?\d*$");
        }
    }
}
