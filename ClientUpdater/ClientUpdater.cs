﻿
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;


namespace FOG
{
	/// <summary>
	/// Update the FOG Service
	/// </summary>
	public class ClientUpdater : AbstractModule {
		
		public ClientUpdater() : base() {
			setName("ClientUpdater");
			setDescription("Update the FOG Service");
			
		}
		
		protected override void doWork() {
			String serverVersion = CommunicationHandler.GetRawResponse("/service/getversion.php?clientver");
			String localVersion = RegistryHandler.GetSystemSetting("Version");
			
			if(serverVersion.CompareTo(localVersion) > 1) {
				CommunicationHandler.DownloadFile("/client/FOGService.msi", AppDomain.CurrentDomain.BaseDirectory + @"\tmp\FOGService.msi");
				prepareUpdateHelpers();
				ShutdownHandler.ScheduleUpdate();			
			}
		}
		
		//Prepare the downloaded update
		private void prepareUpdateHelpers() {
			if(File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\FOGUpdateHelper.exe") && File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\FOGUpdateWaiter.exe")) {
							
				try {
					File.Move(AppDomain.CurrentDomain.BaseDirectory + @"\FOGUpdateHelper.exe", AppDomain.CurrentDomain.BaseDirectory + @"tmp\FOGUpdateHelper.exe");
					File.Move(AppDomain.CurrentDomain.BaseDirectory + @"\FOGUpdateWaiter.exe", AppDomain.CurrentDomain.BaseDirectory + @"tmp\FOGUpdateWaiter.exe");
				} catch (Exception ex) {
					LogHandler.Log(getName(), "Unable to prepare update helpers");
					LogHandler.Log(getName(), "ERROR: " + ex.Message);
				}
			} else {
				LogHandler.Log(getName(), "Unable to locate helper files");
			}
		}
	}
}