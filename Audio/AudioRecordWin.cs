using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoMusicMaker
{
    public class AudioRecordWin : AudioRecordBase
    {
        AudioRecorder mAudioRecorder;

        public override void Init()
        {
            fileDelim = "\\";
            //string pathFileString = ".";
            //var pathWithEnv = @"%USERPROFILE%\AppData\";
            //var pathFileString = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            pathFileString = SaveLoad.PC_PATH_FILE;
            mAudioRecorder = new AudioRecorder();
        }

        public override void Start()
        {
            base.SetFilePaths();
            mAudioRecorder.BeginMonitoring(0);
            mAudioRecorder.BeginRecording(fullSavePathName);
        }

        public override void Stop()
        {
            mAudioRecorder.Stop();

            base.ProcessNotesPostRecord();
        }

        public override bool Stopped()
        {
            return mAudioRecorder.IsStopped();
        }
    }
}
