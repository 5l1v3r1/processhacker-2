﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ProcessHacker
{
    public partial class HandleFilterWindow : Form
    {
        public HandleFilterWindow()
        {
            InitializeComponent();

            ListViewMenu.AddMenuItems(copyMenuItem.MenuItems, listHandles, null);
            listHandles.ContextMenu = menuHandle;

            Misc.SetDoubleBuffered(listHandles, typeof(ListView), true);
        }

        private void HandleFilterWindow_Load(object sender, EventArgs e)
        {
            ColumnSettings.LoadSettings(Properties.Settings.Default.FilterHandleListViewColumns, listHandles);
            this.Size = Properties.Settings.Default.FilterHandleWindowSize;
        }

        private void HandleFilterWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.FilterHandleListViewColumns = ColumnSettings.SaveSettings(listHandles);
            Properties.Settings.Default.FilterHandleWindowSize = this.Size;

            e.Cancel = true;
            this.Visible = false;
        }

        private void menuHandle_Popup(object sender, EventArgs e)
        {
            if (listHandles.SelectedItems.Count == 0)
            {
                Misc.DisableAllMenuItems(menuHandle);
            }
            else if (listHandles.SelectedItems.Count == 1)
            {
                Misc.EnableAllMenuItems(menuHandle);
            }
            else
            {
                Misc.DisableAllMenuItems(menuHandle);

                copyMenuItem.Enabled = true;
            }
        }

        private void goToProcessMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void closeMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void buttonFind_Click(object sender, EventArgs e)
        {
            buttonFind.Enabled = false;
            this.UseWaitCursor = true;
            progress.Visible = true;
            Application.DoEvents();
            listHandles.BeginUpdate();
                                       
            Win32.SYSTEM_HANDLE_INFORMATION[] handles = null;

            try
            {
                handles = Win32.EnumHandles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Process Hacker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Dictionary<int, Win32.ProcessHandle> processHandles = new Dictionary<int, Win32.ProcessHandle>();

            progress.Minimum = 0;
            progress.Maximum = handles.Length;

            for (int i = 0; i < handles.Length; i++)
            {
                Win32.SYSTEM_HANDLE_INFORMATION handle = handles[i];

                progress.Value = i;

                try
                {
                    if (handle.ProcessId == 4)
                        continue;

                    if (!processHandles.ContainsKey(handle.ProcessId))
                        processHandles.Add(handle.ProcessId, 
                            new Win32.ProcessHandle(handle.ProcessId, Win32.PROCESS_RIGHTS.PROCESS_DUP_HANDLE));

                    int object_handle = 0;
                    int retLength = 0;

                    if (Win32.ZwDuplicateObject(processHandles[handle.ProcessId].Handle, handle.Handle,
                        Program.CurrentProcess.Handle, ref object_handle, 0, 0,
                        0x4 // DUPLICATE_SAME_ATTRIBUTES
                        ) != 0)
                        continue;

                    int threadId = 0;

                    Win32.CreateThread(0, 0, new System.Threading.ThreadStart(delegate
                    {
                        Win32.ZwQueryObject(object_handle, Win32.OBJECT_INFORMATION_CLASS.ObjectNameInformation, 0, 0, ref retLength);
                    }), 0, 0, ref threadId);

                    int threadHandle = Win32.OpenThread(Win32.THREAD_RIGHTS.THREAD_ALL_ACCESS, 0, threadId);

                    if (threadHandle != 0)
                    {
                        if (Win32.WaitForSingleObject(threadHandle, 100) != Win32.WAIT_OBJECT_0)
                        {
                            Win32.TerminateThread(threadHandle, 0);
                            Win32.CloseHandle(threadHandle);
                            Win32.CloseHandle(object_handle);
                            continue;
                        }
                    }

                    Win32.CloseHandle(threadHandle);
                    Win32.CloseHandle(object_handle);

                    Win32.ObjectInformation info = Win32.GetHandleInfo(processHandles[handle.ProcessId], handle);

                    if (!info.BestName.ToLower().Contains(textFilter.Text.ToLower()))
                        continue;

                    ListViewItem item = new ListViewItem();

                    item.Name = handle.Handle.ToString();
                    item.Text = Program.HackerWindow.ProcessList.Items[handle.ProcessId.ToString()].Text +
                        " (" + handle.ProcessId.ToString() + ")";
                    item.Tag = handle.ProcessId;
                    item.SubItems.Add(new ListViewItem.ListViewSubItem(item, info.TypeName));
                    item.SubItems.Add(new ListViewItem.ListViewSubItem(item, info.BestName));
                    item.SubItems.Add(new ListViewItem.ListViewSubItem(item, "0x" + handle.Handle.ToString("x")));

                    listHandles.Items.Add(item);
                }
                catch
                {
                    continue;
                }
            }

            foreach (Win32.ProcessHandle phandle in processHandles.Values)
                phandle.Dispose();

            listHandles.EndUpdate();
            progress.Visible = false;
            this.UseWaitCursor = false;
            buttonFind.Enabled = true;
        }
    }
}
