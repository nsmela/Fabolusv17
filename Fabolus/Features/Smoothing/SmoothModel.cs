﻿using Fabolus.Features.Common;
using g3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HelixToolkit.Wpf;
using CommunityToolkit.Mvvm.Messaging;
using Fabolus.Features.Bolus;

namespace Fabolus.Features.Smoothing {
    public abstract class SmoothModel {
            public virtual string Name { get; set; }
            public abstract void Initialize(DMesh3 mesh);
            public abstract DMesh3 ToMesh();
    
    }

    public class PoissonSmoothModel : SmoothModel {
        private static string BASE_DIRECTORY = AppDomain.CurrentDomain.BaseDirectory + @"\Files\";
        private static string TEMP_FOLDER = BASE_DIRECTORY + @"\temp\";
        private static string RECONSTRUCTOR_FILE_PATH = BASE_DIRECTORY + @"PoissonRecon.exe";

        public int Depth { get; set; }
        public float Scale { get; set; }
        public int SamplesPerNode { get; set; }
        public float EdgeLength { get; set; }

        public override void Initialize(DMesh3 mesh) {
            //create output file
            //prevents multiple calls to the same thing
            //preemptively save the bolus as a temp file to save time smoothing with poisson

            //check the poisson reconstructor exists
            if (!File.Exists(RECONSTRUCTOR_FILE_PATH)) {
                MessageBox.Show(string.Format("Poisson Reconstructor at {0} was not found!", RECONSTRUCTOR_FILE_PATH), "Developer");
                return;
            }

            //create temp folder where exe is located
            Directory.CreateDirectory(TEMP_FOLDER);

            string exportFile = TEMP_FOLDER + @"temp.ply";
            SaveDMeshToPLYFile(mesh, exportFile);

            if (!File.Exists(exportFile)) {
                MessageBox.Show(string.Format("Failed to write temp ply file at {0}!", exportFile), "Developer");
                return;
            }
        }

        public override DMesh3 ToMesh() {
            //run poisson reconstructor
            ExecutePoisson(TEMP_FOLDER + @"temp.ply", TEMP_FOLDER + @"temp_smooth", Depth, Scale, SamplesPerNode);

            //load new mesh from ply in folder
            var result = ReadPLYFileToDMesh(TEMP_FOLDER + @"temp_smooth.ply");


            //reduce the mesh size 
            // MeshRefinement.Remesh(result, EdgeLength); //for some reason, this is creating more triangles than positions
            //cull the excess 

            return result; 
        }

        private static void ExecutePoisson(string inputFile, string outputFile, int depth, float scale, int samples) {
            if (File.Exists(inputFile)) {
                //Use ProcessStartInfo class
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = RECONSTRUCTOR_FILE_PATH;
                startInfo.Arguments = string.Format(@" --in ""{0}"" --out ""{1}"" --depth {2} --scale {3} --samplesPerNode {4}",
                    inputFile, //in
                    outputFile, //out
                    depth.ToString(), //depth
                    scale.ToString(), //scale
                    samples.ToString()); //samples

                //send the command
                try {
                    using (Process exeProcess = Process.Start(startInfo)) {
                        exeProcess.WaitForExit();
                    }

                } catch (Exception ex) {
                    MessageBox.Show("Recon failed! " + ex.Message);
                }
            }
        }

        //---------------------------------------------------------------------------------------------
        /// <summary>
        /// This method saves the given DMesh3 to the given file in the PLY format
        /// Calculates vertex normals required for poisson reconstruction
        /// </summary>
        /// <param name="mesh">Trianglemesh to export</param>
        /// <param name="outputFileName">Name of the file to write.</param>
        //---------------------------------------------------------------------------------------------
        public static void SaveDMeshToPLYFile(DMesh3 mesh, string outputFileName) {
            if (mesh == null)
                return;

            MeshNormals.QuickCompute(mesh);

            if (File.Exists(outputFileName)) {
                File.SetAttributes(outputFileName, FileAttributes.Normal);
                File.Delete(outputFileName);
            }

            using (TextWriter writer = new StreamWriter(outputFileName)) {
                writer.WriteLine("ply");
                writer.WriteLine("format ascii 1.0");
                writer.WriteLine("element vertex " + mesh.VertexCount);

                writer.WriteLine("property float x");
                writer.WriteLine("property float y");
                writer.WriteLine("property float z");
                writer.WriteLine("property float nx");
                writer.WriteLine("property float ny");
                writer.WriteLine("property float nz");

                writer.WriteLine("element face " + mesh.TriangleCount);

                writer.WriteLine("property list uchar int vertex_indices");

                writer.WriteLine("end_header");

                for (int v = 0; v < mesh.VertexCount; v++) {
                    Vector3d normal = mesh.GetVertexNormal(v);

                    writer.Write(mesh.GetVertex(v).x.ToString("e") + " ");
                    writer.Write(mesh.GetVertex(v).y.ToString("e") + " ");
                    writer.Write(mesh.GetVertex(v).z.ToString("e") + " ");
                    writer.Write(normal.x.ToString("e") + " ");
                    writer.Write(normal.y.ToString("e") + " ");
                    writer.Write(normal.z.ToString("e"));

                    writer.WriteLine();
                }

                int i = 0;
                while (i < mesh.TriangleCount) {
                    var triangle = mesh.GetTriangle(i);

                    writer.Write("3 ");
                    writer.Write(triangle.a + " ");
                    writer.Write(triangle.b + " ");
                    writer.Write(triangle.c + " ");
                    writer.WriteLine();
                    i++;
                }
            }
        }

        private static DMesh3 ReadPLYFileToDMesh(string filepath) {
            //verify file exists
            if (File.Exists(filepath)) {
                List<string> headers = new List<string>();

                bool endheader = false;
                using (BinaryReader b = new BinaryReader(File.Open(filepath, FileMode.Open))) {
                    //reads the header
                    while (!endheader) {
                        string line = ReadReturnTerminatedString(b);
                        headers.Add(line);
                        if (line == "end_header") {
                            endheader = true;
                        }
                    }

                    //determining the vertexes and faces
                    int vertexRef = headers.FindIndex(element => element.StartsWith("element vertex", StringComparison.Ordinal));
                    string text = headers[vertexRef].Substring(headers[vertexRef].LastIndexOf(' ') + 1);
                    int number_of_vertexes = Convert.ToInt32(text);

                    int faceRef = headers.FindIndex(element => element.StartsWith("element face", StringComparison.Ordinal));
                    text = headers[faceRef].Substring(headers[faceRef].LastIndexOf(' ') + 1);
                    int number_of_faces = Convert.ToInt32(text);

                    //read the vertexes
                    DMesh3 mesh = new DMesh3(true); //want normals
                    for (int i = 0; i < number_of_vertexes; i++) {
                        float x, y, z;
                        x = b.ReadSingle();
                        y = b.ReadSingle();
                        z = b.ReadSingle();

                        mesh.AppendVertex(new Vector3d(x, y, z));
                    }

                    //read the faces
                    for (int i = 0; i < number_of_faces; i++) {
                        b.ReadByte();//skips the first bye, always '3'
                        int v0 = b.ReadInt32();
                        int v1 = b.ReadInt32();
                        int v2 = b.ReadInt32();

                        mesh.AppendTriangle(v0, v1, v2);
                    }

                    return mesh;
                }

            } else {
                MessageBox.Show("The file " + filepath + " does not exist!");
            }
            return null;
        }

        private static string ReadReturnTerminatedString(BinaryReader stream) {
            string str = "";
            char ch;
            while ((ch = stream.ReadChar()) != 10)
                str = str + ch;
            return str;
        }
    }

}
