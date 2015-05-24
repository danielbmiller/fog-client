﻿using System;
using System.Diagnostics;
using System.Threading;
using FOG.Handlers;
using FOG.Handlers.Power;
using FOG.Modules;
using FOG.Modules.ClientUpdater;
using FOG.Modules.DisplayManager;
using FOG.Modules.GreenFOG;
using FOG.Modules.HostnameChanger;
using FOG.Modules.SnapinClient;
using FOG.Modules.TaskReboot;
using FOG.Modules.UserTracker;

namespace FOG
{
    class FOGSystemService : AbstractService
    {
        private readonly PipeServer _notificationPipe;
        private readonly Thread _notificationPipeThread;
        private readonly PipeServer _servicePipe;

        public FOGSystemService() : base()
        {
            //Setup the notification pipe server
            _notificationPipeThread = new Thread(notificationPipeHandler);
            _notificationPipe = new PipeServer("fog_pipe_notification");

            //Setup the user-service pipe server, this is only Server -- > Client communication so no need to setup listeners
            _servicePipe = new PipeServer("fog_pipe_service");
        }

        public override void Start()
        {
            // Start the pipe server
            _notificationPipeThread.Priority = ThreadPriority.Normal;
            _notificationPipeThread.Start();

            _servicePipe.Start();

            LogHandler.NewLine();
            LogHandler.PaddedHeader("Authentication");
            LogHandler.Log("Client-Info", string.Format("Version: {0}", RegistryHandler.GetSystemSetting("Version")));
            CommunicationHandler.Authenticate();
            LogHandler.NewLine();

            base.Start();
        }

        public override void Stop()
        {
            base.Stop();

            // Stop the pipe server
            _notificationPipeThread.Abort();

            // Kill the sub-processes
            foreach (var process in Process.GetProcessesByName("FOGUserService"))
                process.Kill();

            foreach (var process in Process.GetProcessesByName("FOGTray"))
                process.Kill();
        }

        //This is run by the pipe thread, it will send out notifications to the tray
        private void notificationPipeHandler()
        {
            while (!Power.ShutdownPending && !Power.UpdatePending)
            {
                if (!_notificationPipe.IsRunning())
                    _notificationPipe.Start();

                Thread.Sleep(3000);

                if (NotificationHandler.Notifications.Count <= 0) continue;

                //Split up the notification into 3 messages: Title, Message, and Duration
                _notificationPipe.SendMessage(string.Format("TLE:{0}", NotificationHandler.Notifications[0].Title));
                Thread.Sleep(750);

                _notificationPipe.SendMessage(string.Format("MSG:{0}", NotificationHandler.Notifications[0].Message));
                Thread.Sleep(750);

                _notificationPipe.SendMessage(string.Format("DUR:{0}", NotificationHandler.Notifications[0].Duration));
                NotificationHandler.Notifications.RemoveAt(0);
            }
        }

        protected override AbstractModule[] GetModules()
        {
            return new AbstractModule[]
            {
                new ClientUpdater(), 
                new TaskReboot(), 
                new HostnameChanger(), 
                new SnapinClient(), 
                new DisplayManager(), 
                new GreenFOG(), 
                new UserTracker() 
            };
        }

        protected override void ModuleLooper()
        {
            base.ModuleLooper();

            if (Power.UpdatePending)
                UpdateHandler.BeginUpdate(_servicePipe);
        }

        protected override int? GetSleepTime()
        {
            var response = CommunicationHandler.GetResponse("/management/index.php?node=client&sub=configure");

            if (response.Error || response.IsFieldValid("#sleep")) return null;

            try
            {
                var sleepTime = int.Parse(response.GetField("#sleep"));
                if (sleepTime >= DefaultSleepTime)
                    return sleepTime;

                LogHandler.Log(Name, string.Format("Sleep time set on the server is below the minimum of {0}", DefaultSleepTime));
            }
            catch (Exception ex)
            {
                LogHandler.Error(Name, "Unable to parse sleep time");
                LogHandler.Error(Name, ex);
            }
            return null;
        }
    }
}