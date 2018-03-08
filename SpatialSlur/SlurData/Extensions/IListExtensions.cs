﻿
/*
 * Notes
 */

using System.Collections.Generic;
using SpatialSlur.SlurData;

namespace SpatialSlur.SlurCore
{
    /// <summary>
    /// 
    /// </summary>
    public static partial class IListExtensions
    {
        #region IList<Vec2d>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        /// <param name="tolerance"></param>
        /// <param name="maxSteps"></param>
        /// <returns></returns>
        public static bool Consolidate(this IList<Vec2d> points, double radius, double tolerance = SlurMath.ZeroTolerance, int maxSteps = 4)
        {
            return DataUtil.ConsolidatePoints(points, radius, tolerance, maxSteps);
        }

        #endregion


        #region IList<Vec3d>

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="radius"></param>
        /// <param name="tolerance"></param>
        /// <param name="maxSteps"></param>
        /// <returns></returns>
        public static bool Consolidate(this IList<Vec3d> points, double radius, double tolerance = SlurMath.ZeroTolerance, int maxSteps = 4)
        {
            return DataUtil.ConsolidatePoints(points, radius, tolerance, maxSteps);
        }

        #endregion
    }
}
