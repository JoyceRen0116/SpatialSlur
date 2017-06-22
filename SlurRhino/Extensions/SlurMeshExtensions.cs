﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using SpatialSlur.SlurCore;
using SpatialSlur.SlurMesh;
using Rhino.Geometry;

/*
 * Notes
 */

namespace SpatialSlur.SlurRhino
{
    using M = HeMesh<HeMesh3d.V, HeMesh3d.E, HeMesh3d.F>;
    using G = HeGraph<HeGraph3d.V, HeGraph3d.E>;


    /// <summary>
    /// 
    /// </summary>
    public static class SlurMeshExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="tolerance"></param>
        /// <param name="allowMultiEdges"></param>
        /// <param name="allowLoops"></param>
        /// <returns></returns>
        public static G ToHeGraph(this IReadOnlyList<Line> lines, double tolerance = 1.0e-8, bool allowMultiEdges = false, bool allowLoops = false)
        {
            return HeGraph3d.Factory.CreateFromLineSegments(lines, (v, p) => v.Position = p, tolerance, allowMultiEdges, allowLoops);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static G ToHeGraph(this Mesh mesh)
        {
            return HeGraph3d.Factory.CreateFromVertexTopology(mesh);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static M ToHeMesh(this IEnumerable<Polyline> polylines, double tolerance = 1.0e-8)
        {
            return HeMesh3d.Factory.CreateFromPolylines(polylines, (v, p) => v.Position = p, tolerance);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static M ToHeMesh(this Mesh mesh)
        {
            return HeMesh3d.Factory.CreateFromMesh(mesh);
        }


        #region IHeElement


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <param name="hedge"></param>
        /// <param name="getPosition"></param>
        /// <returns></returns>
        public static Line ToLine<TV, TE>(this IHalfedge<TV, TE> hedge, Func<TV, Vec3d> getPosition)
            where TV : IHeVertex<TV, TE>
            where TE : IHalfedge<TV, TE>
        {
            Vec3d p0 = getPosition(hedge.Start);
            Vec3d p1 = getPosition(hedge.End);
            return new Line(p0.X, p0.Y, p0.Z, p1.X, p1.Y, p1.Z);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <typeparam name="TF"></typeparam>
        /// <param name="face"></param>
        /// <param name="getPosition"></param>
        /// <returns></returns>
        public static Polyline ToPolyline<TV, TE, TF>(this IHeFace<TV, TE, TF> face, Func<TV, Vec3d> getPosition)
            where TV : IHeVertex<TV, TE, TF>
            where TE : IHalfedge<TV, TE, TF>
            where TF : IHeFace<TV, TE, TF>
        {
            Polyline result = new Polyline();

            foreach (var v in face.Vertices)
            {
                var p = getPosition(v);
                result.Add(p.X, p.Y, p.Z);
            }

            result.Add(result.First);
            return result;
        }


        /// <summary>
        /// Returns the circumcircle of a triangular face.
        /// Assumes the face is triangular.
        /// http://mathworld.wolfram.com/Incenter.html
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <typeparam name="TF"></typeparam>
        /// <param name="face"></param>
        /// <param name="getPosition"></param>
        /// <returns></returns>
        public static Circle GetCircumcircle<TV, TE, TF>(this IHeFace<TV, TE, TF> face, Func<TV, Vec3d> getPosition)
            where TV : IHeVertex<TV, TE, TF>
            where TE : IHalfedge<TV, TE, TF>
            where TF : IHeFace<TV, TE, TF>
        {
            var he = face.First;
            var p0 = getPosition(he.PrevInFace.Start);
            var p1 = getPosition(he.PrevInFace.Start);
            var p2 = getPosition(he.NextInFace.Start);
            return new Circle(p0.ToPoint3d(), p0.ToPoint3d(), p0.ToPoint3d());
        }


        /*
        /// <summary>
        /// Returns the incircle of a triangular face.
        /// Assumes face is triangular.
        /// http://mathworld.wolfram.com/Incenter.html
        /// </summary>
        /// <returns></returns>
        public static Circle GetIncircle(this HeFace face)
        {
            // TODO
            Vec3d p0 = _first.Previous.Start.Position;
            Vec3d p1 = _first.Start.Position;
            Vec3d p2 = _first.Next.Start.Position;

            double d01 = p0.DistanceTo(p1);
            double d12 = p1.DistanceTo(p2);
            double d20 = p2.DistanceTo(p0);

            double p = (d01 + d12 + d20) * 0.5; // semiperimeter
            double pInv = 1.0 / p; // inverse semiperimeter
            radius = Math.Sqrt(p * (p - d01) * (p - d12) * (p - d20)) * pInv; // triangle area (Heron's formula) / semiperimeter

            pInv *= 0.5; // inverse perimeter
            return p0 * (d12 * pInv) + p1 * (d20 * pInv) + p2 * (d01 * pInv);
        }
        */


        #endregion


        #region HeElementList
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hedges"></param>
        /// <param name="result"></param>
        /// <param name="parallel"></param>
        public static void GetEdgeLines<TV, TE>(this IHeStructure<TV, TE> structure, Func<TV, Vec3d> getPosition, Action<TE, Line> setResult, bool parallel = false)
            where TV : HeElement, IHeVertex<TV, TE>
            where TE : HeElement, IHalfedge<TV, TE>
        {
            var hedges = structure.Halfedges;

            Action<Tuple<int, int>> func = range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var he = hedges[i << 1];
                    if (he.IsRemoved) continue;
                    setResult(he, he.ToLine(getPosition));
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, hedges.Count >> 1), func);
            else
                func(Tuple.Create(0, hedges.Count >> 1));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="result"></param>
        /// <param name="parallel"></param>
        public static void GetFacePolylines<TV, TE, TF>(this IHeStructure<TV, TE, TF> structure, Func<TV, Vec3d> getPosition, Action<TF, Polyline> setResult, bool parallel = false)
            where TV : HeElement, IHeVertex<TV, TE, TF>
            where TE : HeElement, IHalfedge<TV, TE, TF>
            where TF : HeElement, IHeFace<TV, TE, TF>
        {
            var faces = structure.Faces;

            Action<Tuple<int, int>> func = range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var f = faces[i];
                    if (f.IsRemoved) continue;

                    setResult(f, f.ToPolyline<TV, TE, TF>(getPosition));
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, faces.Count), func);
            else
                func(Tuple.Create(0, faces.Count));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="result"></param>
        /// <param name="parallel"></param>
        public static void GetFaceCircumcircles<TV, TE, TF>(this IHeStructure<TV, TE, TF> structure, Func<TV, Vec3d> getPosition, Action<TF, Circle> setResult, bool parallel = false)
            where TV : HeElement, IHeVertex<TV, TE, TF>
            where TE : HeElement, IHalfedge<TV, TE, TF>
            where TF : HeElement, IHeFace<TV, TE, TF>
        {
            var faces = structure.Faces;

            Action<Tuple<int, int>> func = range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var f = faces[i];
                    if (f.IsRemoved) continue;
                    setResult(f, f.GetCircumcircle<TV, TE, TF>(getPosition));
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, faces.Count), func);
            else
                func(Tuple.Create(0, faces.Count));
        }


        #endregion


        #region IHeStructure


        /// <summary>
        /// 
        /// </summary>
        /// <param name="xform"></param>
        public static void Transform<TV, TE>(this IHeStructure<TV, TE> structure, Transform xform, bool parallel = false)
            where TV : HeElement, IHeVertex<TV, TE>, IVertex3d
            where TE : HeElement, IHalfedge<TV, TE>
        {
            var verts = structure.Vertices;

            Action<Tuple<int, int>> func = range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var v = verts[i];
                    v.Position = xform.Multiply(v.Position, true);
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, verts.Count), func);
            else
                func(Tuple.Create(0, verts.Count));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="xform"></param>
        public static void SpaceMorph<TV, TE>(this IHeStructure<TV, TE> structure, SpaceMorph xmorph, bool parallel = false)
            where TV : HeElement, IHeVertex<TV, TE>, IVertex3d
            where TE : HeElement, IHalfedge<TV, TE>
        {
            var verts = structure.Vertices;

            Action<Tuple<int, int>> func = range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var v = verts[i];
                    v.Position = xmorph.MorphPoint(v.Position);
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, verts.Count), func);
            else
                func(Tuple.Create(0, verts.Count));
        }


        #endregion

        
        #region HeMesh


        /// <summary>
        /// Note that unused and n-gon faces are skipped.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static Mesh ToMesh<TV, TE, TF>(this HeMesh<TV, TE, TF> mesh)
            where TV : HeVertex<TV, TE, TF>, IVertex3d
            where TE : Halfedge<TV, TE, TF>
            where TF : HeFace<TV, TE, TF>
        {
            return mesh.ToMesh(
                v => v.Position.ToPoint3f(),
                v => v.Normal.ToVector3f(),
                v => v.TexCoord.ToPoint2f(),
                null
                );
        }


        /// <summary>
        /// Note that unused and n-gon faces are skipped.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static Mesh ToMesh<TV, TE, TF>(this HeMesh<TV, TE, TF> mesh, Func<TV, Point3f> getPosition, Func<TV, Vector3f> getNormal, Func<TV, Point2f> getTexCoord, Func<TV, Color> getColor)
            where TV : HeVertex<TV, TE, TF>
            where TE : Halfedge<TV, TE, TF>
            where TF : HeFace<TV, TE, TF>
        {
            var verts = mesh.Vertices;
            var faces = mesh.Faces;

            Mesh result = new Mesh();
            var newVerts = result.Vertices;
            var newFaces = result.Faces;

            var newNorms = result.Normals;
            var newTexCoords = result.TextureCoordinates;
            var newColors = result.VertexColors;

            // add vertices
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                newVerts.Add(getPosition(v));

                if (getNormal != null) newNorms.Add(getNormal(v));
                if (getTexCoord != null) newTexCoords.Add(getTexCoord(v));
                if (getColor != null) newColors.Add(getColor(v));
            }

            // add faces
            foreach (var f in mesh.Faces)
            {
                if (f.IsRemoved) continue;
                var he = f.First;
                int degree = f.Degree;

                if (degree == 3)
                {
                    newFaces.AddFace(
                        he.Start.Index,
                        he.NextInFace.Start.Index,
                        he.PrevInFace.Start.Index
                        );
                }
                else if (degree == 4)
                {
                    newFaces.AddFace(
                        he.Start.Index,
                        he.NextInFace.Start.Index,
                        he.NextInFace.NextInFace.Start.Index,
                        he.PrevInFace.Start.Index
                        );
                }
                else
                {
                    // TODO support different triangulation schemes for n-gons
                }
            }

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <typeparam name="TF"></typeparam>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static Mesh ToPolySoup<TV, TE, TF>(this HeMesh<TV, TE, TF> mesh)
            where TV : HeVertex<TV, TE, TF>, IVertex3d
            where TE : Halfedge<TV, TE, TF>
            where TF : HeFace<TV, TE, TF>
        {
            return mesh.ToPolySoup(v => v.Position.ToPoint3f());
        }


        /// <summary>
        /// Vertices of the resulting mesh are not shared between faces.
        /// Note that unused and n-gon faces are skipped.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static Mesh ToPolySoup<TV, TE, TF>(this HeMesh<TV, TE, TF> mesh, Func<TV, Point3f> getPosition)
        where TV : HeVertex<TV, TE, TF>
        where TE : Halfedge<TV, TE, TF>
        where TF : HeFace<TV, TE, TF>
        {
            Mesh result = new Mesh();
            var newVerts = result.Vertices;
            var newFaces = result.Faces;

            // add vertices per face
            foreach (var f in mesh.Faces)
            {
                if (f.IsRemoved) continue;
                int nv = newVerts.Count;
                int degree = 0;

                // add face vertices
                foreach (var v in f.Vertices)
                {
                    newVerts.Add(getPosition(v));
                    degree++;
                }

                // add face(s)
                if (degree == 3)
                {
                    newFaces.AddFace(nv, nv + 1, nv + 2);
                }
                else if (degree == 4)
                {
                    newFaces.AddFace(nv, nv + 1, nv + 2, nv + 3);
                }
                else
                {
                    // TODO support different triangulation schemes for n-gons
                }
            }

            return result;
        }


        /*
        /// <summary>
        /// 
        /// </summary>
        /// <param name="xform"></param>
        public static void Transform(this M mesh, Transform xform, bool parallel = false)
        {
            var verts = mesh.Vertices;

            Action<Tuple<int, int>> func = range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var v = verts[i];
                    v.Position = xform.Multiply(v.Position, true);
                    v.Normal = xform.Multiply(v.Normal);
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, verts.Count), func);
            else
                func(Tuple.Create(0, verts.Count));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="xform"></param>
        public static void SpaceMorph(this M mesh, SpaceMorph xmorph, bool parallel = false)
        {
            var verts = mesh.Vertices;

            Action<Tuple<int, int>> func = range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    var v = verts[i];
                    v.Position = xmorph.MorphPoint(v.Position);
                    // v.Normal = xmorph.MorphPoint(v.Normal);
                }
            };

            if (parallel)
                Parallel.ForEach(Partitioner.Create(0, verts.Count), func);
            else
                func(Tuple.Create(0, verts.Count));
        }
        */


        #endregion


        #region HeGraphFactory


        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="lines"></param>
        /// <param name="tolerance"></param>
        /// <param name="allowMultiEdges"></param>
        /// <param name="allowLoops"></param>
        /// <returns></returns>
        public static HeGraph<TV, TE> CreateFromLineSegments<TV, TE>(this HeGraphFactory<TV, TE> factory, IReadOnlyList<Line> lines, double tolerance = 1.0e-8, bool allowMultiEdges = false, bool allowLoops = false)
            where TV : HeVertex<TV, TE>, IVertex3d
            where TE : Halfedge<TV, TE>
        {
            return factory.CreateFromLineSegments(lines, (v, p) => v.Position = p, tolerance, allowMultiEdges, allowLoops);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="lines"></param>
        /// <param name="tolerance"></param>
        /// <param name="allowMultiEdges"></param>
        /// <param name="allowLoops"></param>
        /// <returns></returns>
        public static HeGraph<TV, TE> CreateFromLineSegments<TV, TE>(this HeGraphFactory<TV, TE> factory, IReadOnlyList<Line> lines, Action<TV, Vec3d> setPosition, double tolerance = 1.0e-8, bool allowMultiEdges = false, bool allowLoops = false)
        where TV : HeVertex<TV, TE>
        where TE : Halfedge<TV, TE>
        {
            Vec3d[] endPoints = new Vec3d[lines.Count << 1];

            for (int i = 0; i < endPoints.Length; i += 2)
            {
                Line ln = lines[i >> 1];
                endPoints[i] = ln.From.ToVec3d();
                endPoints[i + 1] = ln.To.ToVec3d();
            }

            return factory.CreateFromLineSegments(endPoints, setPosition, tolerance, allowMultiEdges, allowLoops);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="dual"></param>
        /// <returns></returns>
        public static HeGraph<TV, TE> CreateFromVertexTopology<TV, TE>(this HeGraphFactory<TV, TE> factory, Mesh mesh)
            where TV : HeVertex<TV, TE>, IVertex3d
            where TE : Halfedge<TV, TE>
        {
            return factory.CreateFromVertexTopology( mesh, 
                (v, p) => v.Position = p.ToVec3d(), 
                (v, n) => v.Normal = n.ToVec3d(), 
                (v, t) => v.TexCoord = t.ToVec2d(), 
                delegate { }
                );
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="dual"></param>
        /// <returns></returns>
        public static HeGraph<TV, TE> CreateFromVertexTopology<TV, TE>(this HeGraphFactory<TV, TE> factory, Mesh mesh, Action<TV, Point3f> setPosition, Action<TV, Vector3f> setNormal, Action<TV, Point2f> setTexCoord, Action<TV, Color> setColor)
            where TV : HeVertex<TV, TE>
            where TE : Halfedge<TV, TE>
        {
            // TODO
            throw new NotImplementedException();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="dual"></param>
        /// <returns></returns>
        public static HeGraph<TV, TE> CreateFromFaceTopology<TV, TE>(this HeGraphFactory<TV, TE> factory, Mesh mesh)
            where TV : HeVertex<TV, TE>, IVertex3d
            where TE : Halfedge<TV, TE>
        {
            return factory.CreateFromFaceTopology(
                mesh,
                (v, p) => v.Position = p.ToVec3d(),
                (v, n) => v.Normal = n.ToVec3d(),
                (v, t) => v.TexCoord = t.ToVec2d(),
                delegate { }
                );
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="dual"></param>
        /// <returns></returns>
        public static HeGraph<TV, TE> CreateFromFaceTopology<TV, TE>(this HeGraphFactory<TV, TE> factory, Mesh mesh, Action<TV, Point3f> setPosition, Action<TV, Vector3f> setNormal, Action<TV, Point2f> setTexCoord, Action<TV, Color> setColor)
            where TV : HeVertex<TV, TE>
            where TE : Halfedge<TV, TE>
        {
            // TODO
            throw new NotImplementedException();
        }


        #endregion


        #region HeMeshFactory


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static HeMesh<TV, TE, TF> CreateFromMesh<TV, TE, TF>(this HeMeshFactory<TV, TE, TF> factory, Mesh mesh)
            where TV : HeVertex<TV, TE, TF>, IVertex3d
            where TE : Halfedge<TV, TE, TF>
            where TF : HeFace<TV, TE, TF>
        {
            return factory.CreateFromMesh(mesh,
                (v, p) => v.Position = p.ToVec3d(),
                (v, n) => v.Normal = n.ToVec3d(),
                (v, t) => v.TexCoord = t.ToVec2d(),
                delegate { });
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static HeMesh<TV,TE,TF> CreateFromMesh<TV,TE,TF>(this HeMeshFactory<TV, TE, TF> factory, Mesh mesh, Action<TV, Point3f> setPosition, Action<TV, Vector3f> setNormal, Action<TV, Point2f> setTexCoord, Action<TV, Color> setColor)
            where TV : HeVertex<TV, TE, TF>
            where TE : Halfedge<TV, TE, TF>
            where TF : HeFace<TV, TE, TF>
        {
            var verts = mesh.Vertices;
            var faces = mesh.Faces;
            var norms = mesh.Normals;
            var texCoords = mesh.TextureCoordinates;
            var colors = mesh.VertexColors;

            var result = factory.Create(verts.Count, verts.Count << 3, faces.Count);
            bool hasNorms = (norms.Count == verts.Count);
            bool hasTexCoords = (texCoords.Count == verts.Count);
            bool hasColors = (colors.Count == verts.Count);

            // add vertices
            for (int i = 0; i < verts.Count; i++)
            {
                var v = result.AddVertex();
                setPosition(v, verts[i]);

                if (hasNorms) setNormal (v, norms[i]);
                if (hasTexCoords) setTexCoord(v, texCoords[i]);
                if (hasColors) setColor(v, colors[i]);
            }

            // add faces
            for (int i = 0; i < faces.Count; i++)
            {
                MeshFace f = faces[i];
                if (f.IsQuad)
                    result.AddFace(f.A, f.B, f.C, f.D);
                else
                    result.AddFace(f.A, f.B, f.C);
            }

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static HeMesh<TV, TE, TF> CreateFromPolylines<TV, TE, TF>(this HeMeshFactory<TV, TE, TF> factory, IEnumerable<Polyline> polylines, double tolerance = 1.0e-8)
            where TV : HeVertex<TV, TE, TF>, IVertex3d
            where TE : Halfedge<TV, TE, TF>
            where TF : HeFace<TV, TE, TF>
        {
            return factory.CreateFromPolylines(polylines, (v, p) => v.Position = p, tolerance);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static HeMesh<TV, TE, TF> CreateFromPolylines<TV, TE, TF>(this HeMeshFactory<TV, TE, TF> factory, IEnumerable<Polyline> polylines, Action<TV, Vec3d> setPosition, double tolerance = 1.0e-8)
            where TV : HeVertex<TV, TE, TF>
            where TE : Halfedge<TV, TE, TF>
            where TF : HeFace<TV, TE, TF>
        {
            List<Vec3d> points = new List<Vec3d>();
            List<int> sizes = new List<int>();

            // get all polyline points
            foreach (Polyline p in polylines)
            {
                int n = p.Count - 1;
                if (!p.IsClosed || n < 3) continue;  // skip open or invalid loops

                // collect all points in the loop
                for (int i = 0; i < n; i++)
                    points.Add(p[i].ToVec3d());

                sizes.Add(n);
            }

            var vertPos = points.RemoveDuplicates(out int[] indexMap, tolerance);
            return factory.CreateFromFaceVertexData(vertPos, indexMap.Segment(sizes), setPosition);
        }


        #endregion
    }
}