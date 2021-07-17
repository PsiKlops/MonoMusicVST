using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jacobi.Vst.Core;
using Jacobi.Vst.Host.Interop;

namespace MonoMusicMaker
{
    public class PluginAudioManager : MidiBase
    {
        public const byte NOTE_ON = 0x90;
        public const byte NOTE_OFF = 0x80;
        public const byte CHANNEL_MASK = 0x07;


        public Object mLock = new Object();

        List<VSTPlugin> mPlugins = new List<VSTPlugin>();

        public override void NoteOn(int channel, int NoteNum, int velocity)
        {
            NoteCommand(channel, NoteNum, velocity, true);
        }

        public override void NoteOff(int channel, int NoteNum)
        {
            NoteCommand(channel, NoteNum, 0, false);
        }

        void NoteCommand(int channel, int NoteNum, int velocity, bool bOn)
        {
            //LOCK
            //lock (mLock)
            {
                int idx = channel - 1;
                if (mPlugins.Count > idx)
                {
                    List<VstEvent> midiEvents = new List<VstEvent>(1024);

                    midiEvents.Clear();
                    VSTPlugin vp = mPlugins[idx];

                    byte[] midiData = new byte[4];

                    channel -= 1;
                    if (channel > 15 || channel < 0)
                    {
                        channel = 0;
                    }
                    int command = NOTE_ON | channel;

                    if (!bOn)
                    {
                        command = NOTE_OFF | channel;
                        velocity = 0;
                    }
                    midiData[0] = (byte)command;
                    midiData[1] = (byte)NoteNum;
                    midiData[2] = (byte)velocity;
                    midiData[3] = 0;

                    VstMidiEvent midiEvent = new VstMidiEvent(
                        /*DeltaFrames*/ 0,
                        /*NoteLength*/ 0,
                        /*NoteOffset*/  0,
                                        midiData,
                        /*Detune*/        0,
                        /*NoteOffVelocity*/ 127);
                    //midiEvents.Add(midiEvent);

                    vp.AddMidiEvent(midiEvent);
                    //vp.PluginContext.PluginCommandStub.Commands.ProcessEvents(midiEvents.ToArray());
                }
            }
        }


        public void ProcessAllPluginMidi()
        {
            foreach (VSTPlugin vp in mPlugins)
            {
                VstEvent[] filteredMidiEvents = vp.GetMidiEvents();
                if(filteredMidiEvents.Length>0)
                {
                    vp.PluginContext.PluginCommandStub.Commands.ProcessEvents(filteredMidiEvents);
                }
                vp.ClearMidiEvents();
            }
        }

        public VSTPlugin GetPluginContext(int indx)
        {
            if(mPlugins.Count>0)
            {
               return  mPlugins[indx];
            }

            return null;
         }

        public int GetNumPluginContext()
        {
            return mPlugins.Count;
        }


        public VstEvent[] GetMidiEvents(int piIndex)
        {
            VSTPlugin vp = GetPluginContext(piIndex);

            if(vp == null)
            {
                return null;
            }

            return vp.GetMidiEvents();
        }
        public void ClearMidiEvents(int piIndex)
        {
            VSTPlugin vp = GetPluginContext(piIndex);

            if (vp == null)
            {
                return;
            }

            vp.ClearMidiEvents();
        }

        public void AddPlugin(VstPluginContext pi)
        {
            pi.PluginCommandStub.Commands.SetSampleRate(44100); //TODO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! put before new VSTPlugin
            VSTPlugin vp = new VSTPlugin(pi);
            mPlugins.Add(vp);
        }

        public bool RemovePlugin(VstPluginContext pi)
        {
            foreach(VSTPlugin vp in mPlugins)
            {
                if(vp.PluginContext==pi)
                {
                    mPlugins.Remove(vp);
                    return true;
                }
            }

            return false;
        }

 
        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override void InitSDR()
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] msg)
        {
            throw new NotImplementedException();
        }

        public override void SendMidi(int m, int n, int v)
        {
            throw new NotImplementedException();
        }

        //public override void NoteOn(int channel, int note, int velocity)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void NoteOff(int channel, int note)
        //{
        //    throw new NotImplementedException();
        //}

        public override void SetInstrument(int instrument, int channel)
        {
            throw new NotImplementedException();
        }

        public override void ShowKeyboard()
        {
            throw new NotImplementedException();
        }

        public override void HideKeyboard()
        {
            throw new NotImplementedException();
        }

        public override void SetMidiController(int channel, int ccType, int ccParam)
        {
            throw new NotImplementedException();
        }

        public override void SetBuffer(byte[] buffer)
        {
            throw new NotImplementedException();
        }
    }
}
