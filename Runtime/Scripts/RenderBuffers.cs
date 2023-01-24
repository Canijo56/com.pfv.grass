using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace PFV.Grass
{
    public class RenderBuffers : IDisposable
    {

        public enum DrawArgs
        {
            DrawArgs = 0,
            ShadowDrawArgs = 1
        }
        public enum ComputeArgs
        {
            GenerateBladesArgs = 0,
            InterpolateBladesArgs = 1,
        }

        private SharedBuffer<DrawIndirectArgs> _drawArgsBuffer;
        public SharedBuffer<DrawIndirectArgs> drawArgsBuffer => _drawArgsBuffer;

        private SharedBuffer<ComputeIndirectArgs> _computeArgsBuffer;
        public SharedBuffer<ComputeIndirectArgs> computeArgsBuffer => _computeArgsBuffer;

        private ComputeBuffer _interpolateDispatch;
        public ComputeBuffer interpolateDispatch => _interpolateDispatch;
        private ComputeBuffer _generateDispatch;
        public ComputeBuffer generateDispatch => _generateDispatch;
        private ComputeBuffer _auxInt;
        public ComputeBuffer auxInt => _auxInt;
        private ComputeBuffer _bladeSourceData;
        public ComputeBuffer bladeSourceData => _bladeSourceData;

        private ComputeBuffer _trianglesBuffer;
        public ComputeBuffer trianglesBuffer => _trianglesBuffer;

        private ComputeBuffer _vertexBuffer;
        public ComputeBuffer vertexBuffer => _vertexBuffer;

        private ComputeBuffer _visibleTriangles;
        public ComputeBuffer visibleTrianglesAppend => _visibleTriangles;

        private ComputeBuffer _isVisiblePerVertex;
        public ComputeBuffer isVisiblePerVertex => _isVisiblePerVertex;

        private ComputeBuffer _grassInstanceData;
        public ComputeBuffer grassInstanceData => _grassInstanceData;

        public int vertexAmount { get; private set; }
        public int triangleAmount { get; private set; }

        public RenderBuffers()
        {
            // Debug.Log("Creating buffers");
            _drawArgsBuffer = new SharedBuffer<DrawIndirectArgs>();
            _drawArgsBuffer.AddSubBuffer(DrawArgs.DrawArgs, 1);
            _drawArgsBuffer.AddSubBuffer(DrawArgs.ShadowDrawArgs, 1);
            _drawArgsBuffer.Allocate(ComputeBufferType.IndirectArguments);
            _drawArgsBuffer.buffer.name = "(Grass) Draw Args Buffer";

            // _computeArgsBuffer = new SharedBuffer<ComputeIndirectArgs>();
            // _computeArgsBuffer.AddSubBuffer(ComputeArgs.GenerateBladesArgs, 1);
            // _computeArgsBuffer.AddSubBuffer(ComputeArgs.InterpolateBladesArgs, 1);
            // _computeArgsBuffer.Allocate(ComputeBufferType.IndirectArguments);
            // _computeArgsBuffer.buffer.name = "(Grass) Compute Args Buffer";

            // _computeArgsBuffer.SetData(ComputeArgs.GenerateBladesArgs, new ComputeIndirectArgs[1] { new ComputeIndirectArgs(){
            //         threadGroupsX = 0, // thread groups X (filled by compute)
            //         threadGroupsY = 1, // thread groups Y
            //         threadGroupsZ = 1  // thread groups Z
            //     }});
            // _computeArgsBuffer.SetData(ComputeArgs.InterpolateBladesArgs, new ComputeIndirectArgs[1] { new ComputeIndirectArgs(){
            //         threadGroupsX = 0, // thread groups X (filled by compute)
            //         threadGroupsY = 1, // thread groups Y
            //         threadGroupsZ = 1  // thread groups Z
            //     }});

            _generateDispatch = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
            _generateDispatch.name = "(Grass) Generate Blades Args";
            _generateDispatch.SetData(new[] { 0, 1, 1 });
            _interpolateDispatch = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
            _interpolateDispatch.name = "(Grass) Interpolate Blades Args";
            _interpolateDispatch.SetData(new[] { 0, 1, 1 });

            _auxInt = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.Structured);
            _auxInt.SetData(new[] { 0, 0, 0 });
            _auxInt.name = "(Grass) Aux int Buffer";

        }
        public void Validate(RenderSharedData data)
        {
            if (data.settings.mesh)
            {
                _drawArgsBuffer.SetData(DrawArgs.DrawArgs, new DrawIndirectArgs[1] { new DrawIndirectArgs(){
                    indexCountPerInstance =(uint)data.settings.mesh.GetIndexCount(0),
                    instanceCount = 0, // filled by compute
                    startIndexLocation = (uint)data.settings.mesh.GetIndexStart(0),
                    baseVertexLocation = (uint)data.settings.mesh.GetBaseVertex(0) ,
                    startInstanceLocation = 0
                }});
                _drawArgsBuffer.SetData(DrawArgs.ShadowDrawArgs, new DrawIndirectArgs[1] { new DrawIndirectArgs(){
                    indexCountPerInstance = (uint)data.settings.mesh.GetIndexCount(0),
                    instanceCount = 0, // filled by compute
                    startIndexLocation = (uint)data.settings.mesh.GetIndexStart(0),
                    baseVertexLocation = (uint)data.settings.mesh.GetBaseVertex(0) ,
                    startInstanceLocation = 0
                }});
            }


            List<GrassVertex> vertices = new List<GrassVertex>();
            List<GrassTriangle> triangles = new List<GrassTriangle>();
            data.mgr.GetAllVertexAndTriangles(ref vertices, ref triangles);
            vertexAmount = vertices.Count;
            if (_vertexBuffer == null || _vertexBuffer.count != vertexAmount)
            {
                _vertexBuffer?.Release();
                // Debug.Log("Resizing vertices");
                _vertexBuffer = new ComputeBuffer(Mathf.Max(1, vertices.Count), Marshal.SizeOf<GrassVertex>(), ComputeBufferType.Structured);
                _vertexBuffer.name = "(Grass) Vertex Buffer";
            }
            // Debug.Log($"Setting buffer data {vertices.Count}");
            _vertexBuffer.SetData(vertices);

            triangleAmount = triangles.Count;
            if (_trianglesBuffer == null || _trianglesBuffer.count != triangleAmount)
            {
                _trianglesBuffer?.Release();
                // Debug.Log("Resizing triangles");
                _trianglesBuffer = new ComputeBuffer(Mathf.Max(1, triangles.Count), Marshal.SizeOf<GrassTriangle>(), ComputeBufferType.Structured);
                _trianglesBuffer.name = "(Grass) Triangle Buffer";
            }
            _trianglesBuffer.SetData(triangles);

            if (_bladeSourceData == null || _bladeSourceData.count != data.settings.maxBlades)
            {
                _bladeSourceData?.Release();
                // Debug.Log("Resizing bladeSourceData");
                _bladeSourceData = new ComputeBuffer(data.settings.maxBlades, Marshal.SizeOf<BladeSourceData>(), ComputeBufferType.Append);
                _bladeSourceData.name = "(Grass) Blade Source Data AppendBuffer";
                _bladeSourceData.SetCounterValue(0);
            }

            if (_grassInstanceData == null || _grassInstanceData.count != data.settings.maxBlades)
            {
                _grassInstanceData?.Release();
                // Debug.Log("Resizing grassInstanceData");
                _grassInstanceData = new ComputeBuffer(data.settings.maxBlades, Marshal.SizeOf<GrassBladeInstanceData>(), ComputeBufferType.Append);
                _grassInstanceData.name = "(Grass) Grass Instances Data AppendBuffer";
                _grassInstanceData.SetCounterValue(0);
            }

            if (_isVisiblePerVertex == null || _isVisiblePerVertex.count != vertexAmount)
            {
                _isVisiblePerVertex?.Release();
                // Debug.Log("Resizing isVisiblePerVertex");
                _isVisiblePerVertex = new ComputeBuffer(Mathf.Max(1, vertexAmount), Marshal.SizeOf<VertexCullResult>(), ComputeBufferType.Structured);
                _isVisiblePerVertex.name = "(Grass) Is Visible per Vertex Buffer";
            }

            if (_visibleTriangles == null || _visibleTriangles.count != triangleAmount)
            {
                _visibleTriangles?.Release();
                // Debug.Log("Resizing visibleTriangles");
                _visibleTriangles = new ComputeBuffer(Mathf.Max(1, triangleAmount), sizeof(uint), ComputeBufferType.Append);
                _visibleTriangles.name = "(Grass) Visible Triangles AppendBuffer";
                _visibleTriangles.SetCounterValue(0);
            }

        }

        public void Dispose()
        {
            // Debug.Log("Disposing buffers");
            _interpolateDispatch?.Release();
            _interpolateDispatch = null;
            _generateDispatch?.Release();
            _generateDispatch = null;
            _drawArgsBuffer?.Release();
            _drawArgsBuffer = null;
            _computeArgsBuffer?.Release();
            _computeArgsBuffer = null;
            _auxInt?.Release();
            _auxInt = null;
            _trianglesBuffer?.Release();
            _trianglesBuffer = null;
            _vertexBuffer?.Release();
            _vertexBuffer = null;
            _visibleTriangles?.Release();
            _visibleTriangles = null;
            _isVisiblePerVertex?.Release();
            _isVisiblePerVertex = null;
            _grassInstanceData?.Release();
            _grassInstanceData = null;
            _bladeSourceData?.Release();
            _bladeSourceData = null;
        }
    }
}