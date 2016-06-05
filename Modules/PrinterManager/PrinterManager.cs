﻿/*
 * FOG Service : A computer management client for the FOG Project
 * Copyright (C) 2014-2016 FOG Project
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
using System.Collections.Generic;
using System.Linq;
using Zazzles;
using Zazzles.Middleware;
using Zazzles.Modules;

// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace FOG.Modules.PrinterManager
{
    /// <summary>
    ///     Manage printers
    /// </summary>
    public class PrinterManager : AbstractModule<PrinterMessage>
    {
        private static string LogName;
        private readonly PrintManagerBridge _instance;
        private readonly List<string> _configuredPrinters;

        public PrinterManager()
        {
            Compatiblity = Settings.OSType.Windows;
            Name = "PrinterManager";
            LogName = Name;
            _configuredPrinters = new List<string>();

            switch (Settings.OS)
            {
                case Settings.OSType.Windows:
                    _instance = new WindowsPrinterManager();
                    break;
                default:
                    _instance = new UnixPrinterManager();
                    break;
            }
        }

        protected override void DoWork(Response data, PrinterMessage msg)
        {
            //Get printers
            if (msg.Mode.Equals("0")) return;

            if (data.Error && data.ReturnCode.Equals("np"))
            {
                RemoveExtraPrinters(new List<Printer>(), msg);
                return;
            }
            if (data.Error) return;
            if (!data.Encrypted)
            {
                Log.Error(Name, "Response was not encrypted");
                return;
            }

            RemoveExtraPrinters(msg.Printers, msg);

            Log.Entry(Name, "Adding printers");
            foreach (var printer in msg.Printers)
            {
                if (!PrinterExists(printer.Name))
                {
                    printer.Add(_instance);
                    CleanPrinter(printer.Name);
                }
                else
                {
                    Log.Entry(Name, printer.Name + " already exists");
                    CleanPrinter(printer.Name);
                }
                BatchConfigure(msg.Printers);
            }
        }

        private void RemoveExtraPrinters(List<Printer> newPrinters, PrinterMessage msg)
        {
            var managedPrinters = newPrinters.Where(printer => printer != null).Select(printer => printer.Name).ToList();

            if (!msg.Mode.Equals("ar"))
            {
                foreach (var name in msg.AllPrinters.Where(name => !managedPrinters.Contains(name) && PrinterExists(name)))
                    CleanPrinter(name, true);
            }
            else
            {
                var printerNames = _instance.GetPrinters();
                foreach (var name in printerNames.Where(name => !managedPrinters.Contains(name)))
                    _instance.Remove(name);
            }
        }

        private bool PrinterExists(string name)
        {
            try
            {
                var printerList = _instance.GetPrinters();
                return printerList.Contains(name);
            }
            catch (Exception ex)
            {
                Log.Error(Name, "Could not detect if printer exists");
                Log.Error(Name, ex);
            }

            return false;
        }

        private void CleanPrinter(string name, bool cleanOriginal = false)
        {
            var printerList = _instance.GetPrinters();

            const string copyWord = "(Copy";
            var matches = printerList.Where(printer => printer.Contains(copyWord)).ToList();

            if(cleanOriginal && printerList.Contains(name))
                matches.Add(name);

            foreach (var printer in matches.Select(match => new Printer { Name = match }))
            {
                printer.Remove(_instance);
            }
        }

        private void BatchConfigure(List<Printer> printers)
        {
            var stringPrinters = new List<string>();

            foreach (var printer in printers.Where(printer => !_configuredPrinters.Contains(printer.ToString())))
            {
                stringPrinters.Add(printer.ToString());
                _configuredPrinters.Add(printer.ToString());

                try
                {
                    _instance.Configure(printer);
                }
                catch (Exception ex)
                {
                    Log.Error(LogName, "Unable to configure " + printer.Name);
                    Log.Error(LogName, ex);
                }
            }

            // Perform an except removal since _configuredPrinters is read only
            var extras = _configuredPrinters.Except(stringPrinters);

            foreach (var extraPrinter in extras)
            {
                _configuredPrinters.Remove(extraPrinter);
            }
        }
    }
}