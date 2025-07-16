using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint.Mem
{
    // https://www.reddit.com/r/csharp/comments/1jx9fgo/how_do_i_write_to_a_memory_address_of_another/
    // https://www.codeproject.com/Articles/670373/Csharp-Read-Write-Another-Process-Memory
    // https://github.com/Di0n/AmongUsMod/blob/main/AmongUsMod/mem.hpp
    public class ProcessMemory : IDisposable
    {
        // Memory api process
        private class MAProcess
        {
            public IntPtr Handle { get; set; }
            public int ProcessId { get; set; }

            public MAProcess(IntPtr handle, int processId)
            {
                Handle = handle;
                ProcessId = processId;
            }
         }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static unsafe extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, void* lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern unsafe bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        void* lpBuffer,
        int nSize,
        out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static unsafe extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            int dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);


        private const int PROCESS_WM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        private MAProcess process;
        private bool disposedValue;

        private ProcessMemory(MAProcess proc)
        {
            process = proc;
        }
        ~ProcessMemory()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Opens an existing local process object.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        /// <exception cref="MemoryException"></exception>
        public static ProcessMemory Attach(int processId)
        {
            IntPtr procHandle = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            int errorCode = Marshal.GetLastWin32Error();
            if (procHandle == IntPtr.Zero)
                throw new MemoryException("Failed to open process.", errorCode);

            return new ProcessMemory(new MAProcess(procHandle, processId));
        }

        /// <summary>
        /// Reads a built-in value from a memory address
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="MemoryException"></exception>
        public unsafe T Read<T>(IntPtr address) where T : unmanaged
        {
            int size = sizeof(T);
            byte* buffer = stackalloc byte[size];

            if (!ReadProcessMemory(process.Handle, address, buffer, size, out int bytesRead) || bytesRead != size)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new MemoryException($"Failed to read memory", errorCode);
            }

            return *(T*)buffer;
        }

        /// <summary>
        /// Write a built-in value to a set memory address
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public unsafe bool Write<T>(IntPtr address, T val) where T : unmanaged
        {
            int size = typeof(T) == typeof(bool) ? 1 : sizeof(T);
            byte* buffer = stackalloc byte[size]; // (byte*)Marshal.AllocHGlobal(size); / Marshal.FreeHGlobal((IntPtr)buffer);

            *(T*)buffer = val;

            return WriteRaw(address, buffer, size);
        }

        /// <summary>
        /// Sets a range of memory to NOP instructions (0x90).
        /// </summary>
        /// <param name="address"></param>
        /// <param name="size"></param>
        /// <returns>Wether the write was succesfull or not</returns>
        public unsafe bool WriteNOP(IntPtr address, int size)
        {
            byte* nopArray = stackalloc byte[size];

            for (int i = 0; i < size; i++)
                nopArray[i] = 0x90; // NOP instruction

            return WriteRaw(address, nopArray, size);
        }

        private unsafe bool WriteRaw(IntPtr address, byte* src, int size)
        {
            // Set memory region to writable
            VirtualProtectEx(process.Handle, address, size, PAGE_EXECUTE_READWRITE, out uint oldProtect);
            // Write to memory
            bool result = WriteProcessMemory(process.Handle, address, src, size, out int bytesWritten);
            // Revert old protection
            VirtualProtectEx(process.Handle, address, size, oldProtect, out _);

            return result && bytesWritten == size;
        }

        public IntPtr GetModuleBaseAddress(string moduleName)
        {
            Process? proc = Process.GetProcessById(process.ProcessId);
            if (proc == null) return IntPtr.Zero;

            foreach (ProcessModule module in proc.Modules)
            {
                if (string.Equals(module.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Follows pointer chain
        /// </summary>
        /// <param name="baseAddr"></param>
        /// <param name="offsets"></param>
        /// <returns></returns>
        public IntPtr FindDynamicAddress(IntPtr baseAddr, params int[] offsets)
        {
            IntPtr address = baseAddr;

            for (int i = 0; i < offsets.Length; i++)
            {
                address = Read<IntPtr>(address); // Lees pointer op huidig adres
                //if (address == IntPtr.Zero)
                //    throw new MemoryException($"Null pointer encountered in chain at offset index {i}");
                address = IntPtr.Add(address, offsets[i]); // Voeg offset toe
            }

            return address;
        }

        public IntPtr FindDynamicAddress(PointerPath ptrPath) => FindDynamicAddress(ptrPath.BaseAddress, ptrPath.Offsets);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Managed code
                }

                if (process.Handle != IntPtr.Zero)
                {
                    CloseHandle(process.Handle);
                    process.Handle = IntPtr.Zero;
                    process.ProcessId = 0;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class MemoryException : Exception
    {
        public int ErrorCode { get; }
        public MemoryException() : base() { }
        public MemoryException(string message) : base(message) { }
        public MemoryException(string message, Exception innerException) : base(message, innerException) { }
        public MemoryException(string message, int errorCode) : this(message)
        {
            ErrorCode = errorCode;
        }
    }

}
