using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jacobi.Vst.Core;
using Jacobi.Vst.Host.Interop;

namespace MonoMusicMaker
{
    public class VSTPlugin
    {
        public VstPluginContext PluginContext { get; set; }
        MidiQueue mMidiQueue = new MidiQueue();

        public VstAudioBufferManager outputBuffers;
        public VstAudioBufferManager inputBuffers;

        public VSTPlugin(VstPluginContext pi)
        {
            PluginContext = pi;

        }

        public void InitBuffer(int count, int BytesPerWaveSample)
        {
           outputBuffers = new VstAudioBufferManager(
               PluginContext.PluginInfo.AudioOutputCount,
               count / (PluginContext.PluginInfo.AudioOutputCount * BytesPerWaveSample)
            );
            inputBuffers = new VstAudioBufferManager(
               PluginContext.PluginInfo.AudioInputCount,
               count / (Math.Max(1, PluginContext.PluginInfo.AudioInputCount) * BytesPerWaveSample)
           );
        }
        public void AddMidiEvent(VstMidiEvent me)
        {
            mMidiQueue.AddMidiEvent(me);
        }

        public VstEvent[] GetMidiEvents()
        {
            return mMidiQueue.GetMidiEvents();
        }
        
        public void ClearMidiEvents()
        {
            mMidiQueue.ClearMidiEvents();
        }
    }
}
