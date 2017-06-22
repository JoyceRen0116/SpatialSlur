﻿using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading.Tasks;
using SpatialSlur.SlurCore;

/*
 * Notes
 */

namespace SpatialSlur.SlurField
{
    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public class GridVectorField2d : GridField2d<Vec2d>
    { 
        #region Static

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="mapper"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static GridVectorField2d CreateFromImage(Bitmap bitmap, Func<Color, Vec2d> mapper, Domain2d domain)
        {
            int nx = bitmap.Width;
            int ny = bitmap.Height;

            var result = new GridVectorField2d(domain, nx, ny);
            FieldIO.ReadFromImage(result, bitmap, mapper);

            return result;
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="countX"></param>
        /// <param name="countY"></param>
        public GridVectorField2d(Domain2d domain, int countX, int countY)
            : base(domain, countX, countY)
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="countX"></param>
        /// <param name="countY"></param>
        /// <param name="sampleMode"></param>
        /// <param name="wrapMode"></param>
        public GridVectorField2d(Domain2d domain, int countX, int countY, SampleMode sampleMode, WrapMode wrapMode)
            : base(domain, countX, countY, sampleMode, wrapMode)
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="countX"></param>
        /// <param name="countY"></param>
        /// <param name="sampleMode"></param>
        /// <param name="wrapModeX"></param>
        /// <param name="wrapModeY"></param>
        public GridVectorField2d(Domain2d domain, int countX, int countY, SampleMode sampleMode, WrapMode wrapModeX, WrapMode wrapModeY)
            : base(domain, countX, countY, sampleMode, wrapModeX, wrapModeY)
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        public GridVectorField2d(Grid2d other)
            : base(other)
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public GridVectorField2d Duplicate()
        {
            var copy = new GridVectorField2d(this);
            copy.Set(this);
            return copy;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override GridField2d<Vec2d> DuplicateBase()
        {
            return Duplicate();
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected override Vec2d ValueAtLinear(Vec2d point)
        {
            (double u, double v) = Fract(point, out int i0, out int j0);

            int i1 = WrapX(i0 + 1);
            int j1 = WrapY(j0 + 1) * CountX;

            i0 = WrapX(i0);
            j0 = WrapY(j0) * CountX;

            var vals = Values;
            return Vec2d.Lerp(
                Vec2d.Lerp(vals[i0 + j0], vals[i1 + j0], u),
                Vec2d.Lerp(vals[i0 + j1], vals[i1 + j1], u),
                v);
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected override Vec2d ValueAtLinearUnchecked(Vec2d point)
        {
            (double u, double v) = Fract(point, out int i0, out int j0);

            j0 *= CountX;
            int i1 = i0 + 1;
            int j1 = j0 + CountX;

            var vals = Values;
            return Vec2d.Lerp(
                Vec2d.Lerp(vals[i0 + j0], vals[i1 + j0], u),
                Vec2d.Lerp(vals[i0 + j1], vals[i1 + j1], u),
                v);
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override Vec2d ValueAt(GridPoint2d point)
        {
            return Values.ValueAt(point.Corners, point.Weights);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="value"></param>
        public void SetAt(GridPoint2d point, Vec2d value)
        {
            Values.SetAt(point.Corners, point.Weights, value);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="amount"></param>
        public void IncrementAt(GridPoint2d point, Vec2d amount)
        {
            Values.IncrementAt(point.Corners, point.Weights, amount);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallel"></param>
        /// <returns></returns>
        public GridVectorField2d GetLaplacian(bool parallel = false)
        {
            GridVectorField2d result = new GridVectorField2d(this);
            GetLaplacian(result.Values, parallel);
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="parallel"></param>
        public void GetLaplacian(GridVectorField2d result, bool parallel = false)
        {
            GetLaplacian(result.Values, parallel);
        }


        /// <summary>
        /// 
        /// </summary>
        public void GetLaplacian(Vec2d[] result, bool parallel)
        {
            var vals = Values;
            int nx = CountX;
            int ny = CountY;

            double dx = 1.0 / (ScaleX * ScaleX);
            double dy = 1.0 / (ScaleY * ScaleY);

            (int di, int dj) = FieldUtil.GetBoundaryOffsets(this);

            Action<Tuple<int, int>> func = range =>
            {
                (int i, int j) = IndicesAt(range.Item1);

                for (int index = range.Item1; index < range.Item2; index++, i++)
                {
                    if (i == nx) { j++; i = 0; }

                    Vec2d tx0 = (i == 0) ? vals[index + di] : vals[index - 1];
                    Vec2d tx1 = (i == nx - 1) ? vals[index - di] : vals[index + 1];

                    Vec2d ty0 = (j == 0) ? vals[index + dj] : vals[index - nx];
                    Vec2d ty1 = (j == CountY - 1) ? vals[index - dj] : vals[index + nx];

                    Vec2d t = vals[index] * 2.0;
                    result[index] = (tx0 + tx1 - t) * dx + (ty0 + ty1 - t) * dy;
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, Count), func);
            else
                func(Tuple.Create(0, Count));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallel"></param>
        /// <returns></returns>
        public GridScalarField2d GetDivergence(bool parallel = false)
        {
            GridScalarField2d result = new GridScalarField2d(this);
            GetDivergence(result.Values, parallel);
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="parallel"></param>
        public void GetDivergence(GridScalarField2d result, bool parallel = false)
        {
            GetDivergence(result.Values, parallel);
        }


        /// <summary>
        /// 
        /// </summary>
        public void GetDivergence(double[] result, bool parallel)
        {
            var vals = Values;
            int nx = CountX;
            int ny = CountY;

            double dx = 1.0 / (2.0 * ScaleX);
            double dy = 1.0 / (2.0 * ScaleY);

            (int di, int dj) = FieldUtil.GetBoundaryOffsets(this);

            Action<Tuple<int, int>> func = range =>
            {
                (int i, int j) = IndicesAt(range.Item1);

                for (int index = range.Item1; index < range.Item2; index++, i++)
                {
                    if (i == nx) { j++; i = 0; }

                    Vec2d tx0 = (i == 0) ? vals[index + di] : vals[index - 1];
                    Vec2d tx1 = (i == nx - 1) ? vals[index - di] : vals[index + 1];

                    Vec2d ty0 = (j == 0) ? vals[index + dj] : vals[index - nx];
                    Vec2d ty1 = (j == ny - 1) ? vals[index - dj] : vals[index + nx];

                    result[index] = (tx1.X - tx0.X) * dx + (ty1.Y - ty0.Y) * dy;
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, Count), func);
            else
                func(Tuple.Create(0, Count));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="parallel"></param>
        /// <returns></returns>
        public GridScalarField2d GetCurl(bool parallel = false)
        {
            GridScalarField2d result = new GridScalarField2d((Grid2d)this);
            GetCurl(result.Values, parallel);
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="parallel"></param>
        public void GetCurl(GridScalarField2d result, bool parallel = false)
        {
           GetCurl(result.Values, parallel);
        }


        /// <summary>
        /// 
        /// </summary>
        public void GetCurl(double[] result, bool parallel)
        {
            var vals = Values;
            int nx = CountX;
            int ny = CountY;

            double dx = 1.0 / (2.0 * ScaleX);
            double dy = 1.0 / (2.0 * ScaleY);

            (int di, int dj) = FieldUtil.GetBoundaryOffsets(this);

            Action<Tuple<int, int>> func = range =>
            {
                (int i, int j) = IndicesAt(range.Item1);

                for (int index = range.Item1; index < range.Item2; index++, i++)
                {
                    if (i == nx) { j++; i = 0; }

                    Vec2d tx0 = (i == 0) ? vals[index + di] : vals[index - 1];
                    Vec2d tx1 = (i == nx - 1) ? vals[index - di] : vals[index + 1];

                    Vec2d ty0 = (j == 0) ? vals[index + dj] : vals[index - nx];
                    Vec2d ty1 = (j == ny - 1) ? vals[index - dj] : vals[index + nx];

                    result[index] = (tx1.Y - tx0.Y) * dx - (ty1.X - ty0.X) * dy;
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, Count), func);
            else
                func(Tuple.Create(0, Count));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("VectorField2d ({0} x {1})", CountX, CountY);
        }
    }
}