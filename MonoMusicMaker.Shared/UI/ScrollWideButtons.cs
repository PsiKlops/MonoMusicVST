using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Graphics;

namespace MonoMusicMaker
{
    //A grid of wide across screen buttons single column that scrolls up down
    //useful for loading files
    public class ScrollWideButtons : ButtonGrid
    {
        const int WIN_SCREEN_HEIGHT = 900;
        const int ANDROID_SCREEN_HEIGHT = 1080;

        const float MAX_UP_DOWN_RATE = 8f; //scroll up/down rate per time
        const float MIN_UP_DOWN_RATE = 3.5f; //scroll up/down rate per time when trigger start slowing into place
        const float ADJUST_UP_DOWN_RATE = 3f; //scroll up/down rate per time when slowing into place
        const float SLOW_DOWN_RATE = 1f;
        int mCurrentDeltaY = 0;
        float mfCurrentDeltaY = 0f;
        float mfSign = 1f; //positive
        bool mbScrolling = false;

        int mMaxScrollUp = 0;
        int mMaxScrollDowm = 0;
        int mCurrentYOffset = 0;

        const int DEFAULT_OFF_IGNORE_X = -1;
        public int mIgnoreTouchAfterX = DEFAULT_OFF_IGNORE_X; // if set this will free up area right hand side of screen so we can add buttons e.g. for load files scroll 

        public ScrollWideButtons(List<GridEntryData> entries) : base(entries, GRID_TYPE.UP_DOWN_GRID)
        {
            int lengthOffBottom = mGridHeight;
#if ANDOID
             int screenHeight=ANDROID_SCREEN_HEIGHT;
#else
            int screenHeight= WIN_SCREEN_HEIGHT;
#endif
            lengthOffBottom-=screenHeight;
            lengthOffBottom-=ButtonGrid.GRID_START_X;
            mMaxScrollUp = -(lengthOffBottom + screenHeight / 2);
            mMaxScrollDowm = (screenHeight / 2);


        }

        public void ResetScroll()
        {
            mbScrolling = false;
            mCurrentDeltaY = 0;
            mfCurrentDeltaY = mCurrentDeltaY;
        }

        public override Button Update(MelodyEditorInterface.MEIState state)
        {
            if (mExitButton != null)
            {
                if (mExitButton.Update(state))
                {
                    ResetScroll();
                    return mExitButton;
                }
                if (mExitButton.Held)
                {
                    return null;
                }
            }

            if (state.input.Held)
            {
                if(mIgnoreTouchAfterX!=DEFAULT_OFF_IGNORE_X && state.input.X >  mIgnoreTouchAfterX)
                {
                    ResetScroll();
                    return null;
                }
                mCurrentDeltaY = state.input.mDeltaY;
                mfCurrentDeltaY = Math.Abs(mCurrentDeltaY);
                if(mfCurrentDeltaY> MAX_UP_DOWN_RATE)
                {
                    mfCurrentDeltaY = MAX_UP_DOWN_RATE;
                }
                mfSign = 1;
                if(mCurrentDeltaY != 0)
                {
                    mfSign = (float)mCurrentDeltaY / mfCurrentDeltaY;
                }
            }
            else
            {
                if (mfCurrentDeltaY < MIN_UP_DOWN_RATE)
                {
                    //get nearest stop point
                    int butHeight = (Button.mHeight + ButtonGrid.GRID_GAP_Y);
                    int halfbutHeight = butHeight / 2;

                    int offset = mCurrentYOffset % butHeight;

                    if(Math.Abs(offset) < 4)
                    {
#if WINDOWS || ANDROID
                        Console.WriteLine("Scroll offset  {0} mfCurrentDeltaY {1}", offset, mfCurrentDeltaY);
#endif
                        ResetScroll();
                    }
                    else
                    {
#if WINDOWS || ANDROID
                        Console.WriteLine("ADJUST_UP_DOWN_RATE Scroll offset  {0} mfCurrentDeltaY {1}", offset, mfCurrentDeltaY);
#endif
                        mfCurrentDeltaY = ADJUST_UP_DOWN_RATE;
                        mbScrolling = true;
                        mCurrentDeltaY = (int)(mfCurrentDeltaY * mfSign);
                    }
                }
                else
                {
#if WINDOWS || ANDROID
                    Console.WriteLine("-= SLOW_DOWN_RATE; Scroll mfCurrentDeltaY {0}", mfCurrentDeltaY);
#endif
                    mbScrolling = true;
                    mfCurrentDeltaY -= SLOW_DOWN_RATE;
                    mCurrentDeltaY = (int)(mfCurrentDeltaY * mfSign);
                }
            }
#if WINDOWS || ANDROID
            Console.WriteLine("mCurrentYOffset {0} mCurrentDeltaY {1}", mCurrentYOffset, mCurrentDeltaY);
#endif
            mCurrentYOffset += mCurrentDeltaY;

            if(mCurrentYOffset <= mMaxScrollUp)
            {
                mCurrentYOffset = mMaxScrollUp;
            }
            if (mCurrentYOffset >= mMaxScrollDowm)
            {
                mCurrentYOffset = mMaxScrollDowm;
            }

            Vector2 vOffset = Vector2.Zero;
            vOffset.Y = mCurrentYOffset;

            Button returnButton = null;

            foreach (Button button in mButtons)
            {
                if (mExitButton != button)
                {
                    button.mOffsetPos = vOffset;
                    if (button.Update(state))
                    {
                        returnButton = button;
                    }
                }
            }

            return returnButton;
        }
        public override void Draw(SpriteBatch sb)
        {
            foreach (Button button in mButtons)
            {
                if (mExitButton != button)
                {
                    button.Draw(sb);
                }
            }
            if (mExitButton != null)
            {
                mExitButton.Draw(sb);
            }
        }
    }
}
