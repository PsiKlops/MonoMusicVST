using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoMusicMaker
{
    public class AutoTuneSettings
    {
        public AutoTuneSettings()
        {
            // set up defaults
            SnapMode = true;
            PluggedIn = true;
            AutoPitches = new HashSet<NotePitchShift>();
            AutoPitches.Add(NotePitchShift.C);
            AutoPitches.Add(NotePitchShift.CSharp); 
            AutoPitches.Add(NotePitchShift.D);
            AutoPitches.Add(NotePitchShift.DSharp);
            AutoPitches.Add(NotePitchShift.E);
            AutoPitches.Add(NotePitchShift.F);
            AutoPitches.Add(NotePitchShift.FSharp);
            AutoPitches.Add(NotePitchShift.G);
            AutoPitches.Add(NotePitchShift.GSharp); 
            AutoPitches.Add(NotePitchShift.A);
            AutoPitches.Add(NotePitchShift.ASharp); 
            AutoPitches.Add(NotePitchShift.B);
            VibratoDepth = 0.0;
            VibratoRate = 4.0;
            AttackTimeMilliseconds = 0.0;
        }

        public bool Enabled { get; set; }
        public bool SnapMode { get; set; } // snap mode finds a note from the list to snap to, non-snap mode is provided with target pitches from outside
        public double AttackTimeMilliseconds { get; set; }
        public HashSet<NotePitchShift> AutoPitches { get; private set; }
        public bool PluggedIn { get; set; } // not currently used
        public double VibratoRate { get; set; } // not currently used
        public double VibratoDepth { get; set; } 

        /*
         *  vibRateSlider = new GuiSlider(0.2, 20.0, 4.0);
            vibDepthSlider = new GuiSlider(0.0, 0.05, 0);
            attackSlider = new GuiSlider(0.0, 200, 0.0);
         */
    }

    public enum NotePitchShift
    {
        C, CSharp, D, DSharp, E, F, FSharp, G, GSharp, A, ASharp, B
    }
}
