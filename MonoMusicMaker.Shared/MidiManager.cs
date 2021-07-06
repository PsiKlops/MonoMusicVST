using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Midi;

namespace MonoMusicMaker
{
    public class MidiManager
    {
        const int MAX_INSTRUMENT = 100;
        const int MIN_INSTRUMENT = 0;

        public int mMMInstrument { get; set; } = 0;

        // public void SendBuffer(byte[] byteBuffer);

//      Room 1 Reverb F0 41 10 42 12 40 01 30 00 0F F7
//      Room 2 Reverb F0 41 10 42 12 40 01 30 01 0E F7
//      Room 3 Reverb F0 41 10 42 12 40 01 30 02 0D F7
//      Hall 1 Reverb F0 41 10 42 12 40 01 30 03 0C F7
//      Hall 2 Reverb(default)        F0 41 10 42 12 40 01 30 04 0B F7
//      Plate Reverb F0 41 10 42 12 40 01 30 05 0A F7
//      Delay F0 41 10 42 12 40 01 30 06 09 F7
//      Panning Delay F0 41 10 42 12 40 01 30 07 08 F7
//      Chorus 1                                     F0 41 10 42 12 40 01 38 00 07 F7
//      Chorus 2                                     F0 41 10 42 12 40 01 38 01 06 F7


        MidiBase mMidiBase = null;
        public MidiManager(MidiBase mb)
        {
            mMidiBase = mb;
        }

        public MidiBase GetMidiBase() { return mMidiBase; }

        public void Play(int noteNum)
        {
            //assert 

            if(mMidiBase!=null)
            {
                mMidiBase.SendMidi(0x90, 66, noteNum);
            }
        }

        public void MMSetInstrument(int instr, int channel)
        {
            if (mMidiBase != null)
            {
                mMMInstrument = instr; //TODO will be different per track / channel
                mMidiBase.SetInstrument(instr, channel);
            }
        }
        public void MMSetVolume(int volume, int channel)
        {
            if (mMidiBase != null)
            {
                mMidiBase.SetMidiController( channel, (int)MidiController.MainVolume,  volume);
            }
        }
        public void MMSetPan(int pan, int channel)
        {
            if (mMidiBase != null)
            {
                mMidiBase.SetMidiController( channel, (int)MidiController.Pan,  pan);
            }
        }

        public void MMSetCommand(int commandNum, int value, int channel)
        {
            if (mMidiBase != null)
            {
                mMidiBase.SetMidiController(channel, commandNum, value);
            }
        }

    }
}
