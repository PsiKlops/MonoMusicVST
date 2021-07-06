using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Commons.Music.Midi;
using NAudio.Midi;

#if ANDROID
using Android.Media.Midi;
#endif

namespace MonoMusicMaker
{
    public class MidiSynthIn
    {
        MidiIn mMidiIn;
        MidiBase mMidiBase;
        //List<string> mInDevices = new List<string>(); 

        public bool Enabled { get; set; } = false;

        public MidiSynthIn(MidiBase midiBase)
        {
            mMidiBase = midiBase;
            Init();
        }

        public bool Init()
        {
#if ANDROID
#else
            //mInDevices.Clear();

            //for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            //{
            //    mInDevices.Add(MidiIn.DeviceInfo(device).ProductName);
            //}

            if (MidiIn.NumberOfDevices > 0)
            {
                mMidiIn = new MidiIn(0);

                if (mMidiIn != null)
                {
                    mMidiIn.MessageReceived += new EventHandler<MidiInMessageEventArgs>(midiIn_MessageReceived);
                    mMidiIn.ErrorReceived += new EventHandler<MidiInMessageEventArgs>(midiIn_ErrorReceived);
                    mMidiIn.Start();
                }

                string productName = MidiIn.DeviceInfo(0).ProductName;

                System.Diagnostics.Debug.WriteLine(string.Format("SYNTH IN DEBUG {0}. Device name {1}", mMidiIn, productName));
                mMidiBase.mMidiKeyboardName = productName;

                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(string.Format("NO MIDI IN DEVICES!! :( "));
            }
#endif
            return false;
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            mMidiBase.mbRecordEnabled = Enabled;
#if ANDROID
#else
            if (MidiIn.NumberOfDevices == 0)
            {
                if (mMidiIn != null)
                {
                    mMidiIn = null;
                    mMidiBase.mMidiKeyboardName = "";
                }
                return false;
            }
            else if (mMidiIn == null)
            {
                if (Init())
                {
                    return true;
                }
            }

            if (mMidiIn != null)
            {
                return true;
            }

#endif
            return false;
        }

        void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(Color.Red, String.Format("Time {0} Message 0x{1:X8} Event {2}",
                                                             e.Timestamp, e.RawMessage, e.MidiEvent));
        }

        private void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(Color.Blue, String.Format("Time {0} Message 0x{1:X8} Event {2}",
                                                             e.Timestamp, e.RawMessage, e.MidiEvent));

            if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn)
            {
                NoteEvent noe = (NoteEvent)e.MidiEvent;
                mMidiBase.MidiInNoteOn(noe.Channel, noe.NoteNumber, noe.Velocity);
            }
            else if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOff)
            {
                NoteEvent noe = (NoteEvent)e.MidiEvent;
                mMidiBase.MidiInNoteOff(noe.Channel, noe.NoteNumber);
            }
            else if (e.MidiEvent.CommandCode == MidiCommandCode.ControlChange)
            {
                ControlChangeEvent cce = (ControlChangeEvent)e.MidiEvent;
                int value = 0;
                if (cce.Controller == MidiController.Modulation)
                {
                    value = cce.ControllerValue;
                    mMidiBase.SetControllerBend(value);
                }
            }
            else if (e.MidiEvent.CommandCode == MidiCommandCode.PitchWheelChange)
            {
                PitchWheelChangeEvent pwce = (PitchWheelChangeEvent)e.MidiEvent;
                mMidiBase.SetPitchBend(pwce.Pitch);
            }
        }
    }

}
