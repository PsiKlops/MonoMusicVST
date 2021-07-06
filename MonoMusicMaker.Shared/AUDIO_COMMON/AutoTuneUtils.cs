using System;
using NAudio.Wave;

namespace MonoMusicMaker
{
    public class AutoTuneUtils
    {
        public const int BUFFER_LENGTH = 8192 * 2;
        //public const int ALT_BUFFER_LENGTH = 4096;
        public const int ALT_BUFFER_LENGTH = (int)AudioRecordBase.m_sampleRate / Pitch.PitchTracker.PITCH_RECORDS_PER_SEC * 4; //  882*4;

        public bool UseAlternatePitch { get; set; } = true;

        public static void ApplyAutoTune(string fileToProcess, string tempFile, AutoTuneSettings autotuneSettings, Pitch.PitchTracker pt = null)
        {
            if (pt != null)
            {
                using (var reader = new WaveFileReader(fileToProcess))
                {
                    Wave16ToFloatProvider stream32 = new Wave16ToFloatProvider(reader);
                    int locbytesRead;
                    WaveBuffer waveBuffer = null;

                    do
                    {
                        if (waveBuffer == null || waveBuffer.MaxSize < ALT_BUFFER_LENGTH)
                        {
                            waveBuffer = new WaveBuffer(ALT_BUFFER_LENGTH);
                        }

                        locbytesRead = stream32.Read(waveBuffer, 0, ALT_BUFFER_LENGTH);  //reader.Length - (waveBuffer.Length % reader.WaveFormat.BlockAlign)
                                                                                         //Debug.Assert(bytesRead == count);
                        if(locbytesRead!=0)
                        {
                            //pitchsource->getPitches();
                            int frames = locbytesRead / sizeof(float); // MRH: was count

                            if (frames == ALT_BUFFER_LENGTH)
                            {
                                frames = 0;
                            }
                            pt.ProcessBuffer(waveBuffer.FloatBuffer, reader.CurrentTime, frames);
                        }

                        //converted.Write(buffer, 0, locbytesRead);
                    } while (locbytesRead != 0);
                }
            }
            else
            {
                using (var reader = new WaveFileReader(fileToProcess))
                {
                    Wave16ToFloatProvider stream32 = new Wave16ToFloatProvider(reader);
                    IWaveProvider streamEffect = new AutoTuneWaveProvider(stream32, autotuneSettings);
                    IWaveProvider stream16 = new WaveFloatTo16Provider(streamEffect);
                    using (var converted = new WaveFileWriter(tempFile, stream16.WaveFormat))
                    {
                        // buffer length needs to be a power of 2 for FFT to work nicely
                        // however, make the buffer too long and pitches aren't detected fast enough
                        // successful buffer sizes: 8192, 4096, 2048, 1024
                        // (some pitch detection algorithms need at least 2048)
                        var buffer = new byte[BUFFER_LENGTH];
                        var fbuffer = new float[BUFFER_LENGTH];
                        int bytesRead;

                        do
                        {
                            bytesRead = stream16.Read(buffer, 0, buffer.Length);
                            converted.Write(buffer, 0, bytesRead);
                        } while (bytesRead != 0 && converted.Length < reader.Length);
                    }
                }
            }
        }
    }
}
