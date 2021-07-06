using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NAudio.Midi;

namespace MonoMusicMaker
{
    public class TopLineDrumAI
    {
        public const float FOUR_BEAT_DRUM_REGION_RANGE = 4.0f;
        const int mBeatLength = PlayArea.NUM_DRAW_BEATS; //num beats in track
        public static int mTrackWidth;

        public Rectangle mRect;
        public PlayArea mParent;
        public int mStartX;
        public int mStartY;

        public bool mSelected = false;

        List<AIDrumRegion> mRegions;

        public TopLineDrumAI(PlayArea parent)
        {
            mParent = parent;
            Init();
        }

        public void Init()
        {
            mStartX = MelodyEditorInterface.TRACK_START_X;
            mStartY = MelodyEditorInterface.TRACK_START_Y - NoteLine.TRACK_HEIGHT;
            mTrackWidth = (int)mBeatLength * (int)MelodyEditorInterface.BEAT_PIXEL_WIDTH;
            mRect = new Rectangle(mStartX, mStartY, mTrackWidth, NoteLine.TRACK_HEIGHT);

            mRegions = new List<AIDrumRegion>(); //empty set

            int numRegions = (int)PlayArea.MAX_BEATS / (int)(FOUR_BEAT_DRUM_REGION_RANGE);
            float currentBeatPos = 0f;

            for (int i = 0; i < numRegions; i++)
            {
                AIDrumRegion dr = new AIDrumRegion(this, false);
                dr.SetPosLen(currentBeatPos, FOUR_BEAT_DRUM_REGION_RANGE);
                mRegions.Add(dr);

                currentBeatPos += FOUR_BEAT_DRUM_REGION_RANGE;
            }
        }

        // // // // // // // // // // // // //
        //LOAD SAVE
        public List<AIDrumRegion.AIDrumSaveData> GetRegionSaveData()
        {
            List<AIDrumRegion.AIDrumSaveData> saveRegionData = new List<AIDrumRegion.AIDrumSaveData>();

            foreach (AIDrumRegion r in mRegions)
            {
                AIDrumRegion.AIDrumSaveData sd = r.GetSaveData();
                saveRegionData.Add(sd);
            }

            return saveRegionData;
        }

        public void SetRegionSaveData(List<AIDrumRegion.AIDrumSaveData> loadRegionData)
        {
            mRegions = new List<AIDrumRegion>(); //Will set up with empty stuff in init but when loading save need to empty list here
            foreach (AIDrumRegion.AIDrumSaveData ld in loadRegionData)
            {
                AIDrumRegion possibleRegion = new AIDrumRegion(this, ld.mbThirdBeat);
                mParent.RemoveAllNotesInRegion(ld.BeatPos, ld.BeatPos + FOUR_BEAT_DRUM_REGION_RANGE); //get rid of any loaded in notes in preparation for re-generating them from drum data from json
                possibleRegion.SetPosLen(ld.BeatPos, FOUR_BEAT_DRUM_REGION_RANGE);

                possibleRegion.LoadSaveData(ld);
                mRegions.Add(possibleRegion);
            }
        }
        //LOAD SAVE
        // // // // // // // // // // // // //

        public List<AIDrumRegion> GetOverLappedRegions(Rectangle rect)
        {
            List<AIDrumRegion> overllapedRegions = new List<AIDrumRegion>();

            foreach (AIDrumRegion r in mRegions)
            {
                if(r.mRect.Intersects(rect))
                {
                    overllapedRegions.Add(r);
                }
            }

            return overllapedRegions;
        }

        public void RefreshDrawRectForOffset(int offset)
        {
            //Refresh existing notes
            foreach (AIDrumRegion r in mRegions)
            {
                r.RefreshRectToData();
            }
        }

        public bool QuarterNoteHere(float pos)
        {
            foreach (AIDrumRegion r in mRegions)
            {
                if (r.mbHasBeenEdited && r.IsNoteInRegion(pos, 0.25f))
                {
                    return true;
                }
            }

            return false;
        }

        //Update is updating input, play area update will take care of playing the laid down notes from the drum interface
        public bool UpdateInput(MelodyEditorInterface.MEIState state, bool bAllowInputPlayArea, ref bool InActiveRegion)
        {
            //Dont we need to just update while the song is passing through like a note?
            foreach (AIDrumRegion r in mRegions)
            {
                r.Update(state);
                //InActiveRegion |= r.Active;
            }

            return false;
        }

        public void Draw(SpriteBatch sb)
        {
            Color col = Color.Purple;
            if (mSelected)
            {
                col = Color.Yellow;
            }
            sb.FillRectangle(mRect, col);

            foreach (AIDrumRegion r in mRegions)
            {
                r.Draw(sb);
            }
        }
    }
}
