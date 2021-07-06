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
    public class TopLineChordAI
    {
        float mScale = 1f;
        public const float TWO_BEAT_REGION_RANGE = 2.0f;
        public const float FOUR_BEAT_REGION_RANGE = 4.0f;
        const int mBeatLength = PlayArea.NUM_DRAW_BEATS; //num beats in track

        public Rectangle mRect;
        public PlayArea mParent;
        public SpriteFont mFont;
        List<AIChordRegion> mRegions;
        List<AIChordRegion.AIRegionSaveData> mDeletedRegions; //when undoing or clicking again on space where old region was and saved here
        public static int mTrackWidth;
        public int mStartX;
        public int mStartY;
        bool mbIsDrumChannel = false;

        //Wrong put in airegion itself List<Note> mRootNotes; //set up from the top single note along the tracks below to give the root note for chords arpeggios etc

        AIChordRegion mLastRegion = null;

        public TopLineChordAI(PlayArea parent)
        {
            mParent = parent;

            if(mParent.mChannel==10)
            {
                mbIsDrumChannel = true;
            }
        }

        public bool DeleteRegion(AIChordRegion region)
        {
            foreach (AIChordRegion r in mRegions)
            {
                if(r == region)
                {
                    AIChordRegion.AIRegionSaveData rsd =  r.GetSaveData();
                    mDeletedRegions.Add(rsd);
                    r.RefreshAfterChange(null, false, true);
                    mRegions.Remove(r);
                    return true;
                }
            }
            return false;
        }
        public void RemoveDeleteRegion(AIChordRegion.AIRegionSaveData rsd)
        {
            mDeletedRegions.Remove(rsd);
        }

        public void Init()
        {
            mStartX = MelodyEditorInterface.TRACK_START_X;
            mStartY = MelodyEditorInterface.TRACK_START_Y - NoteLine.TRACK_HEIGHT;
            mTrackWidth = (int)mBeatLength * (int)MelodyEditorInterface.BEAT_PIXEL_WIDTH;
            mRect = new Rectangle(mStartX, mStartY, mTrackWidth, NoteLine.TRACK_HEIGHT);

            mRegions = new List<AIChordRegion>(); //empty set
            mDeletedRegions = new List<AIChordRegion.AIRegionSaveData>();
        }

        // // // // // // // // // // // // //
        //LOAD SAVE
        public List<AIChordRegion.AIRegionSaveData> GetRegionSaveData()
        {
            List<AIChordRegion.AIRegionSaveData> saveRegionData = new List<AIChordRegion.AIRegionSaveData>();

            foreach (AIChordRegion r in mRegions)
            {
                AIChordRegion.AIRegionSaveData sd = r.GetSaveData();
                saveRegionData.Add(sd);
            }

            return saveRegionData;
        }

        public void SetRegionSaveData(List<AIChordRegion.AIRegionSaveData> loadRegionData)
        {
            foreach (AIChordRegion.AIRegionSaveData ld in loadRegionData)
            {
                AIChordRegion possibleRegion = new AIChordRegion(this);
                possibleRegion.LoadSaveData(ld);
                mRegions.Add(possibleRegion);
            }
        }
        //LOAD SAVE
        // // // // // // // // // // // // //

        public bool Update(MelodyEditorInterface.MEIState state, bool bAllowInputPlayArea, ref bool InActiveRegion)
        {

            //TODO PARAM REGION NEEDS TO GO OVER THIS _ OR EVENTUALLY BE SWAP ABLE
            //if(mbIsDrumChannel)
            {
                return false;
            }
            //Dont we need to just update while the song is passing through like a note?
            foreach (AIChordRegion r in mRegions)
            {
                r.Update(state);
                //InActiveRegion |= r.Active;
            }

            if(state.mbPlayAreaUpdate)
            {
                if (state.mbRegionRefreshToEdit)
                {
                    bool bPopupNeedsToChange = RefreshAllRegionsToLatest(null, true);

                    if(bPopupNeedsToChange)
                    {
                        state.mAIRegionPopup.RefreshPopupToRegion();
                    }
                    state.mbRegionRefreshToEdit = false;
                }
            }

            if(mParent.HasCurrentView)
            {
                if (state.mbAllowInputPlayArea && bAllowInputPlayArea) //TODO rationalise
                {
                    return InputUpdate(state);
                }
            }

            return false;
        }


        public bool RefreshAllRegionsToLatest(Note addedNote = null, bool bForceRefresh = false)
        {
            bool bPopupNeedsToChange = false;
            //Dont we need to just update while the song is passing through like a note?
            foreach (AIChordRegion r in mRegions)
            {
                r.RefreshAfterChange(addedNote, bForceRefresh);

                bPopupNeedsToChange |= r.ChangePopupAfterRefreshToData;
            }

            return bPopupNeedsToChange;
        }

        AIChordRegion TouchInRegion(Rectangle touchRect)
        {
            foreach (AIChordRegion r in mRegions)
            {
                if (r.Overlaps(touchRect))
                {
                    return r;
                }
            }
            return null;
        }

        AIChordRegion.AIRegionSaveData GetAnySavedRegionHere(float touchPoint)
        {
            foreach (AIChordRegion.AIRegionSaveData rsd in mDeletedRegions)
            {
                if (rsd.PointInRegion(touchPoint))
                {
                    return rsd;
                }
            }
            return null;
        }

        public bool QuarterNoteHere(float pos)
        {
            foreach (AIChordRegion r in mRegions)
            {
                if (r.IsNoteInRegion(pos, 0.25f))
                {
                    return true;
                }
            }

            return false;
        }

        public void RefreshDrawRectForOffset(int offset)
        {
            //Refresh existing notes
            foreach (AIChordRegion r in mRegions)
            {
                r.RefreshRectToData();
            }
        }

        //Input allow setting 2 regions per 4 beat bar, fixed at placing at start or beat 2 pos for now
        //once set the touch will pass through to the region and allow setting AI states on it
        //eg. what chord type maj, 7th etc, note pattern ballad, arpeggio etc
        bool InputUpdate(MelodyEditorInterface.MEIState state)
        {
            if (state.input.LeftUp)
            {
                if (state.input.mRectangle.Intersects(mRect) && state.mbPlayAreaUpdate)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format(" HELD - - Track InputUpdate NoteNum: {0}", NoteNum));

                    AIChordRegion r = TouchInRegion(state.input.mRectangle);

                    if(r==null)
                    {
                        //Note UI
                        int pixelOffsetX = state.input.mRectangle.X - mStartX;
                        float drawPosBeatTouch = (float)pixelOffsetX / MelodyEditorInterface.BEAT_PIXEL_WIDTH;
                        float actualbeatTouch = drawPosBeatTouch + mParent.StartBeat; //correct for draw offset

                        float fCorrectToRangePlacePosOffset = actualbeatTouch % TWO_BEAT_REGION_RANGE;  //for now each new region will be 2 bars long and only place exactly at start of 4 bars or at middle of 4 bar
                        float fCorrectTo4BeatRangePlacePosOffset = actualbeatTouch % FOUR_BEAT_REGION_RANGE;  //Now can have a 4 beat bar as the last bar to copy, so need to know if we have touch place where we can place one
                        actualbeatTouch -= fCorrectToRangePlacePosOffset;

                        //Check if there was an old region here and resurrect and use that data instead of new one - TODO see how annoying this is
                        AIChordRegion.AIRegionSaveData rsd = GetAnySavedRegionHere(actualbeatTouch);

                        AIChordRegion possibleRegion = new AIChordRegion(this);

                        float barWidthToUse = FOUR_BEAT_REGION_RANGE; //No longer support this two bar example -> TWO_BEAT_REGION_RANGE;
                        float barStartToUse = actualbeatTouch;

                        if (rsd!=null)
                        {
                            possibleRegion.LoadSaveData(rsd);
                            barWidthToUse = possibleRegion.BeatLength;
                            barStartToUse = possibleRegion.BeatPos;
                            RemoveDeleteRegion(rsd);
                        }
                        else if(mLastRegion!=null)
                        {
                            if(mLastRegion.BeatLength== FOUR_BEAT_REGION_RANGE)
                            {
                                if (fCorrectToRangePlacePosOffset == fCorrectTo4BeatRangePlacePosOffset)
                                {
                                    barWidthToUse = FOUR_BEAT_REGION_RANGE;
                                    possibleRegion.CopyValuesFromRegion(mLastRegion);
                                }
                                //fall through and create new one as we cant put 4 beat one here like the last one
                            }
                            else
                            {
                                possibleRegion.CopyValuesFromRegion(mLastRegion);
                            }
                        }
                        mLastRegion = possibleRegion;
                        possibleRegion.SetPosLen(barStartToUse, barWidthToUse); //redundant if getting old/last data above?
                        mRegions.Add(possibleRegion);
                        return true;
                    }
                    //else
                    //{
                    //    r.Update(state);
                    //}
                }
            }

            return false;
        }

        public void Draw(SpriteBatch sb)
        {
            if (mbIsDrumChannel)
            {
                return;
            }

            sb.FillRectangle(mRect, Color.DarkBlue );
 
            foreach (AIChordRegion r in mRegions)
            {
                r.Draw(sb);
            }
        }
    }
}
