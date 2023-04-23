using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;
using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabolus.Features.Common {
    public static class MeshRefinement {
        public static DMesh3 Remesh(DMesh3 mesh, float edgeLength) {
            if (mesh == null) return mesh;

            //remesh the smooth if exists
            var reducer = new Reducer(mesh);

            //add constraint to boundries
            reducer.SetExternalConstraints(new MeshConstraints());
            MeshConstraintUtil.FixAllBoundaryEdges(reducer.Constraints, mesh);

            //ensure surface is along original mesh
            var tree = new DMeshAABBTree3(new DMesh3(mesh));
            tree.Build();
            reducer.SetProjectionTarget(new MeshProjectionTarget(tree.Mesh, tree));
            reducer.ProjectionMode = Reducer.TargetProjectionMode.Inline;

            reducer.ReduceToEdgeLength(edgeLength);
           
            return reducer.Mesh;
        }
    }
}
