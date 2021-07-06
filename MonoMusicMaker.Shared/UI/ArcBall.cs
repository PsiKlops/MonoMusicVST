using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace MonoMusicMaker
{
    public class ArcBall
    {
        private static float Epsilon = 1.0e-5f;

        Vector3 StVec; // Saved click vector
        Vector3 EnVec; // Saved drag vector
        float adjustWidth; // Mouse bounds width
        float adjustHeight; // Mouse bounds height

        public ArcBall(float NewWidth, float NewHeight)
        {
            StVec = new Vector3();
            EnVec = new Vector3();
            setBounds(NewWidth, NewHeight);
        }

        public void mapToSphere(Point point, ref Vector3 vector)
        {

            Vector2 tempPoint = new Vector2(point.X, point.Y);

            tempPoint.X = 1.0f - (tempPoint.X * this.adjustWidth);
            tempPoint.Y = (tempPoint.Y * this.adjustHeight) - 1.0f; ;

            float length = (tempPoint.X * tempPoint.X) + (tempPoint.Y * tempPoint.Y);
            System.Diagnostics.Debug.WriteLine(string.Format("length: {0}", length));

            //Log.d("length: ", String.valueOf(length));

            if (length > 1.0f)
            {
                float norm = (float)(1.0 / System.Math.Sqrt(length));
                vector.X = tempPoint.X * norm;
                vector.Y = tempPoint.Y * norm;
                vector.Z = 0.0f;
            }
            else
            {
                vector.X = tempPoint.X;
                vector.Y = tempPoint.Y;
                vector.Z = (float)System.Math.Sqrt(1.0f - length);
            }

        }

        public void setBounds(float NewWidth, float NewHeight)
        {
            System.Diagnostics.Debug.Assert((NewWidth > 1.0f) && (NewHeight > 1.0f));

            adjustWidth = 1.0f / ((NewWidth - 1.0f) * 0.5f);
            adjustHeight = 1.0f / ((NewHeight - 1.0f) * 0.5f);
        }

        public void click(Point NewPt)
        {
            mapToSphere(NewPt, ref this.StVec);

        }

        public void drag(Point NewPt, ref Quaternion NewRot)
        {

            this.mapToSphere(NewPt, ref EnVec);

            if (NewRot != null)
            {
                Vector3 Perp = new Vector3();

                Vector3.Cross(ref StVec, ref EnVec, out Perp);

                if (Perp.Length() > Epsilon)
                {
                    NewRot.X = Perp.X;
                    NewRot.Y = Perp.Y;
                    NewRot.Z = Perp.Z;
                    NewRot.W = Vector3.Dot(StVec, EnVec);
                }
                else
                {
                    NewRot.X = NewRot.Y = NewRot.Z = 0.0f;
                    NewRot.W = 1.0f;
                }
            }
        }
    }
}
