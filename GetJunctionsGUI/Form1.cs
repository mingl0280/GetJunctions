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
using System.Threading;

namespace GetJunctionsGUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private delegate void TBoxWriterD(ref string i);

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            button1.Enabled = false;
            Thread th = new Thread(new ThreadStart(doScanThread));
            th.Start();
            timer1.Start();
        }

        private void doScanThread()
        {
            DriveInfo[] AllDrives = DriveInfo.GetDrives();
            foreach (DriveInfo Drive in AllDrives)
            {
                if (Drive.IsReady && (Drive.DriveType == DriveType.Fixed || Drive.DriveType == DriveType.Ram || Drive.DriveType == DriveType.Removable))
                {
                    if (File.Exists(Drive.Name + "Junctions.txt"))
                        File.Delete(Drive.Name + "Junctions.txt");

                    StreamWriter Writer = new StreamWriter(Drive.Name + "Junctions.txt");

                    //Writer.AutoFlush = true;
                    List<String> JncLists = new List<string>();
                    DirectoryInfo Dir = new DirectoryInfo(Drive.Name);
                    GetJunctions(Dir, JncLists);
                    foreach (string s in JncLists)
                    {
                        Writer.WriteLine(s);
                    }
                    Writer.Flush();
                    Writer.Close();
                    Writer.Dispose();
                }
            }
            strCurrentDir = "Finished.";
        }


        private void TBoxWriter(ref string i)
        {
            textBox1.AppendText(i);
        }

        private string strCurrentDir = "";

        private void GetJunctions(DirectoryInfo curDir, List<string> Writer)
        {
            strCurrentDir = curDir.FullName;
            try
            {
                if (curDir.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    string finalPath = NativeMethods.GetFinalPathName(curDir.FullName);
                    Writer.Add(curDir.FullName + "  [-->>]  " + finalPath);
                    BeginInvoke(new TBoxWriterD(TBoxWriter), curDir.FullName + "  [-->>]  " + finalPath + Environment.NewLine);
                }
                else
                {
                    var cDirs = curDir.GetDirectories();
                    foreach (DirectoryInfo D in cDirs)
                    {
                        GetJunctions(D, Writer);
                    }
                    var cFiles = curDir.GetFiles();
                    foreach (FileInfo FI in cFiles)
                    {
                        if (FI.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            string finalPath = NativeMethods.GetFinalPathName(FI.FullName);
                            Writer.Add(FI.FullName + "  [-->>]  " + finalPath);
                            BeginInvoke(new TBoxWriterD(TBoxWriter), FI.FullName + "  [-->>]  " + finalPath + Environment.NewLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(curDir.FullName + " Error: " + ex.Message);
                BeginInvoke(new TBoxWriterD(TBoxWriter), curDir.FullName + " Error: " + ex.Message + Environment.NewLine);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = strCurrentDir;
            if (strCurrentDir == "Finished.")
            {
                timer1.Stop();
                button1.Enabled = true;
            }
        }
    }

    public static class NativeMethods
    {
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private const uint FILE_READ_EA = 0x0008;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
                [MarshalAs(UnmanagedType.LPTStr)] string filename,
                [MarshalAs(UnmanagedType.U4)] uint access,
                [MarshalAs(UnmanagedType.U4)] FileShare share,
                IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
                IntPtr templateFile);

        public static string GetFinalPathName(string path)
        {
            var h = CreateFile(path,
                FILE_READ_EA,
                FileShare.ReadWrite | FileShare.Delete,
                IntPtr.Zero,
                FileMode.Open,
                FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);
            if (h == INVALID_HANDLE_VALUE)
                throw new Win32Exception();

            try
            {
                var sb = new StringBuilder(1024);
                var res = GetFinalPathNameByHandle(h, sb, 1024, 0);
                if (res == 0)
                    throw new Win32Exception();

                return sb.ToString().Replace("\\\\?\\", "");
            }
            finally
            {
                CloseHandle(h);
            }
        }
    }
}
