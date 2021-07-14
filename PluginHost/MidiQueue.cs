using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jacobi.Vst.Core;
using Jacobi.Vst.Host.Interop;

namespace MonoMusicMaker
{
    public class MidiQueue
    {
        const int SIZE_MIDI_QUEUE = 1024;
        
        List<VstEvent> mMidiEvents = new List<VstEvent>(SIZE_MIDI_QUEUE);


        public void AddMidiEvent(VstMidiEvent me)
        {
             mMidiEvents.Add(me);
        }

        public VstEvent[] GetMidiEvents()
        {
            return mMidiEvents.ToArray();
        }
        public void ClearMidiEvents()
        {
            mMidiEvents.Clear();
        }

    }
}
