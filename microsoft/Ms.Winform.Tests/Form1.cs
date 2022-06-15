using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ms.Winform.Tests
{
    public partial class Form1 : Form
    {
        SynchronizationContext? _synchronizationContext =null;
        public Form1()
        {
           InitializeComponent();
            _synchronizationContext = SynchronizationContext.Current;
            button1.Text = "单个线程修改Ui";
            button2.Text = "通过同步上下调用新线程修改Ui";
            button3.Text = "定时器";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                label1.Text = Thread.CurrentThread.ManagedThreadId.ToString();
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
            Task.Run(() =>
            {
                if (_synchronizationContext != null)
                {
                    _synchronizationContext.Post(state =>
                    {
                        label1.Text = "通过传递SynchronizationContext达到多线程操作Ui";
                    }, null);
                }
            });
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 1;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private int index=0;
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _synchronizationContext?.Post(state =>
            {
                label1.Text = index.ToString();
                index++;
            }, null);
        }
    }
}
