﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

namespace TurtleTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IROSBridgeController
    {
        RosBridgeLogic bridgeLogic;


        private AsyncCallback _pfnCallBack;
        private String _pureResponse = "";
        public String _lastLine = "";
        private RosBridgeDotNet.RosBridgeDotNet.TurtlePoseResponse _responseObj = new RosBridgeDotNet.RosBridgeDotNet.TurtlePoseResponse();
        private String[] _lines;
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        enum ConnectionState
        {
            Disconnected = 0, Connected
        }
        int connectionState;
        enum SubscriptionState
        {
            Unsubscribed = 0, Subscribed
        }
        int subscriptionState;


        public class SocketPacket
        {
            public System.Net.Sockets.Socket thisSocket;
            public byte[] dataBuffer = new byte[1];
        }
        public MainWindow()
        {
            InitializeComponent();
            stackControls.Visibility = System.Windows.Visibility.Hidden;
            this.DataContext = _responseObj;
            this.bridgeLogic = new RosBridgeLogic();
            this.connectionState = (int)ConnectionState.Disconnected;
            this.subscriptionState = (int)SubscriptionState.Unsubscribed;
        }
        

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
     
            if (connectionState==(int)(ConnectionState.Disconnected))
            {
                if (ConnectToRos())
                {
                    connectionState = (int)ConnectionState.Connected;
                    btnConnect.Background = Brushes.LightGreen;
                    btnConnect.Content = "Disconnect";
                    stackControls.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                DisconnectFromRos();
                connectionState = (int)ConnectionState.Disconnected;
                btnConnect.Background = Brushes.OrangeRed;
                btnConnect.Content = "Connect";
                stackControls.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void btnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (subscriptionState == (int)SubscriptionState.Unsubscribed)
            {
                try
                {
                    bridgeLogic.sendSubscription("/turtle1/pose");
                    subscriptionState = (int)SubscriptionState.Subscribed;
                    btnSubscribe.Content = "Unsubscribe";
                }
                catch (Exception se)
                {
                    MessageBox.Show(se.Message);
                }
            }
            else
            {
                try
                {
                    bridgeLogic.sendUnsubscribe("/turtle1/pose");
                    subscriptionState = (int)SubscriptionState.Unsubscribed;
                    btnSubscribe.Content = "Subscribe";
                }
                catch (Exception se)
                {
                    MessageBox.Show(se.Message);
                }
            }
        }

        private double convertTextBlocktoRadians()
        {
            double deg = Double.Parse(txtTheta.Text);
            return deg * Math.PI / 180.0;
        }

        private void btnForward_Click(object sender, RoutedEventArgs e)
        {
            PublishturtleMessage(Double.Parse(txtLgth.Text), 0);
        }

        private void btnBackward_Click(object sender, RoutedEventArgs e)
        {
            PublishturtleMessage(-Double.Parse(txtLgth.Text), 0);
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {

            PublishturtleMessage(Double.Parse(txtLgth.Text), convertTextBlocktoRadians());
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            PublishturtleMessage(Double.Parse(txtLgth.Text), -convertTextBlocktoRadians());
            //Update();
        }
        /// <summary>
        /// Return true if connection was succesful
        /// </summary>
        /// <returns></returns>
        /// 

        private static void ConnectCallback(IAsyncResult ar)
        {
            try 
            {
                Socket client = (Socket) ar.AsyncState;
                client.EndConnect(ar);
               
                connectDone.Set();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        bool ConnectToRos()
        {
            if (txtIP.Text == "" || txtPort.Text == "")
            {
                MessageBox.Show("IP Address and Port Number are required to connect to the Server\n");
                return false;
            }
            
            try
            {
                bridgeLogic.Initialize("ws://" + txtIP.Text + ":" + txtPort.Text, this);
                bridgeLogic.Connect();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }
        void DisconnectFromRos()
        {
            bridgeLogic.Disconnect();
        }
        public void WaitForData()
        {
            try
            {
                if (_pfnCallBack == null)
                {
                    _pfnCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();                
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
            catch (NullReferenceException ne)
            {
                MessageBox.Show(ne.Message);
            }

        }
        public void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket theSockId = (SocketPacket)asyn.AsyncState;
                int iRx = theSockId.thisSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(theSockId.dataBuffer, 0, iRx, chars, 0);
                String szData = new String(chars);
                try
                {
                    //RetrieveReceivedEventData(pureResponse);
                    szData = Regex.Replace(szData, @"[^\u0000-\u007F]", String.Empty);
                    //System.Diagnostics.Debug.WriteLine(szData.ToString() + " ");
                    _pureResponse = Regex.Replace(_pureResponse, @"\0", String.Empty);
                    _pureResponse += szData;
                    _lines = _pureResponse.Split(new String[] { "\r\n", "\n" }, StringSplitOptions.None);
                    if (_lines[_lines.Length - 1].Contains("pose\"}"))
                    {
                        _lastLine = _lines[_lines.Length - 1];
                        //MessageBox.Show(_lines[_lines.Length - 1]);
                    }
                    _responseObj = JsonConvert.DeserializeObject<RosBridgeDotNet.RosBridgeDotNet.TurtlePoseResponse>(_lastLine);
                    System.Diagnostics.Debug.WriteLine("x: " + _responseObj.msg.x + " y: " + _responseObj.msg.y);

                }
                catch (Exception e)
                {
                   MessageBox.Show(e.ToString());
                }
                WaitForData();
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }

        private void PublishturtleMessage(double linear, double angular)
        {
            try
            {
                var v = new { linear = linear, angular = angular };
                bridgeLogic.sendPublish("/turtle1/command_velocity", 
                    JObject.Parse(JsonConvert.SerializeObject(v)));
            }
            catch (SocketException se)
            {
                MessageBox.Show(se.Message);
            }
        }

        public delegate void UpdateTextElements(String data);

        public void ReceiveUpdate(String data)
        {
            JObject jsonData = JObject.Parse(data);
            Dispatcher.Invoke(new Action(() => labelX.Content = "x: " + jsonData["msg"]["x"]));
            Dispatcher.Invoke(new Action(() => labelY.Content = "y: " + jsonData["msg"]["y"]));
            Dispatcher.Invoke(new Action(() => labelTheta.Content = "t: " + jsonData["msg"]["theta"] +
                " (Deg: " + Math.Round((float)jsonData["msg"]["theta"] * 180 / Math.PI, 4).ToString() + ")"));
        }
        

    }
}