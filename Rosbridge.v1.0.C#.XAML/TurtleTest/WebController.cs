﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace TurtleTest
{
    class WebController: RosBridgeUtility.IROSWebTeleopController
    {
        private RosBridgeUtility.RosBridgeConfig conf;

        private HttpServer NeobotixStateServer;
        private RosBridgeUtility.ServerStorage LastMsgs;
        private RosBridgeUtility.ImageStorage LastRGB;
        private RosBridgeUtility.ImageStorage LastDepth;

        RosBridgeUtility.RosBridgeLogic parent;

        public WebController(RosBridgeUtility.RosBridgeConfig conf, RosBridgeUtility.RosBridgeLogic parent)
        {
            this.conf = conf;
            this.parent = parent;
            // Initialize webserver
            NeobotixStateServer = new HttpServer(4649);
            NeobotixStateServer.RootPath = "../../Public";
            Console.WriteLine(NeobotixStateServer.RootPath);

            LastMsgs = new RosBridgeUtility.ServerStorage();
            LastRGB = new RosBridgeUtility.ImageStorage();
            LastDepth = new RosBridgeUtility.ImageStorage();
            NeobotixStateServer.OnGet += (sender, e) =>
            {
                var req = e.Request;
                Console.WriteLine(e.Request.QueryString);
                var res = e.Response;
                var path = req.RawUrl;                
                if (path == "/")
                {
                    path += "index.html";
                }
                path = NeobotixStateServer.RootPath + path;
                //var content = NeobotixStateServer.GetFile(path);
                Console.WriteLine(path);
                Console.WriteLine(File.Exists(path));
                var content = System.IO.File.ReadAllBytes(path);
                
                if (content == null)
                {
                    res.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }
                if (path.EndsWith(".html"))
                {
                    res.ContentType = "text/html";
                    res.ContentEncoding = Encoding.UTF8;
                }
                res.OutputStream.Write(content, 0, content.Length);
            };
            NeobotixStateServer.OnPut += NeobotixStateServer_OnPut;
            NeobotixStateServer.AddWebSocketService<RosBridgeUtility.OdometryBehavior>(("/neobot_odom"), 
                () => new RosBridgeUtility.OdometryBehavior(LastMsgs));
            NeobotixStateServer.AddWebSocketService<RosBridgeUtility.StateBehavior>(("/server_state"),
                () => new RosBridgeUtility.StateBehavior(LastMsgs));
            NeobotixStateServer.AddWebSocketService<RosBridgeUtility.TeleopKeyBehavior>(("/neobot_teleop"),
                () => new RosBridgeUtility.TeleopKeyBehavior(LastMsgs, this));
            NeobotixStateServer.AddWebSocketService<RosBridgeUtility.LaserScanBehavior>(("/neobot_laser"),
                () => new RosBridgeUtility.LaserScanBehavior(LastMsgs));
            NeobotixStateServer.AddWebSocketService<RosBridgeUtility.ImageBehavior>(("/kinect_image"),
                () => new RosBridgeUtility.ImageBehavior(LastRGB));
            NeobotixStateServer.AddWebSocketService<RosBridgeUtility.ImageBehavior>(("/kinect_depth"),
                () => new RosBridgeUtility.ImageBehavior(LastDepth));
        }

        public void teleopTarget(String request)
        {
            JObject msg = JObject.Parse(request);
            switch (msg["command"].ToString().Replace("\"",""))
            {
                case "forward":
                    parent.moveForward(conf.getPublicationList());
                    break;
                case "backward":
                    parent.moveBackward(conf.getPublicationList());
                    break;
                case "left":
                    parent.moveLeft(conf.getPublicationList());
                    break;
                case "right":
                    parent.moveRight(conf.getPublicationList());
                    break;
                case "inc_velocity":
                    parent.increaseVelocity();
                    break;
                case "dec_velocity":
                    parent.decreaseVelocity();
                    break;
                case "stop":
                    parent.stopTarget(conf.getPublicationList());
                    break;
                case "inc_ang":
                    parent.increaseAngVelocity();
                    break;
                case "dec_ang":
                    parent.decreaseAngVelocity();
                    break;
            }
            
        }

        void NeobotixStateServer_OnPut(object sender, HttpRequestEventArgs e)
        {
            Console.WriteLine("Received new PUT message");
        }

        public void startWebServer()
        {
            NeobotixStateServer.Start();
        }

        public void stopWebServer()
        {
            NeobotixStateServer.Stop();
        }

        public void updateVelocityState(RosBridgeUtility.VelocityState msg)
        {
            LastMsgs.lastStateMsg = msg;
        }

        public void updateImageState(ref BitmapSource source, Int32 height, Int32 width, RosBridgeUtility.ImageStorage target)
        {
            target.lastImageMsg = new RosBridgeUtility.ImageState();
            target.lastImageMsg.height = height;
            target.lastImageMsg.width = width;
            var encoder = new JpegBitmapEncoder();
            var frame = BitmapFrame.Create(source);
            encoder.Frames.Add(frame);
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                target.lastImageMsg.pixels = Convert.ToBase64String(stream.ToArray());
            }
        }

        public void updateRGBState(ref BitmapSource source, Int32 height, Int32 width)
        {
            updateImageState(ref source, height, width, LastRGB);
        }

        public void updateDepthState(ref BitmapSource source, Int32 height, Int32 width)
        {
            updateImageState(ref source, height, width, LastDepth);
        }

        public void updateMessages(JObject jsonData)
        {
            //Console.WriteLine(jsonData["topic"]);
            if (jsonData["topic"].ToString().Replace("\"", "").Equals(conf.odometryTopic))
            {
                RosBridgeUtility.OdometryMsg msg = new RosBridgeUtility.OdometryMsg();
                Double x, y, z, angleX, angleY, angleZ, angleW = 0;
                Double.TryParse(jsonData["msg"]["pose"]["pose"]["position"]["x"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out x);
                Double.TryParse(jsonData["msg"]["pose"]["pose"]["position"]["y"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out y);
                Double.TryParse(jsonData["msg"]["pose"]["pose"]["position"]["z"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out z);
                Double.TryParse(jsonData["msg"]["pose"]["pose"]["orientation"]["x"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out angleX);
                Double.TryParse(jsonData["msg"]["pose"]["pose"]["orientation"]["y"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out angleY);
                Double.TryParse(jsonData["msg"]["pose"]["pose"]["orientation"]["z"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out angleZ);
                Double.TryParse(jsonData["msg"]["pose"]["pose"]["orientation"]["w"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out angleW);
                msg.x = x;
                msg.y = y;
                msg.z = z;
                msg.ori_w = angleW;
                msg.ori_x = angleX;
                msg.ori_y = angleY;
                msg.ori_z = angleZ;
                LastMsgs.lastOdom = msg;
            }
            if (jsonData["topic"].ToString().Replace("\"", "").Equals(conf.laserFieldTopic))
            {
                Double angle_inc;
                Double min_angle;
                Double.TryParse(jsonData["msg"]["angle_increment"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out angle_inc);
                Double.TryParse(jsonData["msg"]["angle_min"].ToString(),
                    NumberStyles.Number,
                    CultureInfo.CreateSpecificCulture("en-US"),
                    out min_angle);
                JArray ranges = JArray.Parse(jsonData["msg"]["ranges"].ToString());
                RosBridgeUtility.LaserScanMsg laserMsg = new RosBridgeUtility.LaserScanMsg();
                laserMsg.ranges = ranges;
                laserMsg.angle_increment = angle_inc;
                laserMsg.min_angle = min_angle;
                LastMsgs.lastLaserScanMsg = laserMsg;
            }
        }
    }
}
