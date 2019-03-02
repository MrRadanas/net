using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        int time = 60;      // время на восстановление связи

        [DataContract]
        public class Node
        {
            [DataMember]
            public string Ip { get; set; }
            [DataMember]
            public string Name { get; set; }

            public Button Btn { get; set; }

            public int before_error, after_error;

            public Node(string ip, string name)
            {
                Ip = ip;
                Name = name;
            }

            public void addbtn(Button button)
            {
                Btn = button;
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        Node[] nodes;
        private void Form1_Load(object sender, EventArgs e)
        {
            DataContractJsonSerializer jsonFormatterNode = new DataContractJsonSerializer(typeof(Node[]));
            // читаем данные json
            try
            {
                using (FileStream fs = new FileStream("net.json", FileMode.OpenOrCreate))
                {
                    nodes = (Node[])jsonFormatterNode.ReadObject(fs);

                    int i = 16;
                    foreach (Node node in nodes)
                    {
                        i += 21;
                        //Создаем новую кнопку
                        Button newbutton = new Button
                        {
                            Width = 15,
                            Height = 15,
                            BackColor = Color.Red
                        };

                        //Добавляем элемент на форму
                        flowLayoutPanel1.Controls.Add(newbutton);
                        //this.Controls.Add(newbutton);

                        node.addbtn(newbutton);
                        toolTip1.SetToolTip(node.Btn, node.Name);
                        Console.WriteLine("Ip: {0} --- Node: {1}", node.Ip, node.Name);
                    }

                    this.MaximumSize = new Size(i, 540);
                    this.MinimumSize = new Size(i, 40);
                }
            }
            catch
            {
                if (DialogResult.OK == MessageBox.Show("Файл net.json поврежден либо отсутствует!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error))
                    Application.Exit();
            }

            timer1.Start();

            notifyIcon1.ShowBalloonTip(5, "Подсказка", "Нажмите, чтобы отобразить окно", ToolTipIcon.Info);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string error_node1;
            error_node1 = null;
            this.Text = "Net: " + DateTime.Now.ToString();
            notifyIcon1.Text = "Net: " + DateTime.Now.ToString();

            int max_error1 = 0;
            foreach (Node node in nodes)
            {
                if (max_error1 < node.after_error)
                    max_error1 = node.after_error;

                if (node.after_error == 0)
                    node.Btn.BackColor = Color.Green;
                else
                    node.Btn.BackColor = Color.Red;

                if (time / (timer1.Interval / 1000) <= node.before_error && node.after_error == 0)
                    richTextBox1.Text = DateTime.Now.ToString() + " - Связь с узлом " + node.Name + " восстановлена!\r\n" + richTextBox1.Text;

                if (node.after_error * timer1.Interval / 1000 < time && time <= (node.after_error + 1) * timer1.Interval / 1000)
                {
                    error_node1 += "Нет связи с узлом " + node.Name + "!\r\n";
                    richTextBox1.Text = DateTime.Now.ToString() + " - Нет связи с узлом " + node.Name + "!\r\n" + richTextBox1.Text;
                }
            }

            if (time <= (max_error1 + 1) * timer1.Interval / 1000)
                notifyIcon1.Icon = SystemIcons.Error;
            else
                notifyIcon1.Icon = Resource1.success;

            if (!String.IsNullOrEmpty(error_node1))
            {
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
                notifyIcon1.BalloonTipText = error_node1;
                notifyIcon1.BalloonTipTitle = "Ошибка";
                notifyIcon1.ShowBalloonTip(300);
            }

            if (backgroundWorker1.IsBusy != true)
            {
                // Start the asynchronous operation.
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void отобразитьСкрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            form_visible();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            form_visible();
        }

        private void form_visible()
        {
            if (this.Visible)
                this.Hide();
            else
                this.Show();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions
            {
                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                DontFragment = true
            };

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 150;
            PingReply reply;


            foreach (Node node in nodes)
            {
                node.before_error = node.after_error;
                try
                {
                    reply = pingSender.Send(node.Ip, timeout, buffer, options);
                    if (reply.Status == IPStatus.Success)       // вместо всего if можно оставить еррор 0, во всех иных случиях будет срабатывать исключение
                        node.after_error = 0;
                    else
                        node.after_error++;
                }
                catch
                {
                    node.after_error++;
                }
            }

            /* if (reply.Status == IPStatus.Success)
             {
                 node_error[i, 0] = 0;
                 //MessageBox.Show("Address: {0}" + reply.Address.ToString() + "RoundTrip time: {0}" + reply.RoundtripTime + "Time to live: {0}" + reply.Options.Ttl + "Don't fragment: {0}" + reply.Options.DontFragment + "Buffer size: {0}" + reply.Buffer.Length);
                 //Console.WriteLine("Address: {0}", reply.Address.ToString());
                 //Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                 //Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                 //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                 //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
             }
             else
             {
                 node_error[i, 0]++;
             }*/

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //e.Cancel = true;
            //form_visible();
        }
    }
}
