﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpatialSlur.SlurCore;

/*
 * Notes
 */

namespace SpatialSlur.SlurDynamics.Constraints
{
    using H = ParticleHandle;

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Coincident : MultiConstraint<H>, IConstraint
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="weight"></param>
        public Coincident(double weight = 1.0, int capacity = DefaultCapacity)
            : base(weight, capacity)
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="weight"></param>
        public Coincident(IEnumerable<int> indices, double weight = 1.0, int capacity = DefaultCapacity)
            : base(weight, capacity)
        {
            Handles.AddRange(indices.Select(i => new H(i)));
        }


        /// <summary>
        /// 
        /// </summary>
        public ConstraintType Type
        {
            get { return ConstraintType.Position; }
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodies"></param>
        public void Calculate(IReadOnlyList<IBody> bodies)
        {
            Vec3d mean = new Vec3d();

            foreach(var h in Handles)
                mean += bodies[h].Position;

            mean /= Handles.Count;

            foreach (var h in Handles)
                h.Delta = mean - bodies[h].Position;
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bodies"></param>
        public void Apply(IReadOnlyList<IBody> bodies)
        {
            foreach (var h in Handles)
                bodies[h].ApplyMove(h.Delta, Weight);
        }


        #region Explicit interface implementations

        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        IEnumerable<IHandle> IConstraint.Handles
        {
            get { return Handles; }
        }

        #endregion
    }
}
