using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

namespace CGFinal.Helpers {
    // avoid collision with other classes
    public static class ComputeHelper
    {
       public static int GetStride<T>() {
            return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
       }

       public static ComputeBuffer CreateStructBuffer<T>(T[] data) {
            var buffer = new ComputeBuffer(data.Length, GetStride<T>());
            buffer.SetData(data);
            return buffer;
       }

       public static void DebugStructBuffer<T>(ComputeBuffer buffer, int count) {
            T[] data = new T[count];
            buffer.GetData(data);
            for (int i = 0; i < count; i++) {
                Debug.Log($"DebugStructBuffer: {i} -> {data[i]}");
            }
       }

       public static void SetBuffer(ComputeShader shader, ComputeBuffer buffer, string name, params int[] kernelIndices) {
            foreach (var kernelIndex in kernelIndices) {
                shader.SetBuffer(kernelIndex, name, buffer);
            }
       }
    }
}

