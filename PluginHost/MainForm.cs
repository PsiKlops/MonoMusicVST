﻿using Jacobi.Vst.Host.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace PluginHost
{
    public partial class MainForm : Form
    {
        private List<VstPluginContext> _plugins = new List<VstPluginContext>();

        bool mActive;

        public VstPluginContext GetMainPlugin()
        {
            if(_plugins.Count>0)
            {
                return _plugins[0];
            }

            return null;
        }
        MonoMusicMaker.MonoAsio mMonoAsio;
        public MainForm()
        {
            InitializeComponent();
            Text = "VST.NET 2 Dummy Host Sample";

        }

        private void FillPluginList()
        {
            PluginListVw.Items.Clear();

            foreach (VstPluginContext ctx in _plugins)
            {
                ListViewItem lvItem = new ListViewItem(ctx.PluginCommandStub.Commands.GetEffectName());
                lvItem.SubItems.Add(ctx.PluginCommandStub.Commands.GetProductString());
                lvItem.SubItems.Add(ctx.PluginCommandStub.Commands.GetVendorString());
                lvItem.SubItems.Add(ctx.PluginCommandStub.Commands.GetVendorVersion().ToString());
                lvItem.SubItems.Add(ctx.Find<string>("PluginPath"));
                lvItem.Tag = ctx;

                PluginListVw.Items.Add(lvItem);
            }
        }

        private VstPluginContext OpenPlugin(string pluginPath)
        {
            try
            {
                HostCommandStub hostCmdStub = new HostCommandStub();
                hostCmdStub.PluginCalled += new EventHandler<PluginCalledEventArgs>(HostCmdStub_PluginCalled);

                VstPluginContext ctx = VstPluginContext.Create(pluginPath, hostCmdStub);

                // add custom data to the context
                ctx.Set("PluginPath", pluginPath);
                ctx.Set("HostCmdStub", hostCmdStub);

                // actually open the plugin itself
                ctx.PluginCommandStub.Commands.Open();

                return ctx;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.ToString(), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return null;
        }

        public void ReleaseAllPlugins()
        {
            foreach (VstPluginContext ctx in _plugins)
            {
                // dispose of all (unmanaged) resources
                ctx.Dispose();
            }

            _plugins.Clear();
        }

        private VstPluginContext SelectedPluginContext
        {
            get
            {
                if (PluginListVw.SelectedItems.Count > 0)
                {
                    return (VstPluginContext)PluginListVw.SelectedItems[0].Tag;
                }

                return null;
            }
        }

        private void HostCmdStub_PluginCalled(object sender, PluginCalledEventArgs e)
        {
            HostCommandStub hostCmdStub = (HostCommandStub)sender;

            // can be null when called from inside the plugin main entry point.
            if (hostCmdStub.PluginContext.PluginInfo != null)
            {
                Debug.WriteLine("Plugin " + hostCmdStub.PluginContext.PluginInfo.PluginID + " called:" + e.Message);
            }
            else
            {
                Debug.WriteLine("The loading Plugin called:" + e.Message);
            }
        }

        private void BrowseBtn_Click(object sender, EventArgs e)
        {
            OpenFileDlg.FileName = PluginPathTxt.Text;

            if (OpenFileDlg.ShowDialog(this) == DialogResult.OK)
            {
                PluginPathTxt.Text = OpenFileDlg.FileName;
            }
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            VstPluginContext ctx = OpenPlugin(PluginPathTxt.Text);

            if (ctx != null)
            {
                _plugins.Add(ctx);

                FillPluginList();

                if (mMonoAsio != null && _plugins.Count>0)
                {
                    mMonoAsio.SetPluginCOntext(_plugins[0]);
                }
                
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ReleaseAllPlugins();

            if (mMonoAsio != null)
            {
                mMonoAsio.Stop();
            }
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            //CHANGE TO NAUDIO TO GET OUT OF THIS CRASHY BLUE WAVE SHIT!
            mMonoAsio = new MonoMusicMaker.MonoAsio();
            mMonoAsio.Init(this);
            mMonoAsio.Start();
        }

        private void ViewPluginBtn_Click(object sender, EventArgs e)
        {
            PluginForm dlg = new PluginForm
            {
                PluginContext = SelectedPluginContext
            };

            dlg.ShowDialog(this);
        }

        private void DeleteBtn_Click(object sender, EventArgs e)
        {
            VstPluginContext ctx = SelectedPluginContext;

            if (ctx != null)
            {
                ctx.Dispose();

                _plugins.Remove(ctx);

                FillPluginList();
            }
        }
    }
}
