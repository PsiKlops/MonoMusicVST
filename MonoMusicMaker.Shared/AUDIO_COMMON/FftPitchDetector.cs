﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoMusicMaker
{
    // FFT based pitch detector. seems to work best with block sizes of 4096
    public class FftPitchDetector : IPitchDetector
    {
        private float sampleRate;

        public FftPitchDetector(float sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        // http://en.wikipedia.org/wiki/Window_function
        private float HammingWindow(int n, int N) 
        {
            return 0.54f - 0.46f * (float)Math.Cos((2 * Math.PI * n) / (N - 1));
        }

        private float[] fftBuffer;
        private float[] prevBuffer;
        public float DetectPitch(float[] buffer, int inFrames)
        {
            Func<int, int, float> window = HammingWindow;
            if (prevBuffer == null)
            {
                prevBuffer = new float[inFrames];
            }

            // double frames since we are combining present and previous buffers
            int frames = inFrames * 2;
            if (fftBuffer == null)
            {
                fftBuffer = new float[frames * 2]; // times 2 because it is complex input
            }

            for (int n = 0; n < frames; n++)
            {
                if (n < inFrames)
                {
                    fftBuffer[n * 2] = prevBuffer[n] * window(n, frames);
                    fftBuffer[n * 2 + 1] = 0; // need to clear out as fft modifies buffer
                }
                else
                {
                    fftBuffer[n * 2] = buffer[n-inFrames] * window(n, frames);
                    fftBuffer[n * 2 + 1] = 0; // need to clear out as fft modifies buffer
                }
            }

            // assuming frames is a power of 2
            SmbPitchShift.smbFft(fftBuffer, frames, -1);

            //"(I am using 85Hz to 300Hz, which is adequate for pitch detecting vocals)" https://channel9.msdn.com/coding4fun/articles/AutotuneNET
            float binSize = sampleRate / frames;
            int minBin = (int)(AutoCorrelator.MIN_FREQUENCY / binSize);
            int maxBin = (int)(AutoCorrelator.MAX_FREQUENCY / binSize);
            float maxIntensity = 0f;
            int maxBinIndex = 0;
            for (int bin = minBin; bin <= maxBin; bin++)
            {
                float real = fftBuffer[bin * 2];
                float imaginary = fftBuffer[bin * 2 + 1];
                float intensity = real * real + imaginary * imaginary;
                if (intensity > maxIntensity)
                {
                    maxIntensity = intensity;
                    maxBinIndex = bin;
                }
            }
            const float MIN_INTENSITY = 0.1f;

            for (int n = 0; n < inFrames; n++) //Stu: found this wasn't ever set, so added this here!?
            {
                prevBuffer[n] = buffer[n];
            }

            if (maxIntensity> MIN_INTENSITY)
            {
                return binSize * maxBinIndex;
            }
            else
            {
                return 0f;
            }
        }
    }
}
