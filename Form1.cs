using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace LANHelper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
        [DllImport("user32")]
        public static extern void LockWorkStation();
        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        bool beginMove = false;
        int currentXPosition;
        int currentYPosition;

        public HttpListener listener;

        private void Form1_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        public bool IsInt(string str)
        {
            try
            {
                int a = Convert.ToInt32(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void startHttpListen(String port)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + port + "/th7/");
            //listener.Prefixes.Add("http://127.0.0.1:"+port+"/th7/");
            listener.Start();
            listener.BeginGetContext(ListenerHandle, listener);
        }

        public String RunCommand(String Command)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.Verb = "runas";
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            
            p.Start();
            p.StandardInput.WriteLine(Command);
            p.StandardInput.AutoFlush = true;
            p.StandardInput.WriteLine("exit");
            string outstr = p.StandardOutput.ReadToEnd();
            p.Close();
            return outstr;

        }

        private void ListenerHandle(IAsyncResult result)
        {
            try
            {
                if (listener.IsListening)
                {
                    listener.BeginGetContext(ListenerHandle, result);
                    HttpListenerContext context = listener.EndGetContext(result);
                    HttpListenerRequest request = context.Request;
                    string content = "";
                    String getData = String.Empty;
                    String ret = "";
                    switch (request.HttpMethod)
                    {
                        case "POST":
                            {
                                Stream stream = context.Request.InputStream;
                                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                                content = reader.ReadToEnd();
                            }
                            break;
                        case "GET":
                            {
                                try
                                {
                                    getData = request.QueryString[0];
                                }catch (Exception)
                                {
                                    getData = "";
                                }
                            }
                            break;
                    }
                    
                    switch (getData)
                    {
                        case "shutdown":
                            Process.Start("shutdown", "/s /t 0");
                            ret = getData;
                            break;
                        case "reboot":
                            Process.Start("shutdown", "/r /t 0");
                            ret = getData;
                            break;
                        case "exit":
                            ExitWindowsEx(0, 0);
                            ret = getData;
                            break;
                        case "lock":
                            LockWorkStation();
                            ret = getData;
                            break;
                        case "sleep":
                            SetSuspendState(false, true, true);
                            ret = getData;
                            break;
                        default:
                            ret= File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory+"index.html");
                            break;
                    }
                    HttpListenerResponse response = context.Response;
                    response.StatusCode = 200;
                    response.ContentType = "text/html;charset=utf-8";
                    response.ContentEncoding = Encoding.UTF8;
                    //response.AppendHeader("Content-Type", "application/json;charset=UTF-8");

                    using (StreamWriter writer = new StreamWriter(response.OutputStream, Encoding.UTF8))
                    {
                        writer.Write(ret);
                        writer.Close();
                        response.Close();
                    }
                }

            }
            catch (Exception)
            {
             
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String s = textBox1.Text;
            if (IsInt(s))
            {
                if (listener != null)
                {
                    MessageBox.Show("已在运行");
                    return;
                }
                RunCommand("netsh http delete urlacl url = http://*:" + s + "/\n" +
                    "netsh http add urlacl url=http://*:" + s + "/ user=Everyone\n" +
                    "netsh advfirewall firewall add rule name=LANHelper dir=in action=allow protocol=TCP localport=" + s+ "\n" +
                    "netsh advfirewall firewall add rule name=LANHelper dir=out action=allow protocol=TCP localport=" + s);
                startHttpListen(s);
            }
            else
            {
                MessageBox.Show("请输入端口");
            }

            
        }
        private void button2_Click(object sender, EventArgs e)
        {
            String s = textBox1.Text;
            RunCommand("netsh http delete urlacl url = http://*:" + s + "/\n" +
                    "netsh advfirewall firewall delete rule name=\"LANHelper\"");
            this.Close();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                beginMove = true;
                currentXPosition = MousePosition.X;
                currentYPosition = MousePosition.Y;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (beginMove)
            {
                this.Left += MousePosition.X - currentXPosition;
                this.Top += MousePosition.Y - currentYPosition;
                currentXPosition = MousePosition.X;
                currentYPosition = MousePosition.Y;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                currentXPosition = 0;
                currentYPosition = 0;
                beginMove = false;
            }
        }
    }
}
