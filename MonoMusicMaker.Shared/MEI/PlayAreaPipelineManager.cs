using System;
using System.Collections.Generic;
using System.Text;

namespace MonoMusicMaker
{
    public class PlayAreaPipelineManager
    {
        PlayArea mSource;
        PlayArea mDestination;
        public bool Active { get; set; } = false;

        public PlayArea GetDestination ()
        {
            return mDestination;
        }
        public PlayArea GetSource()
        {
            return mSource;
        }

        public bool SetSourceDestination(PlayArea source, PlayArea destination)
        {
            if (destination == source)
            {
                return false;
            }

            mDestination = destination;
            mSource = source;

            return true; // means they are different
        }

        public void Update(MelodyEditorInterface.MEIState state)
        {
            if(Active)
            {
                if(mSource!= null && mDestination!= null)
                {

                }
            }
        }
    }
}
