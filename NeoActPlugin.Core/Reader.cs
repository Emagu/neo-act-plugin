﻿using NeoActPlugin.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NeoActPlugin.Core
{
    class Reader
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumProcessModules(
            IntPtr hProcess,
            [Out] IntPtr[] lphModule,
            uint cb,
            out uint lpcbNeeded);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            ProcessAccessFlags dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            PROCESS_VM_READ = 0x0010,
            PROCESS_QUERY_INFORMATION = 0x0400
        }

        private int? _pid;
        private IntPtr _baseAddress;
        private IntPtr _currentAddress;
        private readonly long[] _offsetsTWJP = { 0x755d030, 0xA0, 0x670, 0x8 };
        private string[] _lastLines = new string[600];

        private IntPtr _processHandle = IntPtr.Zero;
        private bool _needsHandleRefresh = true;

        private DateTime _lastReadTime = DateTime.MinValue;
        private TimeSpan _baseReadInterval = TimeSpan.FromMilliseconds(14);
        private TimeSpan _currentReadInterval = TimeSpan.FromMilliseconds(14);

        private bool _isInErrorState = false;
        private DateTime _lastErrorLogTime = DateTime.MinValue;
        private TimeSpan _errorLogInterval = TimeSpan.FromSeconds(10);

        private bool _initialized = false;

        private static int? _cachedPid;

        public Reader()
        {
        }

        private void LogErrorThrottled(string message)
        {
            if (DateTime.Now - _lastErrorLogTime > _errorLogInterval || !_isInErrorState)
            {
                PluginMain.WriteLog(LogLevel.Error, message);
                _lastErrorLogTime = DateTime.Now;
                _isInErrorState = true;
            }
        }

        private void LogRecovery()
        {
            if (_isInErrorState)
            {
                PluginMain.WriteLog(LogLevel.Info, "Process reconnected successfully.");
                _isInErrorState = false;
            }
        }

        public bool RefreshPointers()
        {
            try
            {
                var newPid = GetProcessId("BNSR.exe");
                if (!newPid.HasValue)
                {
                    LogErrorThrottled("Process not found: BNSR");
                    InvalidateState();
                    return false;
                }

                if (newPid != _pid || _baseAddress == IntPtr.Zero)
                {
                    InvalidateHandles();
                    _pid = newPid;
                    _baseAddress = GetBaseAddress(_pid.Value);

                    if (_baseAddress == IntPtr.Zero)
                    {
                        LogErrorThrottled("Failed to get base address");
                        return false;
                    }
                }
                _currentAddress = FollowPointerChain(_pid.Value, _baseAddress, _offsetsTWJP);

                if (_currentAddress == IntPtr.Zero)
                {
                    PluginMain.WriteLog(LogLevel.Info, "Failed to resolve pointer chain. Make sure you have selected your character and the correct region.");
                    return false;
                }

                LogRecovery();
                return true;
            }
            catch (Exception ex)
            {
                PluginMain.WriteLog(LogLevel.Error, "Error refreshing pointers: " + ex.Message);
                return false;
            }
        }

        public string[] Read()
        {
            if (DateTime.Now - _lastReadTime < _currentReadInterval)
                return Array.Empty<string>();

            if (!RefreshPointers())
            {
                _currentReadInterval = TimeSpan.FromMilliseconds(3000);
                _lastReadTime = DateTime.Now;
                _initialized = false;
                return Array.Empty<string>();
            }

            string[] currentLines = ReadAllLines();

            if (!_initialized)
            {
                currentLines.CopyTo(_lastLines, 0);
                _initialized = true;
                _lastReadTime = DateTime.Now;
                return Array.Empty<string>();
            }

            int validEntries = currentLines.Count(line => !string.IsNullOrEmpty(line));
            var newEntries = UpdateChangeTracking(currentLines);
            AdjustReadInterval(validEntries);
            _lastReadTime = DateTime.Now;

            return newEntries;
        }

        private string[] ReadAllLines()
    {
        string[] currentLines = new string[600];
        for (int i = 0; i < 600; i++)
        {
            IntPtr targetAddress = new IntPtr(_currentAddress.ToInt64() + (i * 0x70));
            byte[] pointerBuffer = ReadMemory(targetAddress, 8);

            if (pointerBuffer == null || IsAllZero(pointerBuffer))
            {
                currentLines[i] = string.Empty;
                continue;
            }

            IntPtr nextAddress = new IntPtr(BitConverter.ToInt64(pointerBuffer, 0));
            if (nextAddress == IntPtr.Zero)
            {
                currentLines[i] = string.Empty;
                continue;
            }

            byte[] stringBuffer = ReadMemory(nextAddress, 512);
            if (stringBuffer == null)
            {
                currentLines[i] = string.Empty;
                continue;
            }

            string decoded = DecodeString(stringBuffer);
            int periodIndex = decoded.IndexOf('.');
            currentLines[i] = periodIndex != -1 ? decoded.Substring(0, periodIndex + 1) : decoded;
        }
        return currentLines;
    }

        private string[] UpdateChangeTracking(string[] currentLines)
        {
            List<string> newEntries = new List<string>();
            for (int i = 0; i < 600; i++)
            {
                if (currentLines[i] != _lastLines[i] && !string.IsNullOrEmpty(currentLines[i]))
                    newEntries.Add(currentLines[i]);
            }
            _lastLines = (string[])currentLines.Clone();
            return newEntries.ToArray();
        }

        private void AdjustReadInterval(int validEntries)
        {
            _currentReadInterval = validEntries > 0
                ? _baseReadInterval
                : TimeSpan.FromMilliseconds(Math.Min(_currentReadInterval.TotalMilliseconds * 1.5, 1000));
        }

        private byte[] ReadMemory(IntPtr address, int size)
        {
            if (!_pid.HasValue) return null;

            try
            {
                if (_needsHandleRefresh || _processHandle == IntPtr.Zero)
                {
                    InvalidateHandles();
                    _processHandle = OpenProcess(ProcessAccessFlags.PROCESS_VM_READ, false, _pid.Value);
                    _needsHandleRefresh = false;

                    if (_processHandle == IntPtr.Zero)
                    {
                        InvalidateState();
                        return null;
                    }
                }

                byte[] buffer = new byte[size];
                int bytesRead;
                bool success = ReadProcessMemory(_processHandle, address, buffer, size, out bytesRead);

                if (!success || bytesRead != size)
                {
                    _needsHandleRefresh = true;
                    return null;
                }

                return buffer;
            }
            catch
            {
                _needsHandleRefresh = true;
                return null;
            }
        }

        private void InvalidateHandles()
        {
            if (_processHandle != IntPtr.Zero)
            {
                CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }
            _needsHandleRefresh = true;
        }

        private void InvalidateState()
        {
            _pid = null;
            _baseAddress = IntPtr.Zero;
            _currentAddress = IntPtr.Zero;
            InvalidateHandles();
        }

        private static string DecodeString(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length - 1; i += 2)
            {
                if (buffer[i] == 0 && buffer[i + 1] == 0)
                    return Encoding.Unicode.GetString(buffer, 0, i);
            }
            return Encoding.Unicode.GetString(buffer);
        }

        private static bool IsAllZero(byte[] buffer)
        {
            foreach (byte b in buffer)
                if (b != 0) return false;
            return true;
        }

        private static int? GetProcessId(string processName)
        {
            var cleanName = processName.Replace(".exe", "");

            if (_cachedPid.HasValue)
            {
                try
                {
                    var proc = Process.GetProcessById(_cachedPid.Value);
                    if (proc.ProcessName.Equals(cleanName, StringComparison.OrdinalIgnoreCase) &&
                        !IsProcessSuspended(proc))
                    {
                        return _cachedPid;
                    }
                }
                catch { /* Process might have exited */ }
            }

            var processes = Process.GetProcessesByName(cleanName);
            if (processes.Length == 0)
            {
                _cachedPid = null;
                return null;
            }

            foreach (var process in processes)
            {
                if (!IsProcessSuspended(process))
                {
                    _cachedPid = process.Id;
                    return _cachedPid;
                }
            }

            _cachedPid = null;
            return null;
        }

        private static bool IsProcessSuspended(Process process)
        {
            try
            {
                if (process.Threads.Count == 0)
                    return true;

                foreach (ProcessThread thread in process.Threads)
                {
                    if (thread.ThreadState != System.Diagnostics.ThreadState.Wait || thread.WaitReason != ThreadWaitReason.Suspended)
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return true;
            }
            return true;
        }

        private IntPtr GetBaseAddress(int pid)
        {
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.PROCESS_QUERY_INFORMATION | ProcessAccessFlags.PROCESS_VM_READ, false, pid);
            if (hProcess == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                if (!EnumProcessModules(hProcess, null, 0, out uint bytesNeeded))
                    return IntPtr.Zero;

                IntPtr[] modules = new IntPtr[bytesNeeded / IntPtr.Size];
                if (!EnumProcessModules(hProcess, modules, bytesNeeded, out _))
                    return IntPtr.Zero;

                return modules.Length > 0 ? modules[0] : IntPtr.Zero;
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        private IntPtr FollowPointerChain(int pid, IntPtr baseAddress, long[] offsets)
        {
            IntPtr currentAddress = baseAddress;
            foreach (long offset in offsets)
            {
                currentAddress = new IntPtr(currentAddress.ToInt64() + offset);
                byte[] buffer = ReadMemory(currentAddress, 8);
                if (buffer == null) return IntPtr.Zero;
                currentAddress = new IntPtr(BitConverter.ToInt64(buffer, 0));
            }
            return currentAddress;
        }
    }
}
