using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using Buffer = SharpDX.Direct3D11.Buffer;
// ReSharper disable RedundantNameQualifier

namespace T3.Operators.Types.Id_be52b670_9749_4c0d_89f0_d8b101395227
{
    public class LoadObj : Instance<LoadObj>, IDescriptiveGraphNode
    {
        [Output(Guid = "1F4E7CAC-1F62-4633-B0F3-A3017A026753")]
        public readonly Slot<MeshBuffers> Data = new Slot<MeshBuffers>();

        public LoadObj()
        {
            Data.UpdateAction = Update;
        }

        private float _scaleFactor;
        
        private void Update(EvaluationContext context)
        {
            var path = Path.GetValue(context);
            var vertexSorting = SortVertices.GetValue(context);
            var useGpuCaching = UseGPUCaching.GetValue(context);
            var scaleFactor = ScaleFactor.GetValue(context);
            if (ClearGPUCache.GetValue(context))
            {
                _meshBufferCache.Clear();
            }
            
            if (path != _lastFilePath || SortVertices.DirtyFlag.IsDirty || Math.Abs(scaleFactor - _scaleFactor) > 0.001f)
            {
                _scaleFactor = scaleFactor;
                _description = System.IO.Path.GetFileName(path);

                if (useGpuCaching)
                {
                    if (_meshBufferCache.TryGetValue(path, out var cachedBuffer))
                    {
                        Data.Value = cachedBuffer.DataBuffers;
                        return;
                    }
                }

                var mesh = ObjMesh.LoadFromFile(path);
                if (mesh == null || mesh.DistinctDistinctVertices.Count == 0)
                {
                    Log.Warning($"Can't read file {path}");
                    return;
                }

                QueryPresenceOfMeshUVs(mesh);

                mesh.UpdateVertexSorting((ObjMesh.SortDirections)vertexSorting);

                _lastFilePath = path;

                var resourceManager = ResourceManager.Instance();

                var newData = new DataSet();

                var reversedLookup = new int[mesh.DistinctDistinctVertices.Count];

                // Create Vertex buffer
                {
                    var verticesCount = mesh.DistinctDistinctVertices.Count;
                    if (newData.VertexBufferData.Length != verticesCount)
                        newData.VertexBufferData = new PbrVertex[verticesCount];

                    for (var vertexIndex = 0; vertexIndex < verticesCount; vertexIndex++)
                    {
                        var sortedVertexIndex = mesh.SortedVertexIndices[vertexIndex];
                        var sortedVertex = mesh.DistinctDistinctVertices[sortedVertexIndex];
                        reversedLookup[sortedVertexIndex] = vertexIndex;
                        newData.VertexBufferData[vertexIndex] = new PbrVertex
                        {
                                                                Position = mesh.Positions[sortedVertex.PositionIndex] * scaleFactor,
                                                                Normal = mesh.Normals[sortedVertex.NormalIndex],
                                                                Tangent = mesh.VertexTangents[sortedVertexIndex],
                                                                Bitangent = mesh.VertexBinormals[sortedVertexIndex],
                                                                Texcoord = mesh.TexCoords[sortedVertex.TextureCoordsIndex],
                                                                Selection = 1,
                        };
                    }

                    newData.VertexBufferWithViews.Buffer = newData.VertexBuffer;
                    resourceManager.SetupStructuredBuffer(newData.VertexBufferData, PbrVertex.Stride * verticesCount, PbrVertex.Stride,
                                                          ref newData.VertexBuffer);
                    resourceManager.CreateStructuredBufferSrv(newData.VertexBuffer, ref newData.VertexBufferWithViews.Srv);
                    resourceManager.CreateStructuredBufferUav(newData.VertexBuffer, UnorderedAccessViewBufferFlags.None, ref newData.VertexBufferWithViews.Uav);
                }

                // Create Index buffer
                {
                    var faceCount = mesh.Faces.Count;
                    if (newData.IndexBufferData.Length != faceCount)
                        newData.IndexBufferData = new SharpDX.Int3[faceCount];

                    for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
                    {
                        var face = mesh.Faces[faceIndex];
                        var v1Index = mesh.GetVertexIndex(face.V0, face.V0n, face.V0t);
                        var v2Index = mesh.GetVertexIndex(face.V1, face.V1n, face.V1t);
                        var v3Index = mesh.GetVertexIndex(face.V2, face.V2n, face.V2t);

                        newData.IndexBufferData[faceIndex]
                            = new SharpDX.Int3(reversedLookup[v1Index],
                                               reversedLookup[v2Index],
                                               reversedLookup[v3Index]);
                    }

                    newData.IndexBufferWithViews.Buffer = newData.IndexBuffer;
                    const int stride = 3 * 4;
                    resourceManager.SetupStructuredBuffer(newData.IndexBufferData, stride * faceCount, stride, ref newData.IndexBuffer);
                    resourceManager.CreateStructuredBufferSrv(newData.IndexBuffer, ref newData.IndexBufferWithViews.Srv);
                    resourceManager.CreateStructuredBufferUav(newData.IndexBuffer, UnorderedAccessViewBufferFlags.None, ref newData.IndexBufferWithViews.Uav);
                }

                if (useGpuCaching)
                {
                    _meshBufferCache[path] = newData;
                }

                _data = newData;
            }

            _data.DataBuffers.VertexBuffer = _data.VertexBufferWithViews;
            _data.DataBuffers.IndicesBuffer = _data.IndexBufferWithViews;
            Data.Value = _data.DataBuffers;
        }

        /// <summary>
        /// A method for logging warnings on the loading of <see cref="ObjMesh"/>
        /// </summary>
        private static void QueryPresenceOfMeshUVs(ObjMesh mesh)
        {
            const string baseMessage = "This prevents computation of Tangents and binormals and will cause black rendering and other issues with displacement as a result.";
            if (mesh.TexCoords.Count == 0)
            {
                Log.Warning($"No UVs are present in this {nameof(ObjMesh)}. {baseMessage}");
                return;
            }

            //if texture coordinates are not evenly divisible by number of vertices (assuming support or future support of multiple UV sets)
            //and if the reason that is true is that there are fewer texCoords than would be required, rather than more texCoords
            int modResult = mesh.TexCoords.Count % mesh.Positions.Count;
            if (modResult != 0 && mesh.Positions.Count * modResult < mesh.TexCoords.Count)
            {
                Log.Warning($"UVs of {nameof(ObjMesh)} are missing vertices. {baseMessage}\n" +
                    $"Vertex count: {mesh.Positions.Count}, UV count {mesh.TexCoords.Count}");
            }
        }

        public string GetDescriptiveString()
        {
            return _description;
        }

        private string _description;
        private string _lastFilePath;
        private DataSet _data = new DataSet();

        private class DataSet
        {
            public readonly MeshBuffers DataBuffers = new();

            public Buffer VertexBuffer;
            public PbrVertex[] VertexBufferData = Array.Empty<PbrVertex>();
            public readonly BufferWithViews VertexBufferWithViews = new();

            public Buffer IndexBuffer;
            public SharpDX.Int3[] IndexBufferData = Array.Empty<Int3>();
            public readonly BufferWithViews IndexBufferWithViews = new();
        }

        private static readonly Dictionary<string, DataSet> _meshBufferCache = new();

        [Input(Guid = "7d576017-89bd-4813-bc9b-70214efe6a27")]
        public readonly InputSlot<string> Path = new();

        [Input(Guid = "FFD22736-A600-4C97-A4A4-AD3526B8B35C")]
        public readonly InputSlot<bool> UseGPUCaching = new();

        [Input(Guid = "DDD22736-A600-4C97-A4A4-AD3526B8B35C")]
        public readonly InputSlot<bool> ClearGPUCache = new();
        
        [Input(Guid = "AA19E71D-329C-448B-901C-565BF8C0DA4F", MappedType = typeof(ObjMesh.SortDirections))]
        public readonly InputSlot<int> SortVertices = new();
        
        [Input(Guid = "C39A61B3-FB6B-4611-8F13-273F13C9C491")]
        public readonly InputSlot<float> ScaleFactor = new();

    }
}