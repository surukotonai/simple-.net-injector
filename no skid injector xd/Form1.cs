using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace no_skid_injector_xd
{

    public partial class main : Form
    {
      
        private string selectedDllPath = string.Empty;

        public class AppConfig
        {
            public string ProcessName { get; set; }
            public string DllPath { get; set; }

        }
        public main()
        {

            InitializeComponent();

            LoadConfig();

            this.button1.Click += new System.EventHandler(this.buttonInject_Click);
        }


        private void main_Load(object sender, EventArgs e)
        {
         //   if (this.labelSelectedDll != null)
          //  {
           //     this.labelSelectedDll.Text = "DLL 未選択";
            //}

        }
        private void main_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig(); 
        }
        private void SaveConfig()
        {

            string configFilePath = Path.Combine(Application.StartupPath, "config.json");

            try
            {

                AppConfig config = new AppConfig
                {

                    ProcessName = this.textBoxProcessName?.Text ?? "",
                    DllPath = this.selectedDllPath ?? ""
                };


                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(config, options);


                File.WriteAllText(configFilePath, jsonString);
            }
            catch (Exception ex)
            {

                Console.Error.WriteLine($"nigash error: {ex.Message}");

            }
        }
        private void LoadConfig()
        {

            string configFilePath = Path.Combine(Application.StartupPath, "config.json");


            if (File.Exists(configFilePath))
            {
                try
                {

                    string jsonString = File.ReadAllText(configFilePath);

                    AppConfig config = JsonSerializer.Deserialize<AppConfig>(jsonString);


                    if (config != null)
                    {
                        if (this.textBoxProcessName != null)
                        {
                            this.textBoxProcessName.Text = config.ProcessName ?? ""; // onull check
                        }
                        this.selectedDllPath = config.DllPath ?? ""; // onull check x2
                        if (this.labelSelectedDll != null)
                        {
                            // 
                            this.labelSelectedDll.Text = !string.IsNullOrEmpty(this.selectedDllPath)
                                                        ? Path.GetFileName(this.selectedDllPath)
                                                        : "DLL 未選択";
                        }
                    }
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"config faild: {ex.Message}",
                                    "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
      
                    if (this.labelSelectedDll != null) { this.labelSelectedDll.Text = "DLL 未選択"; }
                    if (this.textBoxProcessName != null) { this.textBoxProcessName.Text = ""; }
                    this.selectedDllPath = "";
                }
            }
            else
            {

                if (this.labelSelectedDll != null) { this.labelSelectedDll.Text = "DLL 未選択"; }

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "DLL ファイル (*.dll)|*.dll";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "注入するDLLファイルを選択";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
               
                        selectedDllPath = openFileDialog.FileName;

                        
                        if (this.labelSelectedDll != null)
                        {
                            this.labelSelectedDll.Text = Path.GetFileName(selectedDllPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("file select error" + ex.Message, "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        selectedDllPath = string.Empty;
                        if (this.labelSelectedDll != null)
                        {
                            this.labelSelectedDll.Text = "error";
                        }
                    }
                }
            }
        }


        private void buttonInject_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(selectedDllPath))
            {
                MessageBox.Show("fist dll select pls pls pls。", "error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (this.textBoxProcessName == null)
            {
                MessageBox.Show("wtf デザイナで textBoxProcessName を追加してください", "内部error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string processName = this.textBoxProcessName.Text.Trim();
            if (string.IsNullOrEmpty(processName))
            {
                MessageBox.Show("プロセス名入れろよ..。", "error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // remove prossses sssssssssss exe
            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }

            Process[] processes = null;
            try
            {
                processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    MessageBox.Show($"プロセスないね障害すぎ '{this.textBoxProcessName.Text}'", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (processes.Length > 1)
                {
                    MessageBox.Show($"waraing multi target '{processes[0].ProcessName}'. first target use (PID: {processes[0].Id})", "警告", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }

                int targetProcessId = processes[0].Id;
                string targetProcessNameActual = processes[0].ProcessName;


                bool success = DllInjector.InjectDll(targetProcessId, selectedDllPath);

                if (success)
                {
                    MessageBox.Show($"success injection\nDLL: {Path.GetFileName(selectedDllPath)}\nProcess: {targetProcessNameActual} (PID: {targetProcessId})", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {

                    MessageBox.Show($"faild injection. Check console output or permissions / bitness.", "失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"nigash inject error : {ex.Message}", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // プロセスハンドルの解放
                if (processes != null)
                {
                    foreach (var process in processes)
                    {
                        process.Close(); // or process.Dispose();
                    }
                }
            }
        }
    }


    public class DllInjector
    {
        // Windows API Constants
        const uint PROCESS_CREATE_THREAD = 0x0002;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PROCESS_VM_OPERATION = 0x0008;
        const uint PROCESS_VM_WRITE = 0x0020;
        const uint PROCESS_VM_READ = 0x0010;

        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint MEM_RELEASE = 0x00008000;

        const uint PAGE_READWRITE = 0x04; 

        // Windows API Functions (P/Invoke)
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] 
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)] //only asic idk 
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize, // 64 + 32
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            IntPtr nSize, //64 + 32
            out IntPtr lpNumberOfBytesWritten); 

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress, // start adreeees
            IntPtr lpParameter,    
            uint dwCreationFlags,
            out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,     
            uint dwFreeType);  // MEM_RELEASE

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject); // close hand


        public static bool InjectDll(int processId, string dllPath)
        {
            IntPtr hProcess = IntPtr.Zero;  
            IntPtr pDllPath = IntPtr.Zero;  
            IntPtr hThread = IntPtr.Zero;    
            IntPtr bytesWritten;             // WriteProcessMemory wi

            try
            {
                // 0. DLLファイルの存在確認
                if (!File.Exists(dllPath))
                {
                    Console.Error.WriteLine($"Inject Error: DLL not found at '{dllPath}'");

                    MessageBox.Show($"DLLファイルが見つかりません。\nパス: {dllPath}", "インジェクションerror", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }


                hProcess = OpenProcess(
                    PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
                    false, 
                    processId);

                if (hProcess == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"Inject Error: Could not open process (ID: {processId}). Error code: {error}");
                    MessageBox.Show($"プロセスを開けませんでした (PID: {processId})。\nerrorコード: {error}\n管理者権限で実行しているか、プロセスが存在するか確認してください。", "インジェクションerror", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // LoadLibraryW 
                IntPtr kernel32Handle = GetModuleHandle("kernel32.dll");

                IntPtr loadLibraryAddr = GetProcAddress(kernel32Handle, "LoadLibraryW"); 
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"Inject Error: Could not find LoadLibraryW function. Error code: {error}");
                    MessageBox.Show($"Inject Error: Could not find LoadLibraryW function. Error code: {error}", "injection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false; // finally で hProcess は解放される
                }


                byte[] dllPathBytes = Encoding.Unicode.GetBytes(dllPath + "\0"); // Null Unicode
                IntPtr dllPathSize = new IntPtr(dllPathBytes.Length);

                pDllPath = VirtualAllocEx(hProcess, IntPtr.Zero, dllPathSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (pDllPath == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"Inject Error: Could not allocate memory in target process. Error code: {error}");
                    MessageBox.Show($"Inject Error: Could not allocate memory in target process. Error code: {error}", "injection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // DLL pass
                if (!WriteProcessMemory(hProcess, pDllPath, dllPathBytes, dllPathSize, out bytesWritten) || bytesWritten != dllPathSize)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"Inject Error: Could not write DLL path to target process memory. Error code: {error}");
                    MessageBox.Show($"Inject Error: Could not write DLL path to target process memory. Error code: {error}", "injection error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    VirtualFreeEx(hProcess, pDllPath, IntPtr.Zero, MEM_RELEASE); 
                    return false;
                }

                // make remote sw
                IntPtr threadId; 
                hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, pDllPath, 0, out threadId);
                if (hThread == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"Inject Error: Could not create remote thread. Error code: {error}");
                    MessageBox.Show($"Inject Error: Could not create remote thread. Error code: {error}", "injection error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    VirtualFreeEx(hProcess, pDllPath, IntPtr.Zero, MEM_RELEASE);
                    return false;
                }

                Console.WriteLine($"Successfully created remote thread (ID: {threadId}) to load DLL in process {processId}.");

                return true; 

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An unexpected error occurred during injection: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred during injection:\n{ex.Message}", "injection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {

                if (hThread != IntPtr.Zero)
                {
                    CloseHandle(hThread); // sws
                }
                if (hProcess != IntPtr.Zero)
                {
                    CloseHandle(hProcess); // prs
                }
            }
        }
    }
}