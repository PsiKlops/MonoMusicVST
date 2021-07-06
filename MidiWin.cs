using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;

using System.Windows.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoMusicMaker
{
    class MidiWin : MidiBase
    {
        const int CHANNEL_MIN = 1;
        const int CHANNEL_MAX = 16;
        MidiOut mMidiOut = null;
        //int mChannel = CHANNEL_MIN;


        public override void InitSDR()
        {
            if (mMidiOut == null)
            {
                for (int device = 0; device < MidiOut.NumberOfDevices; device++)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("MidiWIN Init device {0} name {1} ",
                        device, MidiOut.DeviceInfo(device).ProductName));
                    //comboBoxMidiInDevices.Items.Add(MidiIn.DeviceInfo(device).ProductName);
                }

                if (MidiOut.NumberOfDevices > 0)
                {
                    mMidiOut = new MidiOut(0); // just get first probably Microsoft GS Wavetable Synth
                    //for (int i=0;i< MidiOut.NumberOfDevices;i++)
                    //{
                    //    try
                    //    {
                    //        mMidiOut = new MidiOut(i); // just get first probably Microsoft GS Wavetable Synth
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        System.Diagnostics.Debug.WriteLine(string.Format("Exception getting index  {0} exception details {1}",i, e.ToString()));
                    //    }

                    //}
                }
            }
        }

        public override void SendMidi(int m, int n, int v)
        {
            //TODO GET RID
            ////MidiFile.DeltaTicksPerQuarterNote
            ////InitSDR(); //TODO
            //int noteNumber = n;
            //int velocity = v;
            //var noteOnEvent = new NoteOnEvent(0, mChannel, noteNumber, velocity, 50);
            //mMidiOut.Send(noteOnEvent.GetAsShortMessage());
        }
        public override void NoteOn(int channel, int note, int velocity)
        {
            //MidiFile.DeltaTicksPerQuarterNote
            //InitSDR(); //TODO
            int noteNumber = note;
            var noteOnEvent = new NoteOnEvent(0, channel, noteNumber, velocity, 50);
            mMidiOut.Send(noteOnEvent.GetAsShortMessage());
        }
        public override void NoteOff(int channel, int note)
        {
            //MidiFile.DeltaTicksPerQuarterNote
            //InitSDR(); //TODO
            // new NoteEvent(0, mChannel, noteNumber, 0, 50);
            var noteOnEvent = new NoteEvent(0, channel, MidiCommandCode.NoteOff, note, 0);
            mMidiOut.Send(noteOnEvent.GetAsShortMessage());
        }

        public override void SetInstrument(int instrument, int channel)
        {
            //InitSDR(); //TODO
            PatchChangeEvent pce = new PatchChangeEvent(0, channel, instrument);
            //MidiEvent me = new MidiEvent(0, mChannel, MidiCommandCode.PatchChange);
            int msgAsInt = (int)pce.GetAsShortMessage();

            System.Diagnostics.Debug.WriteLine(string.Format("SetInstrument channel {0} instrument {1} msgAsInt {2} ",
              channel, instrument, Convert.ToString(msgAsInt, 16)));

            mMidiOut.Send(pce.GetAsShortMessage());
        }

        public override void SetMidiController(int channel, int ccType, int ccParam)
        {
            MidiController midiControl = MidiController.AllNotesOff; //weird default, will exit before this can be used!

            switch (ccType)
            {
                case 7:
                    midiControl = MidiController.MainVolume;
                    break;
                case 10:
                    midiControl = MidiController.Pan;
                    break;

                default:
                    midiControl = (MidiController)ccType;
                    break;
            }

            ControlChangeEvent ce = new ControlChangeEvent(0, channel, midiControl,  ccParam);
            mMidiOut.Send(ce.GetAsShortMessage());
        }

        public override void SetBuffer(byte[] buffer)
        {
            mMidiOut.SendBuffer(buffer);
        }


        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] msg)
        {
            //InitSDR(); //TODO
        }


        public override void ShowKeyboard()
        {
            StartKB();
        }

        public override void HideKeyboard()
        {
            ExitKB();
        }
    }
}
