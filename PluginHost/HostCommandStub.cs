using Jacobi.Vst.Core;
using Jacobi.Vst.Core.Host;
using System;

namespace PluginHost
{
    /// <summary>
    /// The HostCommandStub class represents the part of the host that a plugin can call.
    /// </summary>
    class HostCommandStub : IVstHostCommandStub
    {
        public VstTimeInfo VstTimeInfo { get; private set; }
        public virtual int BPM { get; set; }

        public HostCommandStub()
        {
            VstTimeInfo = new VstTimeInfo();
            Commands = new HostCommands(this);
            BPM = 120;
        }

        /// <summary>
        /// Raised when one of the methods is called.
        /// </summary>
        public event EventHandler<PluginCalledEventArgs> PluginCalled;

        private void RaisePluginCalled(string message)
        {
            PluginCalled?.Invoke(this, new PluginCalledEventArgs(message));
        }

        #region IVstHostCommandsStub Members

        /// <inheritdoc />
        public IVstPluginContext PluginContext { get; set; }

        public IVstHostCommands20 Commands { get; private set; }

        #endregion

        /// <inheritdoc />
        public Jacobi.Vst.Core.VstTimeInfo GetTimeInfo(Jacobi.Vst.Core.VstTimeInfoFlags filterFlags)
        {
            VstTimeInfo.SampleRate = 44100.0;
            VstTimeInfo.Tempo = (double)BPM;
            VstTimeInfo.PpqPosition = (VstTimeInfo.SamplePosition / VstTimeInfo.SampleRate) * (VstTimeInfo.Tempo / 60.0);
            VstTimeInfo.NanoSeconds = 0.0;
            VstTimeInfo.BarStartPosition = 0.0;
            VstTimeInfo.CycleStartPosition = 0.0;
            VstTimeInfo.CycleEndPosition = 0.0;
            VstTimeInfo.TimeSignatureNumerator = 4;
            VstTimeInfo.TimeSignatureDenominator = 4;
            VstTimeInfo.SmpteOffset = 0;
            VstTimeInfo.SmpteFrameRate = new Jacobi.Vst.Core.VstSmpteFrameRate();
            VstTimeInfo.SamplesToNearestClock = 0;
            VstTimeInfo.Flags = VstTimeInfoFlags.TempoValid |
                                VstTimeInfoFlags.PpqPositionValid |
                                VstTimeInfoFlags.TransportPlaying;
            return VstTimeInfo;
        }


        private class HostCommands : IVstHostCommands20
        {
            private readonly HostCommandStub _cmdStub;

            public HostCommands(HostCommandStub cmdStub)
            {
                _cmdStub = cmdStub;
            }

            #region IVstHostCommands20 Members

            /// <inheritdoc />
            public bool BeginEdit(int index)
            {
                //_cmdStub.RaisePluginCalled("BeginEdit(" + index + ")");

                return false;
            }

            /// <inheritdoc />
            public Jacobi.Vst.Core.VstCanDoResult CanDo(string cando)
            {
                //_cmdStub.RaisePluginCalled("CanDo(" + cando + ")");
                return Jacobi.Vst.Core.VstCanDoResult.Unknown;
            }

            /// <inheritdoc />
            public bool CloseFileSelector(Jacobi.Vst.Core.VstFileSelect fileSelect)
            {
                //_cmdStub.RaisePluginCalled("CloseFileSelector(" + fileSelect.Command + ")");
                return false;
            }

            /// <inheritdoc />
            public bool EndEdit(int index)
            {
                //_cmdStub.RaisePluginCalled("EndEdit(" + index + ")");
                return false;
            }

            /// <inheritdoc />
            public Jacobi.Vst.Core.VstAutomationStates GetAutomationState()
            {
                //_cmdStub.RaisePluginCalled("GetAutomationState()");
                return Jacobi.Vst.Core.VstAutomationStates.Off;
            }

            /// <inheritdoc />
            public int GetBlockSize()
            {
                //_cmdStub.RaisePluginCalled("GetBlockSize()");
                return 1024;
            }

            /// <inheritdoc />
            public string GetDirectory()
            {
                //_cmdStub.RaisePluginCalled("GetDirectory()");
                return null;
            }

            /// <inheritdoc />
            public int GetInputLatency()
            {
                //_cmdStub.RaisePluginCalled("GetInputLatency()");
                return 0;
            }

            /// <inheritdoc />
            public Jacobi.Vst.Core.VstHostLanguage GetLanguage()
            {
                _cmdStub.RaisePluginCalled("GetLanguage()");
                return Jacobi.Vst.Core.VstHostLanguage.NotSupported;
            }

            /// <inheritdoc />
            public int GetOutputLatency()
            {
                _cmdStub.RaisePluginCalled("GetOutputLatency()");
                return 0;
            }

            /// <inheritdoc />
            public Jacobi.Vst.Core.VstProcessLevels GetProcessLevel()
            {
                //_cmdStub.RaisePluginCalled("GetProcessLevel()"); <--- this spamming out really slowed audio and caused crackles in debug
                return Jacobi.Vst.Core.VstProcessLevels.Unknown;
            }

            /// <inheritdoc />
            public string GetProductString()
            {
                _cmdStub.RaisePluginCalled("GetProductString()");
                return "VST.NET";
            }

            /// <inheritdoc />
            public float GetSampleRate()
            {
                _cmdStub.RaisePluginCalled("GetSampleRate()");
                return 44.8f;
            }

            /// <inheritdoc />
            public Jacobi.Vst.Core.VstTimeInfo GetTimeInfo(Jacobi.Vst.Core.VstTimeInfoFlags filterFlags)
            {
                //_cmdStub.RaisePluginCalled("GetTimeInfo(" + filterFlags + ")"); <--- this spamming out really slowed audio and caused crackles in debug
                return null;
            }

            /// <inheritdoc />
            public string GetVendorString()
            {
                //_cmdStub.RaisePluginCalled("GetVendorString()");
                return "Jacobi Software";
            }

            /// <inheritdoc />
            public int GetVendorVersion()
            {
                //_cmdStub.RaisePluginCalled("GetVendorVersion()");
                return 1000;
            }

            /// <inheritdoc />
            public bool IoChanged()
            {
                //_cmdStub.RaisePluginCalled("IoChanged()");
                return false;
            }

            /// <inheritdoc />
            public bool OpenFileSelector(Jacobi.Vst.Core.VstFileSelect fileSelect)
            {
                //_cmdStub.RaisePluginCalled("OpenFileSelector(" + fileSelect.Command + ")");
                return false;
            }

            /// <inheritdoc />
            public bool ProcessEvents(Jacobi.Vst.Core.VstEvent[] events)
            {
                //_cmdStub.RaisePluginCalled("ProcessEvents(" + events.Length + ")");
                return false;
            }

            /// <inheritdoc />
            public bool SizeWindow(int width, int height)
            {
                //_cmdStub.RaisePluginCalled("SizeWindow(" + width + ", " + height + ")");
                return false;
            }

            /// <inheritdoc />
            public bool UpdateDisplay()
            {
                //_cmdStub.RaisePluginCalled("UpdateDisplay()");
                return false;
            }

            #endregion

            #region IVstHostCommands10 Members

            /// <inheritdoc />
            public int GetCurrentPluginID()
            {
                //_cmdStub.RaisePluginCalled("GetCurrentPluginID()");
                return _cmdStub.PluginContext.PluginInfo.PluginID;
            }

            /// <inheritdoc />
            public int GetVersion()
            {
                //_cmdStub.RaisePluginCalled("GetVersion()");
                return 1000;
            }

            /// <inheritdoc />
            public void ProcessIdle()
            {
                //_cmdStub.RaisePluginCalled("ProcessIdle()");
            }

            /// <inheritdoc />
            public void SetParameterAutomated(int index, float value)
            {
                //_cmdStub.RaisePluginCalled("SetParameterAutomated(" + index + ", " + value + ")");
            }

            #endregion
        }
    }

    /// <summary>
    /// Event arguments used when one of the mehtods is called.
    /// </summary>
    class PluginCalledEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new instance with a <paramref name="message"/>.
        /// </summary>
        /// <param name="message"></param>
        public PluginCalledEventArgs(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; private set; }
    }
}
