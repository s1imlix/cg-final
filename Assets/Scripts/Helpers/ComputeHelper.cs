using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace CGFinal.Helpers {
    // avoid collision with other classes
    public static class ComputeHelper
    {
        public static void Dispatch(ComputeShader shader, int numIterationsX, int numIterationsY, int numIterationsZ, int kernelIndex) {
            Vector3Int threadGroupsSizes = GetThreadGroupSizes(shader, kernelIndex);
            int threadGroupX = Mathf.CeilToInt(numIterationsX / (float)threadGroupsSizes.x);
            int threadGroupY = Mathf.CeilToInt(numIterationsY / (float)threadGroupsSizes.y);
            int threadGroupZ = Mathf.CeilToInt(numIterationsZ / (float)threadGroupsSizes.z);

            shader.Dispatch(kernelIndex, threadGroupX, threadGroupY, threadGroupZ);
        }
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

        public static void CreateAppendBuffer<T>(ref ComputeBuffer buffer, int count)
		{
			int stride = GetStride<T>();
			bool createNewBuffer = (buffer == null || !buffer.IsValid() || buffer.count != count || buffer.stride != stride);
			if (createNewBuffer)
			{
				Release(buffer);
				buffer = new ComputeBuffer(count, stride, ComputeBufferType.Append);
			}

			buffer.SetCounterValue(0);
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

        public static void CreateRenderTexture3D(ref RenderTexture texture, int width, int height, int depth, GraphicsFormat format = GraphicsFormat.R32G32B32A32_SFloat,
                                                TextureWrapMode wrapMode = TextureWrapMode.Repeat, bool useMipMap = false, string name = "RenderTexture3D") {
            if (texture != null) {
                texture.Release();
            }
            
            texture = new RenderTexture(width, height, 0, format);
            texture.enableRandomWrite = true;
            texture.autoGenerateMips = false;
            texture.dimension = TextureDimension.Tex3D;
            texture.volumeDepth = depth;  // Set the Z-dimension here
            texture.wrapMode = wrapMode;
            texture.filterMode = FilterMode.Bilinear;
            texture.useMipMap = useMipMap;
            texture.name = name;
            texture.Create();
        }

        
    }
}

