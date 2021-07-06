using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MonoMusicMaker
{
    class SliderInput
    {
        public int mLengthRunner = 200;
        public int mWidthRunner = 10;
        public int mWidthSlider = 72;
        public int mThicknessSlider = 64;
        static int mSlideTolerance = 4;

        bool mbLeftRightOrientation = true; //other wise up/down
        public bool Visible { get; set; } = false;
        int mCurrentIntegerSelectedSliderPosition = 0;
        bool mbGrabbed = false;
        public bool Changed { get; set; } = false;

        int m_NumSelectPoints; //set this to return seected integer as a proportin of slider along length

        Vector2 mScreenPos;   //top left
        Vector2 mScreenPosSlider;   //top left

        Texture2D mSliderTexture;
        Texture2D mRunnerTexture;

        public SliderInput(Vector2 screenPos, int length, bool bLeftRight, int integerUnits, int initValue)
        {
            mCurrentIntegerSelectedSliderPosition = initValue;
            mScreenPosSlider = screenPos;

            if(bLeftRight)
            {
                mScreenPosSlider.Y = screenPos.Y - (int)((float)(mWidthSlider- mWidthRunner )/ 2f);
                mScreenPosSlider.X += length*(float)initValue/(float)integerUnits;
            }
            else
            {
                mScreenPosSlider.X = screenPos.X - (int)((float)(mWidthSlider - mWidthRunner)/ 2f);
                mScreenPosSlider.Y += length*(float)initValue / (float)integerUnits;
            }
            mScreenPos = screenPos;
            mLengthRunner = length;
            mbLeftRightOrientation = bLeftRight;
            m_NumSelectPoints = integerUnits;
        }

        public void SetSliderToNewValue(int newValue)
        {
            mCurrentIntegerSelectedSliderPosition = newValue;
            if (mbLeftRightOrientation)
            {
                mScreenPosSlider.Y = mScreenPos.Y - (int)((float)(mWidthSlider - mWidthRunner) / 2f);
                mScreenPosSlider.X = mScreenPos.X + mLengthRunner * (float)newValue / (float)m_NumSelectPoints;
            }
            else
            {
                mScreenPosSlider.X = mScreenPos.X - (int)((float)(mWidthSlider - mWidthRunner) / 2f);
                mScreenPosSlider.Y = mScreenPos.Y + mLengthRunner * (float)newValue / (float)m_NumSelectPoints;
            }
        }
        public int GetCurrentIntegerSelectedSliderPosition()
        {
            return mCurrentIntegerSelectedSliderPosition;
        }

        public void SetVisible(bool value)
        {
            Visible = value;
        }

        public void LoadContent(ContentManager contentMan)
        {
            mSliderTexture = contentMan.Load<Texture2D>("WhiteFilledCircle");
            mRunnerTexture = contentMan.Load<Texture2D>("PlainWhite"); ;
        }

        public bool Update(MelodyEditorInterface.MEIState state)
        {
            if (!Visible)
            {
                return false;
            }
            PsiKlopsInput input = state.input;

            bool bChangedSelection = false;
            if (input.LeftButton == ButtonState.Pressed)
            {
                if (InRange(new Vector2(input.X, input.Y)) || mbGrabbed)
                {
                    int oldIntegerSelectedSliderPosition = mCurrentIntegerSelectedSliderPosition;

                    mbGrabbed = true;
                    int newAbsoluteSlider = 0;
                    if (mbLeftRightOrientation)
                    {
                        int sliderPosition = (int)mScreenPosSlider.X;
                        int sliderBasePosition = (int)mScreenPos.X;

                        //if point over half way line nudge the poistion that way
                        int sliderMidPoint = sliderPosition + (int)((float)(mThicknessSlider) / 2f);
                        int difference = 0;

                        if (input.X> (sliderMidPoint + mSlideTolerance))
                        {
                            difference = input.X - (sliderMidPoint + mSlideTolerance);
                        }
                        else if (input.X < (sliderMidPoint - mSlideTolerance))
                        {
                            difference = input.X - (sliderMidPoint + mSlideTolerance);
                        }

                        newAbsoluteSlider = (sliderPosition + difference) - sliderBasePosition;
                        if (newAbsoluteSlider<0)
                        {
                            newAbsoluteSlider = 0;
                        }
                        else if(newAbsoluteSlider> mLengthRunner)
                        {
                            newAbsoluteSlider = mLengthRunner;
                        }
                        mScreenPosSlider.X = sliderBasePosition + newAbsoluteSlider;
                    }
                    else
                    {
                        int sliderPosition = (int)mScreenPosSlider.Y;
                        int sliderBasePosition = (int)mScreenPos.Y;

                        //if point over half way line nudge the poistion that way
                        int sliderMidPoint = sliderPosition + (int)((float)(mThicknessSlider) / 2f);
                        int difference = 0;

                        if (input.Y > (sliderMidPoint + mSlideTolerance))
                        {
                            difference = input.Y - (sliderMidPoint + mSlideTolerance);
                        }
                        else if (input.Y < (sliderMidPoint - mSlideTolerance))
                        {
                            difference = input.Y - (sliderMidPoint + mSlideTolerance);
                        }

                        newAbsoluteSlider = (sliderPosition + difference) - sliderBasePosition;
                        if (newAbsoluteSlider < 0)
                        {
                            newAbsoluteSlider = 0;
                        }
                        else if (newAbsoluteSlider > mLengthRunner)
                        {
                            newAbsoluteSlider = mLengthRunner;
                        }
                        mScreenPosSlider.Y = sliderBasePosition + newAbsoluteSlider;
                    }

                    mCurrentIntegerSelectedSliderPosition = (int)((float)m_NumSelectPoints * (float)newAbsoluteSlider / (float)mLengthRunner);

                    if(oldIntegerSelectedSliderPosition!=mCurrentIntegerSelectedSliderPosition)
                    {
                        bChangedSelection = true;
                    }

                }
            }
            Changed = false;
            if (input.LeftButton == ButtonState.Released)
            {
                Changed = true;
                mbGrabbed = false;
            }

            return bChangedSelection;  
        }

        bool InRange(Vector2 pos)
        {
            System.Diagnostics.Debug.Assert(mScreenPos != null);

            int startx = (int)mScreenPosSlider.X;
            int endx = (int)mScreenPosSlider.X + mWidthSlider;
            int starty = (int)mScreenPosSlider.Y;
            int endy = (int)mScreenPosSlider.Y + mThicknessSlider;

            if (mbLeftRightOrientation)
            {
                startx = (int)mScreenPosSlider.X;
                endx = (int)mScreenPosSlider.X + mThicknessSlider;
                starty = (int)mScreenPosSlider.Y;
                endy = (int)mScreenPosSlider.Y + mWidthSlider;
            }

            if (pos.X > startx &&
                 pos.X < endx &&
                 pos.Y > starty &&
                 pos.Y < endy)
            {
                return true;
            }

            return false;

        }

        public void Draw(SpriteBatch spriteBatch, Color? color = null)
        {
            if (!Visible)
            {
                return;
            }

            if (color == null)
            {
                color = Color.White;
            }

            Texture2D texture = mSliderTexture;

            int widthRunner = mWidthRunner;
            int heightRunner = mLengthRunner;
            int widthSlider = mWidthSlider;
            int heightSlider = mThicknessSlider;

            if (mbLeftRightOrientation)
            {
                widthRunner = mLengthRunner;
                heightRunner = mWidthRunner;
                widthSlider = mThicknessSlider;
                heightSlider = mWidthSlider ;
            }

            color = Color.Beige;
            spriteBatch.Begin();
            spriteBatch.Draw(mRunnerTexture,
                new Rectangle((int)mScreenPos.X, (int)mScreenPos.Y,
                    widthRunner,
                    heightRunner),
                color.Value);

            color = Color.Blue;
            spriteBatch.Draw(texture,
                new Rectangle((int)mScreenPosSlider.X, (int)mScreenPosSlider.Y,
                    widthSlider,
                    heightSlider),
                color.Value);

            spriteBatch.End();

        }
    }


}
