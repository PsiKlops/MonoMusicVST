using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using  System.Threading;




namespace MonoMusicMaker
{
    public abstract class AudioRecordBase
    {
        public AutoTuneSettings AutoTuneSettings { get; set; }

        public string pathFileString = "NONE";
        public string fullSavePathName = "NONE";
        public string TEMPfullSavePathName = "NONE";
        public string fileDelim = "";
        public abstract void Init();
        public abstract void Start();
        public abstract void Stop();
        public abstract bool Stopped();

        public const float m_sampleRate = 44100.0f;
        public const float mSampleInterval = (float)AutoTuneUtils.ALT_BUFFER_LENGTH / m_sampleRate;

        float mLastSampleTime = 0f;

        public bool Processing { get; set; } = false;
        public bool ResultsReady { get; set; } = false;

        int mLastMidiNote = 0;

        Pitch.PitchTracker mpt;
        List<Pitch.PitchTracker.PitchRecord> mPitchRecords = null;
        List<Pitch.PitchTracker.PitchRecord> mOldPitchRecords = null;

        public List<Pitch.PitchTracker.PitchRecord> GetPitchRecords()
        {
            return mPitchRecords;
        }

        public void SetFilePaths()
        {
            mpt = new Pitch.PitchTracker();
            mpt.SampleRate = m_sampleRate;
            mpt.PitchDetected += OnPitchDetected;
            mpt.Reset();

            string newFile = DateTime.Now.ToString("d") + "_" + DateTime.Now.ToString("T"); // https://github.com/dotnet/coreclr/issues/2317 ToShortDateString() is the same as ToString("d"), ToLongTimeString use DateTime.ToString("T")
            //await Task.Delay(10000);
            newFile = newFile.Replace('/', '.');
            newFile = newFile.Replace(':', '.');
            fullSavePathName = pathFileString + fileDelim + newFile + ".WAV";
            TEMPfullSavePathName = pathFileString + fileDelim + "TEMP"+ newFile + ".WAV";
        }

        private void OnPitchDetected(Pitch.PitchTracker sender, Pitch.PitchTracker.PitchRecord pitchRecord)
        {
            // During the call to PitchTracker.ProcessBuffer, this event will be fired zero or more times,
            // depending how many pitch records will fit in the new and previously cached buffer.
            //
            // This means that there is no size restriction on the buffer that is passed into ProcessBuffer.
            // For instance, ProcessBuffer can be called with one large buffer that contains all of the
            // audio to be processed, or just a small buffer at a time which is more typical for realtime
            // applications. This PitchDetected event will only occur once enough data has been accumulated
            // to do another detect operation.
            if(pitchRecord.Pitch>0)
            {
                Console.WriteLine("PITCH  - {0} MIDI NOTE  - {1}", pitchRecord.Pitch, pitchRecord.MidiNote);
                System.Diagnostics.Debug.WriteLine("PITCH  - {0} MIDI NOTE  - {1} Seconds: {2}", pitchRecord.Pitch, pitchRecord.MidiNote, pitchRecord.mTimeWhen.TotalSeconds);

                if(mLastMidiNote!= pitchRecord.MidiNote)
                {
                    Console.WriteLine("PITCH  - {0} MIDI NOTE  - {1} \n", pitchRecord.Pitch, pitchRecord.MidiNote);
                    System.Diagnostics.Debug.WriteLine(" - - - NEW NOTE! PITCH  - {0} MIDI NOTE  - {1} Seconds: {2}", pitchRecord.Pitch, pitchRecord.MidiNote, pitchRecord.mTimeWhen.TotalSeconds);
                }

                //mPitchRecords.Add(pitchRecord);
                mLastMidiNote = pitchRecord.MidiNote;
            }
            float fTotalSecs = (float)pitchRecord.mTimeWhen.TotalSeconds;
            float fDiff = fTotalSecs - mLastSampleTime;

            mLastSampleTime = fTotalSecs;
            mPitchRecords.Add(pitchRecord);
        }

        public async void ProcessNotesPostRecord()
        {
            while (!Stopped())
            {
                ; ///AAAAAAARGH!
                await Task.Delay(10);
                Console.WriteLine("WAIT FOR STOPPED \n");
                System.Diagnostics.Debug.WriteLine("WAIT FOR STOPPED \n");
            }

            //await Task.Delay(600);
            this.AutoTuneSettings = new AutoTuneSettings(); // default settings
            Process();
        }

        public void Process()
        {
            ResultsReady = false;
            Processing = true;
            if (fullSavePathName=="NONE")
            {
                return;
            }

            if(mpt!=null)
            {
                mpt.Reset();
            }

            mOldPitchRecords = mPitchRecords;
            mPitchRecords = new List<Pitch.PitchTracker.PitchRecord>();

            AutoTuneUtils.ApplyAutoTune(fullSavePathName, TEMPfullSavePathName, AutoTuneSettings, mpt);

            Processing = false;
            ResultsReady = true;
        }
    }
}
