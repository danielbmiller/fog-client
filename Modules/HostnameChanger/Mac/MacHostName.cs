/*
 * FOG Service : A computer management client for the FOG Project
 * Copyright (C) 2014-2015 FOG Project
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 3
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using FOG.Handlers;
using FOG.Handlers.Middleware;

namespace FOG.Modules.HostnameChanger.Mac
{
    internal class MacHostName : IHostName
    {
        private readonly string Name = "HostnameChanger";

        public void RenameComputer(string hostname)
        {
            ProcessHandler.Run("scutil", "--set HostName " + hostname);
            ProcessHandler.Run("scutil", "--set LocalHostName " + hostname);
            ProcessHandler.Run("scutil", "--set ComputerName " + hostname);
        }

        public bool RegisterComputer(Response response)
        {
            string domain = response.GetField("#ADDom");
            string ou = response.GetField("#ADOU");
            string adadmin = response.GetField("#ADUser");
            string adpass = response.GetField("#ADPass");
            ProcessHandler.Run("/bin/bash", string.Format("{0} {1} {2} {3} {4}",	Path.Combine(Settings.Location, "/Scripts/Mac/osxADBind.sh"),domain, ou, adadmin,adpass));
        }

        public void UnRegisterComputer(Response response)
        {
            throw new NotImplementedException();
        }

        public void ActivateComputer(string key)
        {
            throw new NotImplementedException();
        }
    }
}