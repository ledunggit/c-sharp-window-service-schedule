using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ServiceBai2
{
    public partial class ServiceBai2 : ServiceBase
    {
        System.Timers.Timer timer = new System.Timers.Timer();
        private System.Threading.Timer scheduleTimer;
        public ServiceBai2()
        {
            InitializeComponent();
        }      

        protected override void OnStart(string[] args)        {
            
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 10000;
            timer.Enabled = true;
            scheduleRun();
        }

        private void scheduleRun()
        {
            try
            {
                scheduleTimer = new System.Threading.Timer(new TimerCallback(ScheduleCallBack));
                string modeToRun = ConfigurationManager.AppSettings["Mode"].ToUpper();
                WriteToFile("Service Mode: " + modeToRun);
                DateTime scheduleTime = DateTime.MinValue;
                DateTime endTime = DateTime.MinValue;
                if (modeToRun == "DAILY")
                {
                    scheduleTime = DateTime.Parse(ConfigurationManager.AppSettings["TimeStart"]);
                    WriteToFile(scheduleTime.ToString());
                    if (DateTime.Now > scheduleTime)
                    {
                        scheduleTime = scheduleTime.AddDays(1);
                    }
                    WriteToFile(scheduleTime.ToString());
                }
                if (modeToRun == "INTERVAL")
                {
                    int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);
                    scheduleTime = DateTime.Now.AddMinutes(intervalMinutes);
                    if (DateTime.Now > scheduleTime)
                    {
                        scheduleTime = scheduleTime.AddMinutes(intervalMinutes);
                    }
                }
                if (modeToRun == "STARTSTOP")
                {
                    endTime = DateTime.Parse(ConfigurationManager.AppSettings["TimeStop"]);
                    if (scheduleTime > DateTime.Now || DateTime.Now > endTime)
                    {
                        using (ServiceController serviceController = new ServiceController("Service"))
                        {
                            serviceController.Stop();
                        }
                    }
                }
                if (scheduleTime < DateTime.Now)
                {
                    WriteToFile("The time for scheduled is earlier than now, can not set this.");
                    return;
                }
                TimeSpan timeSpan = new TimeSpan();
                if (modeToRun == "STARTSTOP")
                {
                    timeSpan = endTime.Subtract(scheduleTime);
                } else
                {
                    timeSpan = scheduleTime.Subtract(DateTime.Now);
                }                
                string schedule = string.Format("{0} day {1} hour {2} minute {3} seconds", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                WriteToFile("Service scheduled to run after: " + schedule);
                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);
                scheduleTimer.Change(dueTime, Timeout.Infinite);                    
            }
            catch (Exception ex)
            {
                WriteToFile("Error catch: " + ex.Message);
                using (ServiceController serviceController = new ServiceController("Service"))
                {
                    serviceController.Stop();
                }
            }
        }

        private void ScheduleCallBack(object state)
        {
            
            Process.Start("notepad");
            scheduleRun();
            WriteToFile("Service restart by scheduled successfully.");
        }

        private void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            WriteToFile("Rechecking ...");
            Process[] pname = Process.GetProcessesByName("notepad");
            if (pname.Length == 0)
            {
                WriteToFile("Notepad not running!");
            }
            else
            {
                WriteToFile("Notepad running!");
            }            
        }

        private void WriteToFile(string v)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(DateTime.Now.ToString() + ": " + v);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(DateTime.Now.ToString() + ": " + v);
                }
            }
        }

        protected override void OnStop()
        {
            WriteToFile("The service stopped!");
        }
    }
}
