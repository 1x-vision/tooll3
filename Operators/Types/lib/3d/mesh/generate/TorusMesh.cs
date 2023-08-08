using System;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using T3.Core.Resource;
using T3.Core.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_a835ab86_29c1_438e_a7f7_2e297108bfd5
{
    public class TorusMesh : Instance<TorusMesh>
    {
        [Output(Guid = "f8f17f87-56f2-4411-b9bf-b9193b9aa90d")]
        public readonly Slot<MeshBuffers> Data = new Slot<MeshBuffers>();

        public TorusMesh()
        {
            Data.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            try { var resourceManager = ResourceManager.Instance(); var majorRadius = Radius.GetValue(context); var tubeRadius = Thickness.GetValue(context); var segments = Segments.GetValue(context); var radiusSegments = segments.Width.Clamp(1, 10000) + 1; var tubeSegments = segments.Height.Clamp(1, 10000) + 1;

                var spin = Spin.GetValue(context);
                var radiusSpin = spin.X * MathUtils.ToRad;
                var spinMinorInRad = spin.Y * MathUtils.ToRad;

                var fill = Fill.GetValue(context);
                var fillRadius = fill.X / 360f;
                var tubeFill = fill.Y / 360f;

                var smoothAngle = SmoothAngle.GetValue(context);
                
                var useFlatShading = fillRadius / tubeSegments > smoothAngle / 360 || tubeFill / radiusSegments > smoothAngle / 360;
                var faceCount = (tubeSegments -1)  * (radiusSegments - 1) * 2;
                var verticesCount = tubeSegments * radiusSegments;

                // Create buffers
                if (_vertexBufferData.Length != verticesCount)
                    _vertexBufferData = new PbrVertex[verticesCount];
                
                if (_indexBufferData.Length != faceCount)
                    _indexBufferData = new SharpDX.Int3[faceCount];


                // Initialize
                var tubeAngleFraction = tubeFill / (tubeSegments -1) * 2.0 * Math.PI;
                var radiusAngleFraction = fillRadius / (radiusSegments -1) * 2.0 * Math.PI;

                for (int tubeIndex = 0; tubeIndex < tubeSegments; ++tubeIndex)
                {
                    var tubeAngle = tubeIndex * tubeAngleFraction + spinMinorInRad;
                    
                    double tubePosition1X = Math.Sin(tubeAngle)*tubeRadius;
                    double tubePosition1Y = Math.Cos(tubeAngle)*tubeRadius;
                    double tubePosition2X = Math.Sin(tubeAngle+ tubeAngleFraction)*tubeRadius;
                    double tubePosition2Y = Math.Cos(tubeAngle+ tubeAngleFraction)*tubeRadius;

                    var v0 = tubeIndex / (float)(tubeSegments-1);
                    var v1 = (tubeIndex + 1) / (float)(tubeSegments-1);

                    for (int radiusIndex = 0; radiusIndex < radiusSegments; ++radiusIndex)
                    {
                        var vertexIndex = radiusIndex + tubeIndex * radiusSegments;
                        var faceIndex =  2 * (radiusIndex + tubeIndex * (radiusSegments-1));
                        
                        var u0 = (radiusIndex ) / (float)(radiusSegments-1);
                        var u1 = (radiusIndex +1)/ (float)(radiusSegments-1);

                        var radiusAngle = radiusIndex * radiusAngleFraction + radiusSpin;

                        var p = new Vector3((float)(Math.Sin(radiusAngle) * (tubePosition1X + majorRadius)),
                                                    (float)(Math.Cos(radiusAngle) * (tubePosition1X + majorRadius)), 
                                                    (float)tubePosition1Y);
                        
                        var p1 = new Vector3((float)(Math.Sin(radiusAngle + radiusAngleFraction) * (tubePosition1X + majorRadius)),
                                                     (float)(Math.Cos(radiusAngle + radiusAngleFraction) * (tubePosition1X + majorRadius)), 
                                                     (float)tubePosition1Y);
                        
                        var p2 = new Vector3((float)(Math.Sin(radiusAngle) * (tubePosition2X + majorRadius)),
                                                     (float)(Math.Cos(radiusAngle) * (tubePosition2X + majorRadius)), 
                                                     (float)tubePosition2Y);
                        
                        var uv0 = new Vector2(u0, v1);
                        var uv1 = new Vector2(u1, v1);
                        var uv2 = new Vector2(u1, v0);

                        var tubeCenter1 = new Vector3((float)Math.Sin(radiusAngle), (float)Math.Cos(radiusAngle), 0.0f) * majorRadius;
                        var normal0 = Vector3.Normalize(useFlatShading 
                                                                    ? Vector3.Cross(p - p1, p - p2) 
                                                                    : p - tubeCenter1);
                        
                        MeshUtils.CalcTBNSpace(p, uv0, p1, uv1, p2, uv2, normal0, out var tangent0, out var binormal0);

                        _vertexBufferData[vertexIndex + 0] = new PbrVertex
                                                            {
                                                                Position = p,
                                                                Normal = normal0,
                                                                Tangent = tangent0,
                                                                Bitangent = binormal0,
                                                                Texcoord = uv0,
                                                                Selection =1,
                                                            };

                        if (tubeIndex >= tubeSegments - 1 || radiusIndex >= radiusSegments - 1)
                            continue;
                        
                        _indexBufferData[faceIndex + 0] = new SharpDX.Int3(vertexIndex + 0, vertexIndex + 1, vertexIndex + radiusSegments);
                        _indexBufferData[faceIndex + 1] = new SharpDX.Int3(vertexIndex + radiusSegments , vertexIndex + 1, vertexIndex + radiusSegments+1);
                    }
                }
                
                // Write Data
                _vertexBufferWithViews.Buffer = _vertexBuffer;
                ResourceManager.SetupStructuredBuffer(_vertexBufferData, PbrVertex.Stride * verticesCount, PbrVertex.Stride, ref _vertexBuffer);
                ResourceManager.CreateStructuredBufferSrv(_vertexBuffer, ref _vertexBufferWithViews.Srv);
                ResourceManager.CreateStructuredBufferUav(_vertexBuffer, UnorderedAccessViewBufferFlags.None, ref _vertexBufferWithViews.Uav);
                
                _indexBufferWithViews.Buffer = _indexBuffer;
                const int stride = 3 * 4;
                ResourceManager.SetupStructuredBuffer(_indexBufferData, stride * faceCount, stride, ref _indexBuffer);
                ResourceManager.CreateStructuredBufferSrv(_indexBuffer, ref _indexBufferWithViews.Srv);
                ResourceManager.CreateStructuredBufferUav(_indexBuffer, UnorderedAccessViewBufferFlags.None, ref _indexBufferWithViews.Uav);

                _data.VertexBuffer = _vertexBufferWithViews;
                _data.IndicesBuffer = _indexBufferWithViews;
                Data.Value = _data;
                Data.DirtyFlag.Clear();
            }
            catch (Exception e)
            {
                Log.Error("Failed to create torus mesh:" + e.Message);
            }
        }

        private Buffer _vertexBuffer;
        private PbrVertex[] _vertexBufferData = new PbrVertex[0];
        private readonly BufferWithViews _vertexBufferWithViews = new BufferWithViews();

        private Buffer _indexBuffer;
        private SharpDX.Int3[] _indexBufferData = new SharpDX.Int3[0];
        private readonly BufferWithViews _indexBufferWithViews = new BufferWithViews();

        private readonly MeshBuffers _data = new MeshBuffers();

        [Input(Guid = "608DE038-6C7A-43FC-BA89-374C7B1A318E")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "FDBAD44A-2504-453B-BFAE-976828372CC0")]
        public readonly InputSlot<float> Thickness = new InputSlot<float>();

        [Input(Guid = "99F5D952-8490-4930-B8AB-9D8E968183C6")]
        public readonly InputSlot<Size2> Segments = new InputSlot<Size2>();

        [Input(Guid = "770A164B-10E7-4145-B1C9-DAD1F564EC6B")]
        public readonly InputSlot<Vector2> Spin = new InputSlot<Vector2>();

        [Input(Guid = "F3E7341C-0C81-42AF-BA48-B43D345188C1")]
        public readonly InputSlot<Vector2> Fill = new InputSlot<Vector2>();

        // [Input(Guid = "1457EDC2-5F9B-4F72-9CB2-4CA40066F177")]
        // public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "2D083DC4-1576-4A44-8744-E0896424A6A9")]
        public readonly InputSlot<float> SmoothAngle = new InputSlot<float>();
    }
}