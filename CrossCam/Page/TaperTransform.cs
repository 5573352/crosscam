﻿using SkiaSharp;

namespace CrossCam.Page
{
    public enum TaperSide { Left, Top, Right, Bottom }

    public enum TaperCorner { LeftOrTop, RightOrBottom, Both }

    public static class TaperTransform
    {
        public static SKMatrix Make(SKSize size, TaperSide taperSide, TaperCorner taperCorner, float taperFraction)
        {
            var matrix = SKMatrix.MakeIdentity();

            switch (taperSide)
            {
                case TaperSide.Left:
                    matrix.ScaleX = taperFraction;
                    matrix.ScaleY = taperFraction;
                    matrix.Persp0 = (taperFraction - 1) / size.Width;

                    switch (taperCorner)
                    {
                        case TaperCorner.RightOrBottom:
                            break;

                        case TaperCorner.LeftOrTop:
                            matrix.SkewY = size.Height * matrix.Persp0;
                            matrix.TransY = size.Height * (1 - taperFraction);
                            break;

                        case TaperCorner.Both:
                            matrix.SkewY = (size.Height / 2) * matrix.Persp0;
                            matrix.TransY = size.Height * (1 - taperFraction) / 2;
                            break;
                    }
                    break;

                case TaperSide.Top:
                    matrix.ScaleX = taperFraction;
                    matrix.ScaleY = taperFraction;
                    matrix.Persp1 = (taperFraction - 1) / size.Height;

                    switch (taperCorner)
                    {
                        case TaperCorner.RightOrBottom:
                            break;

                        case TaperCorner.LeftOrTop:
                            matrix.SkewX = size.Width * matrix.Persp1;
                            matrix.TransX = size.Width * (1 - taperFraction);
                            break;

                        case TaperCorner.Both:
                            matrix.SkewX = (size.Width / 2) * matrix.Persp1;
                            matrix.TransX = size.Width * (1 - taperFraction) / 2;
                            break;
                    }
                    break;

                case TaperSide.Right:
                    matrix.ScaleX = 1 / taperFraction;
                    matrix.Persp0 = (1 - taperFraction) / (size.Width * taperFraction);

                    switch (taperCorner)
                    {
                        case TaperCorner.RightOrBottom:
                            break;

                        case TaperCorner.LeftOrTop:
                            matrix.SkewY = size.Height * matrix.Persp0;
                            break;

                        case TaperCorner.Both:
                            matrix.SkewY = (size.Height / 2) * matrix.Persp0;
                            break;
                    }
                    break;

                case TaperSide.Bottom:
                    matrix.ScaleY = 1 / taperFraction;
                    matrix.Persp1 = (1 - taperFraction) / (size.Height * taperFraction);

                    switch (taperCorner)
                    {
                        case TaperCorner.RightOrBottom:
                            break;

                        case TaperCorner.LeftOrTop:
                            matrix.SkewX = size.Width * matrix.Persp1;
                            break;

                        case TaperCorner.Both:
                            matrix.SkewX = (size.Width / 2) * matrix.Persp1;
                            break;
                    }
                    break;
            }
            return matrix;
        }
    }
}