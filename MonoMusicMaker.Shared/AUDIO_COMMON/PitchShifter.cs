﻿// this class based on code from awesomebox, a project created by by Ravi Parikh and Keegan Poppen, used with permission
// http://decabear.com/awesomebox.html
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MonoMusicMaker
{
    class PitchShifter
    {
        int detectedNote;
        protected float detectedPitch;  //set == inputPitch when shiftPitch is called
        protected float shiftedPitch;   //set == the target pitch when shiftPitch is called
        int numshifts;  //number of stored detectedPitch, shiftedPitch pairs stored for the viewer (more = slower, less = faster)
        Queue<PitchShift> shifts;
        protected int currPitch;
        protected int attack;
        int numElapsed;
        protected double vibRate;
        protected double vibDepth;
        double g_time;
        protected float sampleRate;

        protected AutoTuneSettings settings;

        public PitchShifter(AutoTuneSettings settings, float sampleRate)
        {
            this.settings = settings;
            this.sampleRate = sampleRate;
            numshifts = 5000;
            shifts = new Queue<PitchShift>(numshifts);

            currPitch = 0;
            attack = 0;
            numElapsed = 0;
            vibRate = 4.0;
            vibDepth = 0.00;
            g_time = 0.0;
        }

        protected float snapFactor(float freq)
        {
            float previousFrequency = 0.0f;
            float correctedFrequency = 0.0f;
            int previousNote = 0;
            int correctedNote = 0;
            for (int i = 1; i < 120; i++)
            {
                bool endLoop = false;
                foreach (int note in this.settings.AutoPitches)
                {
                    if (i % 12 == note)
                    {
                        previousFrequency = correctedFrequency;
                        previousNote = correctedNote;
                        correctedFrequency = (float)(8.175 * Math.Pow(1.05946309, (float)i)); //2 root 12 factor for getting between semitones to frequency
                        correctedNote = i;
                        if (correctedFrequency > freq)
                        {
                            endLoop = true;
                        }
                        break;
                    }
                }
                if (endLoop)
                {
                    break;
                }
            }
            if (correctedFrequency == 0.0) { return 1.0f; }
            int destinationNote = 0;
            double destinationFrequency = 0.0;
            // decide whether we are shifting up or down
            if (correctedFrequency - freq > freq - previousFrequency)
            {
                destinationNote = previousNote;
                destinationFrequency = previousFrequency;
            }
            else
            {
                destinationNote = correctedNote;
                destinationFrequency = correctedFrequency;
            }
            if (destinationNote != currPitch)
            {
                numElapsed = 0;
                currPitch = destinationNote;
            }
            if (attack > numElapsed)
            {
                double n = (destinationFrequency - freq) / attack * numElapsed;
                destinationFrequency = freq + n;
            }
            numElapsed++;
            return (float)(destinationFrequency / freq);
        }

        protected void updateShifts(float detected, float shifted, int targetNote)
        {
            if (shifts.Count >= numshifts) shifts.Dequeue();
            PitchShift shift = new PitchShift(detected, shifted, targetNote);
            Debug.WriteLine(shift);
            shifts.Enqueue(shift);
        }

        void setDetectedNote(float pitch)
        {
            for (int i = 0; i < 120; i++)
            {
                float d = (float)(8.175 * Math.Pow(1.05946309, (float)i) - pitch);
                if (-1.0 < d && d < 1.0)
                {
                    detectedNote = i;
                    return;
                }
            }
            detectedNote = -1;
        }

        bool isDetectedNote(int note)
        {
            return (note % 12) == (detectedNote % 12) && detectedNote >= 0;
        }

        protected float addVibrato(int nFrames)
        {
            g_time += nFrames;
            float d = (float)(Math.Sin(2 * 3.14159265358979 * vibRate * g_time / sampleRate) * vibDepth);
            return d;
        }
    }

    class SmbPitchShifter : PitchShifter
    {
        public SmbPitchShifter(AutoTuneSettings settings, float sampleRate) : base(settings, sampleRate) { }

        public void ShiftPitch(float[] inputBuff, float inputPitch, float targetPitch, float[] outputBuff, int nFrames)
        {
            UpdateSettings();
            detectedPitch = inputPitch;
            float shiftFactor = 1.0f;
            if (this.settings.SnapMode)
            {
                if (inputPitch > 0)
                {
                    shiftFactor = snapFactor(inputPitch);
                    shiftFactor += addVibrato(nFrames);
                }
            }
            else
            {
                float midiPitch = targetPitch;
                shiftFactor = 1.0f;
                if (inputPitch > 0 && midiPitch > 0)
                {
                    shiftFactor = midiPitch / inputPitch;
                }
            }

            if (shiftFactor > 2.0) shiftFactor = 2.0f;
            if (shiftFactor < 0.5) shiftFactor = 0.5f;

            // fftFrameSize was nFrames but can't guarantee it is a power of 2
            // 2048 works, let's try 1024
            int fftFrameSize = 2048;
            int osamp = 8; // 32 is best quality
            SmbPitchShift.smbPitchShift(shiftFactor, nFrames, fftFrameSize, osamp, this.sampleRate, inputBuff, outputBuff);

            //vibrato
            //addVibrato(outputBuff, nFrames);

            shiftedPitch = inputPitch * shiftFactor;
            updateShifts(detectedPitch, shiftedPitch, this.currPitch);
        }

        private void UpdateSettings()
        {
            //these are going here, because this gets called once per frame
            vibRate = this.settings.VibratoRate;
            vibDepth = this.settings.VibratoDepth;
            attack = (int)((this.settings.AttackTimeMilliseconds * 441) / 1024.0);
        }
    }
    
    class PitchShift
    {
        public PitchShift(float detected, float shifted, int destNote)
        {
            this.DetectedPitch = detected;
            this.ShiftedPitch = shifted;
            this.DestinationNote = destNote;
        }

        public float DetectedPitch { get; private set; }
        public float ShiftedPitch { get; private set; }
        public int DestinationNote { get; private set; }

        public override string ToString()
        {
            if(DetectedPitch>0.2f)
            {
                //https://newt.phys.unsw.edu.au/jw/notes.html
                int octaveNum = (DestinationNote / 12) - 1;
                NotePitchShift note = (NotePitchShift)(DestinationNote % 12);

                return String.Format("FOUND - detected {0:f2}Hz, shifted to {1:f2}Hz, {2}{3} DestinationNote {4}", DetectedPitch, ShiftedPitch,
                    note, octaveNum, DestinationNote);

            }
            else
            {
                return String.Format("detected {0:f2}Hz, shifted to {1:f2}Hz, {2}{3} ", DetectedPitch, ShiftedPitch,
    (NotePitchShift)(DestinationNote % 12), DestinationNote / 12);
            }
        }
    }
}
