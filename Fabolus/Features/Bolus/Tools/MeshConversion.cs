﻿using g3;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace Fabolus.Features.Bolus
{
    public static partial class BolusUtility {

        public static DMesh3 MeshGeometryToDMesh(MeshGeometry3D mesh) {
            List<Vector3d> vertices = new();
            foreach (Point3D point in mesh.Positions)
                vertices.Add(new Vector3d(point.X, point.Y, point.Z));

            List<Vector3f> normals = new();
            foreach (Point3D normal in mesh.Normals)
                normals.Add(new Vector3f(normal.X, normal.Y, normal.Z));

            if (normals.Count == 0)
                normals = null;

            List<Index3i> triangles = new();
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
                triangles.Add(new Index3i(mesh.TriangleIndices[i], mesh.TriangleIndices[i + 1], mesh.TriangleIndices[i + 2]));

            //converting the meshes to use Implicit Surface Modeling
            return DMesh3Builder.Build(vertices, triangles, normals);
        }

        public static DMesh3 MeshGeometryToDMesh(MeshGeometry3D mesh, Matrix3D transform) {
            //applying the transform
            Point3DCollection positions = new();
            foreach (Point3D p in mesh.Positions)
                positions.Add(transform.Transform(p));

            List<Vector3d> vertices = new();
            foreach (Point3D point in positions)
                vertices.Add(new Vector3d(point.X, point.Y, point.Z));

            List<Vector3f> normals = new();
            foreach (Point3D normal in mesh.Normals)
                normals.Add(new Vector3f(normal.X, normal.Y, normal.Z));

            if (normals.Count == 0)
                normals = null;

            List<Index3i> triangles = new();
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
                triangles.Add(new Index3i(mesh.TriangleIndices[i], mesh.TriangleIndices[i + 1], mesh.TriangleIndices[i + 2]));

            //converting the meshes to use Implicit Surface Modeling
            return DMesh3Builder.Build(vertices, triangles, normals);
        }
    }
}

