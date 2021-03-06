﻿using SRTPluginBase;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace SRTPluginProviderOri1
{
    public class SRTPluginProviderOoT : IPluginProvider
    {
        private Process processId;
        private GameMemoryOoTScanner gameMemoryScanner;
        private Stopwatch stopwatch;
        private IPluginHostDelegates hostDelegates;
        public IPluginInfo Info => new PluginInfo();

        public bool GameRunning
        {
            get
            {
                if (gameMemoryScanner != null && !gameMemoryScanner.ProcessRunning)
                {
                    processId = GetProcessId();
                    if (processId != null)
                        gameMemoryScanner.Initialize(processId); // Re-initialize and attempt to continue.
                }

                return gameMemoryScanner != null && gameMemoryScanner.ProcessRunning;
            }
        }

        public int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            processId = GetProcessId();
            gameMemoryScanner = new GameMemoryOoTScanner(processId);
            stopwatch = new Stopwatch();
            stopwatch.Start();
            return 0;
        }

        public int Shutdown()
        {
            gameMemoryScanner?.Dispose();
            gameMemoryScanner = null;
            stopwatch?.Stop();
            stopwatch = null;
            return 0;
        }

        public object PullData()
        {
            try
            {
                if (!GameRunning) // Not running? Bail out!
                    return null;

                if (stopwatch.ElapsedMilliseconds >= 2000L)
                {
                    gameMemoryScanner.UpdatePointers();
                    stopwatch.Restart();
                }
                return gameMemoryScanner.Refresh();
            }
            catch (Win32Exception ex)
            {
                if ((ProcessMemory32.Win32Error)ex.NativeErrorCode != ProcessMemory32.Win32Error.ERROR_PARTIAL_COPY)
                    hostDelegates.ExceptionMessage(ex);// Only show the error if its not ERROR_PARTIAL_COPY. ERROR_PARTIAL_COPY is typically an issue with reading as the program exits or reading right as the pointers are changing (i.e. switching back to main menu).

                return null;
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage(ex);
                return null;
            }
        }

        private Process GetProcessId() => Process.GetProcessesByName("Project64")?.FirstOrDefault();
    }
}
