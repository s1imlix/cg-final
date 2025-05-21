using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

namespace CGFinal.Helpers {
    // avoid collision with other classes
    public static class ComputeHelper
    {
        public static void Dispatch(ComputeShader shader, int numIterationsX, int kernelIndex) {
            Vector3Int threadGroupsSizes = GetThreadGroupSizes(shader, kernelIndex);
            int threadGroupX = Mathf.CeilToInt(numIterationsX / (float)threadGroupsSizes.x);

            shader.Dispatch(kernelIndex, threadGroupX, 1, 1);
        }

        public static int GetStride<T>() {
            return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        }

        public static ComputeBuffer CreateStructBuffer<T>(T[] data) {
            var buffer = new ComputeBuffer(data.Length, GetStride<T>());
            buffer.SetData(data);
            return buffer;
        }

        public static ComputeBuffer CreateStructBuffer<T>(int length) {
            var buffer = new ComputeBuffer(length, GetStride<T>());
            return buffer;
        }

        public static GraphicsBuffer CreateArgsBuffer(Mesh particleMesh, int numInstances) {
            GraphicsBuffer _argsBuffer 
                = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 
                                        1, 
                                        GraphicsBuffer.IndirectDrawIndexedArgs.size);

            GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            args[0].indexCountPerInstance = particleMesh.GetIndexCount(0);
            args[0].instanceCount = (uint)numInstances;
            args[0].startIndex = particleMesh.GetIndexStart(0);
            args[0].baseVertexIndex = particleMesh.GetBaseVertex(0);
            args[0].startInstance = 0;

            _argsBuffer.SetData(args);
            return _argsBuffer;
        }

        public static T[] DebugStructBuffer<T>(ComputeBuffer buffer, int count) {
            T[] data = new T[count];
            buffer.GetData(data);
            for (int i = 0; i < count; i++) {
                Debug.Log($"DebugStructBuffer: {i} -> {data[i]}");
            }
            return data;
        }

        public static void SetBuffer(ComputeShader shader, ComputeBuffer buffer, string name, params int[] kernelIndices) {
            foreach (var kernelIndex in kernelIndices) {
                shader.SetBuffer(kernelIndex, name, buffer);
            }
        }
        
        public static Vector3Int GetThreadGroupSizes(ComputeShader shader, int kernelIndex) {
            // https://docs.unity3d.com/6000.1/Documentation/ScriptReference/ComputeShader.GetKernelThreadGroupSizes.html
            uint x, y, z;
            shader.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
            return new Vector3Int((int)x, (int)y, (int)z);
        }

        public static void Release(params ComputeBuffer[] buffers) {
            foreach (var buffer in buffers) {
                if (buffer != null) {
                    buffer.Release();
                }
            }
        }

        public static void Release(params GraphicsBuffer[] buffers) {
            foreach (var buffer in buffers) {
                if (buffer != null) {
                    buffer.Release();
                }
            }
        }
    }
}

