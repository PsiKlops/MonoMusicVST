using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using Jacobi.Vst.Host.Interop;
using Jacobi.Vst.Core;
using NAudio.Wave;

#if NAUDIO_ASIO
#else
using BlueWave.Interop.Asio;
#endif

namespace MonoMusicMaker
{
    public class MonoAsio
    {

        public PluginHost.MainForm mMainForm;

        public class Settings
        {
            public int AsioBufferSize;
            public int mSelectedAsio;

        }
        private ManualResetEventSlim mAsioStopEvent = new ManualResetEventSlim(false);
        private ManualResetEventSlim mAsioStopCompleteEvent = new ManualResetEventSlim(false);
        private ManualResetEventSlim mAsioStartEvent = new ManualResetEventSlim(false);

        private int mAsioBuffSize = 496; //1024;
        const int BYTES_PER_SAMPLE = 4;
#if NAUDIO_ASIO
        NAudio.Wave.AsioOut mAsio;
        NAudio.Wave.BufferedWaveProvider mWaveProvider;
        byte[] mAsioLeftInt32LSBBuff;
        byte[] mAsioRightInt32LSBBuff;
#else
        private AsioDriver mAsio;
        Channel mAsioOutputLeft;    // Audio output to ASIO driver (Currently only 1x stereo)
        Channel mAsioOutputRight;
        PluginHost.AudioBufferInfo mAsioInputBuffers;
        Channel mAsioInputLeft;
        Channel mAsioInputRight;

#endif
        Settings mSettings = new Settings();
        private Thread mAsioThread;
  
        bool asioFail = true;
        MyWave myWave;


        public class MyWave : IWaveProvider
        {
            int BytesPerWaveSample = 4;
            public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            public VstPluginContext VstContext { get;  set; }
  
            int weirdShit = 0;

            public int Read(byte[] outputBuffer, int offset, int count)
            {
                //weirdShit++;

                //if(weirdShit%100 != 0)
                //{
                //    return 0;
                //}
                //weirdShit = 0;

                if (VstContext != null && VstContext.PluginCommandStub!=null)
                {
                    VstAudioBufferManager outputBuffers = new VstAudioBufferManager(
                       VstContext.PluginInfo.AudioOutputCount,
                       count / (VstContext.PluginInfo.AudioOutputCount * BytesPerWaveSample)
                    );

                    VstAudioBufferManager inputBuffers = new VstAudioBufferManager(
                         VstContext.PluginInfo.AudioInputCount,
                         count / (Math.Max(1, VstContext.PluginInfo.AudioInputCount) * BytesPerWaveSample)
                     );

                    //VstContext.PluginCommandStub.Commands.StartProcess();
                    //if (vstEvents.Length > 0)
                    //    VstContext.PluginCommandStub.Commands.ProcessEvents(vstEvents);
                    //VstContext.PluginCommandStub.Commands.ProcessReplacing(inputBuffers.ToArray(), outputBuffers.ToArray());
                    //VstContext.PluginCommandStub.Commands.ProcessReplacing(inputBuffers.Buffers.ToArray(), outputBuffers.Buffers.ToArray());
                    //VstContext.PluginCommandStub.Commands.StopProcess();
                    //VstContext.PluginCommandStub.Commands.MainsChanged(true);
                    VstContext.PluginCommandStub.Commands.StartProcess();
                    VstContext.PluginCommandStub.Commands.ProcessReplacing(inputBuffers.Buffers.ToArray(), outputBuffers.Buffers.ToArray());
                    VstContext.PluginCommandStub.Commands.StopProcess();
                    //VstContext.PluginCommandStub.Commands.MainsChanged(false);


                    //Convert from multi-track to interleaved data
                    int bufferIndex = offset;
                    for (int i = 0; i < outputBuffers.BufferSize; i++)
                    {
                        foreach (VstAudioBuffer vstBuffer in outputBuffers.Buffers)
                        {
                            //Int16 waveValue = (Int16)((vstBuffer[i] + 1) * 128);
                            byte[] bytes = BitConverter.GetBytes(vstBuffer[i]);
                            outputBuffer[bufferIndex] = bytes[0];
                            outputBuffer[bufferIndex + 1] = bytes[1];
                            outputBuffer[bufferIndex + 2] = bytes[2];
                            outputBuffer[bufferIndex + 3] = bytes[3];
                            bufferIndex += 4;
                        }
                    }
                    return count;

                }

                return 0;
            }
        }


        public MonoAsio()
        {

        }
#if NAUDIO_ASIO
        private byte[] sampleToInt32Bytes(float sample)
        {
            // clip
            if (sample > 1.0f)
                sample = 1.0f;
            if (sample < -1.0f)
                sample = -1.0f;
            int i32 = (int)(sample * Int32.MaxValue);
            return BitConverter.GetBytes(i32);
        }
        private byte[] sampleToInt16Bytes(float sample)
        {
            // clip
            if (sample > 1.0f)
                sample = 1.0f;
            if (sample < -1.0f)
                sample = -1.0f;
            int i16 = (int)(sample * Int16.MaxValue);
            return BitConverter.GetBytes(i16);
        }

#endif
        VstPluginContext pluginCOntext;
        public void SetPluginCOntext(VstPluginContext pi )
        {
            if (myWave != null)
            {
                myWave.VstContext = pi;
                myWave.VstContext.PluginCommandStub.Commands.SetSampleRate(44100); //Drum machine was playing fast because this needed setting
            }
        }

        string[] cbAsio = null;
        public void Init(PluginHost.MainForm  mf)
        {
            mMainForm = mf;

            mSettings.AsioBufferSize = 1024; // 512;
            mSettings.mSelectedAsio = 0;

            int driverCount = 0;
            // get asio devices
            try
            {
#if NAUDIO_ASIO
                var driver = NAudio.Wave.AsioOut.GetDriverNames();
                cbAsio = driver;
#else
                driver = BlueWave.Interop.Asio.AsioDriver.InstalledDrivers;
                driverCount = driver.Count();
                //for (int i = 0; i < driver.Count(); i++)
                //{
                //    cbAsioDriver.Items.Add(driver[i].Name);
                //}
#endif
                //if (mSettingsMgr.Settings.AsioDeviceNr != -1)
                //{
                //    if (cbAsioDriver.Items.Count > mSettingsMgr.Settings.AsioDeviceNr)
                //    {
                //        cbAsioDriver.SelectedIndex = mSettingsMgr.Settings.AsioDeviceNr;
                //    }
                //}

                asioFail = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (!asioFail)
            {
                // get asio buffsizes
                if (driverCount > 0)
                {
                    try
                    {
#if NAUDIO_ASIO
                        // todo
#else
                        AsioDriver asio = AsioDriver.SelectDriver(AsioDriver.InstalledDrivers[0]);
#endif
                        //List<int> buffSizes = new List<int>();
                        //foreach (var item in cbAsioBufferSize.Items)
                        //{
                        //    buffSizes.Add(int.Parse(item.ToString()));
                        //}
                        //for (int i = 0; i < buffSizes.Count; i++)
                        {
#if NAUDIO_ASIO
                            // todo
#else
//                            if (buffSizes[i] < asio.BufferSizex.MinSize || buffSizes[i] > asio.BufferSizex.MaxSize)
//                            {
//                                buffSizes.RemoveAt(i);
//                                i--;
//                            }
#endif
                        }
#if NAUDIO_ASIO
                        // todo
#else
                        asio.Release();
                        asio = null;
#endif

                        //for (int i = 0; i < cbAsioBufferSize.Items.Count; i++)
                        //{
                        //    if ((string)cbAsioBufferSize.Items[i] == mSettingsMgr.Settings.AsioBufferSize.ToString())
                        //    {
                        //        cbAsioBufferSize.SelectedIndex = i;
                        //        break;
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        asioFail = true;
                    }
                }
            }
        }
        public PluginHost.AudioBufferInfo OutputBuffers { get; set; }
        public PluginHost.AudioBufferInfo InputBuffers { get; set; }

#if NAUDIO_ASIO
#else
        [STAThread]
#endif
        public unsafe void asioThread()
        {
            try
            {
#if NAUDIO_ASIO
                mAsio = new NAudio.Wave.AsioOut(0);
#else
                mAsio = AsioDriver.SelectDriver(AsioDriver.InstalledDrivers[0]);
                mAsio.SetSampleRate(44100);
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (mAsio != null)
            {

                int outSize = 2; // 64;
                int inSize = 32;

                if (InputBuffers == null || inSize != InputBuffers.Count)
                {
                    InputBuffers = null;    // Dispose first if already existed!
                    InputBuffers = new PluginHost.AudioBufferInfo(inSize, mSettings.AsioBufferSize);
                }
                if (OutputBuffers == null || outSize != OutputBuffers.Count)
                {
                    OutputBuffers = null;    // Dispose first if already existed!
                    OutputBuffers = new PluginHost.AudioBufferInfo(outSize, mSettings.AsioBufferSize);
                }

#if NAUDIO_ASIO
                if (mAsio != null)
                {
                    //mWaveProvider = new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat(44100, 16, 2));
                    //mWaveProvider = new NAudio.Wave.BufferedWaveProvider(new NAudio.Wave.WaveFormat(44100, 16, 2));
                    myWave = new MyWave();
                    mAsio.Init(myWave);
                    //mAsio.InitRecordAndPlayback(mWaveProvider, 0, -1);
                    //mAsio.InitRecordAndPlayback(null, 2, 44100);
                    mAsio.AudioAvailable += mAsio_AudioAvailable;

                    //mAsio.ShowControlPanel();
                    //mAsioBuffSize = mSettingsMgr.Settings.AsioBufferSize;
                }
#else
                int p = mAsio.BufferSizex.PreferredSize;
                int max = mAsio.BufferSizex.MaxSize;
                int min = mAsio.BufferSizex.MinSize;

  
                // get our driver wrapper to create its buffers
                mAsio.CreateBuffers(mSettings.AsioBufferSize);
                // this is our buffer fill event we need to respond to
                mAsio.BufferUpdate += new EventHandler(asio_BufferUpdateHandler);
                mAsioOutputLeft = mAsio.OutputChannels[0];      // Use only 1x stereo out
                mAsioOutputRight = mAsio.OutputChannels[1];

                mAsioInputBuffers = new PluginHost.AudioBufferInfo(2, mSettings.AsioBufferSize);
                mAsioInputLeft = mAsio.InputChannels[0];        // Use only 1x stereo in
                mAsioInputRight = mAsio.InputChannels[1];
#endif
                // todo: test
                //mMixLeft = new float[mAsioBuffSize];
                //mMixRight = new float[mAsioBuffSize];

                // and off we go

                //stopWatchTicksForOneAsioBuffer = (long)(Stopwatch.Frequency / (mAsio.SampleRate / mAsioBuffSize));
#if NAUDIO_ASIO
                mAsioLeftInt32LSBBuff = new byte[mAsioBuffSize * BYTES_PER_SAMPLE];
                    mAsioRightInt32LSBBuff = new byte[mAsioBuffSize * BYTES_PER_SAMPLE];
                    mAsio.Play();
#else
                mAsio.Start();
#endif
                mAsioStartEvent.Set();

                // Keep running untill stop request!
                mAsioStopEvent.Wait();
                StopAsio();
            }
            else
            {
                mIsAsioRunning = false;
                mAsioStartEvent.Set();
            }
        }

        private bool mIsAsioRunning = false;
        private bool mFirstAsioBufferUpdateHandlerCall;
        public void Start()
        {
            if(asioFail)
            {
                return;
            }
            if (mIsAsioRunning) return;

            //if (mSettingsMgr.Settings.AsioDeviceNr != -1)// AsioDriver.InstalledDrivers
            {
                mAsioStopEvent.Reset();
                mAsioStartEvent.Reset();

                mFirstAsioBufferUpdateHandlerCall = true;
                mAsioThread = new Thread(asioThread);
                mAsioThread.SetApartmentState(ApartmentState.STA);
                mAsioThread.Priority = ThreadPriority.Normal;
                mAsioThread.Start();

                mAsioStartEvent.Wait();
                mIsAsioRunning = true;
            }
        }

        [DllImport("avrt.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr AvSetMmThreadCharacteristics(string taskName, out uint taskIndex);

        [DllImport("avrt.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool AvRevertMmThreadCharacteristics(IntPtr avrtHandle);

#if NAUDIO_ASIO

        private unsafe void mAsio_AudioAvailable(object sender, NAudio.Wave.AsioAudioAvailableEventArgs e)
        {

#else

        private unsafe void asio_BufferUpdateHandler(object sender, EventArgs e)
        {
#endif
            VstPluginContext pi = mMainForm.GetMainPlugin();

            if (mFirstAsioBufferUpdateHandlerCall)
            {
                uint taskIndex;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                IntPtr handle = AvSetMmThreadCharacteristics("Pro Audio", out taskIndex);
                mFirstAsioBufferUpdateHandlerCall = false;
            }
#if NAUDIO_ASIO
#else
            // Clear output buffer
            for (int index = 0; index < mSettings.AsioBufferSize; index++)
            {
                mAsioOutputLeft[index] = 0.0f;
                mAsioOutputRight[index] = 0.0f;
            }
#endif
            if (pi == null)
            {
                return;
            }

            if(myWave!=null)
            {
                if(myWave.VstContext==null)
                {
                    myWave.VstContext = pi;
                }
            }
            // Get audio from the instrument plugin
            pi.PluginCommandStub.Commands.ProcessReplacing(InputBuffers.Buffers, OutputBuffers.Buffers);

            bool mTEST_VOL = false;
            if (OutputBuffers.Raw[0][0] > 0.0f)
            {
                mTEST_VOL = true;
            }
            float myVolume = 0.5f;

#if NAUDIO_ASIO
            //// Copy mix            
            for (int index = 0; index < mAsioBuffSize; index++)
            {
                // First copy left sample
                Buffer.BlockCopy(sampleToInt32Bytes(myVolume * OutputBuffers.Raw[0][index]), 0, mAsioLeftInt32LSBBuff, index * BYTES_PER_SAMPLE, BYTES_PER_SAMPLE);
                // Then copy right sample
                Buffer.BlockCopy(sampleToInt32Bytes(myVolume * OutputBuffers.Raw[1][index]), 0, mAsioRightInt32LSBBuff, index * BYTES_PER_SAMPLE, BYTES_PER_SAMPLE);
            }

            try
            {
                // Copy left buff
                Marshal.Copy(mAsioLeftInt32LSBBuff, 0, e.InputBuffers[0], e.SamplesPerBuffer * BYTES_PER_SAMPLE);

                // Copy right buff
                Marshal.Copy(mAsioRightInt32LSBBuff, 0, e.InputBuffers[1], e.SamplesPerBuffer * BYTES_PER_SAMPLE);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            //mVstTimeInfo.SamplePosition++;
            //e.WrittenToOutputBuffers = true;

#endif
  
#if NAUDIO_ASIO
#else
            // Now copy to mix
            for (int index = 0; index < mSettings.AsioBufferSize; index++)
            {
                mAsioOutputLeft[index] = myVolume*OutputBuffers.Raw[0][index];
                mAsioOutputRight[index] = myVolume*OutputBuffers.Raw[1][index];
            }
#endif
        }

        public void Stop()
        {
            //foreach (VstPluginChannel ch in PluginChannels)
            //{
            //    foreach (VstPlugin effectPlugin in ch.EffectPlugins)
            //    {
            //        StopPlugin(effectPlugin);
            //    }
            //    StopPlugin(ch.InstrumentPlugin);
            //}

            if (mIsAsioRunning)
            {
                VstPluginContext pi = mMainForm.GetMainPlugin();
                if (pi != null)
                {
                    pi.PluginCommandStub.Commands.StopProcess();
                    pi.PluginCommandStub.Commands.MainsChanged(false);
                    pi.PluginCommandStub.Commands.Close();
                    mMainForm.ReleaseAllPlugins();
                }
                mAsioStopCompleteEvent.Reset();
                mAsioStopEvent.Set();
                mAsioStopCompleteEvent.Wait();
             }

            //mMidiDevice.CloseAllInPorts();
        }
        //private void StopPlugin(VstPlugin plugin)
        //{
        //    if (plugin.State == PluginState.Activated)
        //    {
        //        plugin.Deactivate();
        //    }

        //    if (plugin.State == PluginState.Deactivated)
        //    {
        //        plugin.Unload();
        //    }
        //}

        private void StopAsio()
        {
            if (mAsio != null)
            {
                mAsio.Stop();
                // Wait at least 200 ms for the last buffer update event to have happened
                Thread.Sleep(200);
#if NAUDIO_ASIO
                mAsio.Dispose();
#else
                mAsio.Release();
#endif
                mAsio = null;
            }
            mIsAsioRunning = false;
            mAsioStopCompleteEvent.Set();
        }

    }
}
