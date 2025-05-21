using UnityEngine;
using CGFinal.Helpers;
using static UnityEngine.Mathf;
using System.Collections.Generic;

public class GPUSort
{
    private const int sortKernel = 0;
    private const int offsetKernel = 1;

    // Driver to the BitonicMergeSort.compute 
    ComputeShader sortProgram;
    ComputeBuffer _indexBuffer;

    public GPUSort() {
        sortProgram = Resources.Load<ComputeShader>("BitonicMergeSort");
        if (sortProgram == null) {
            Debug.LogError("BitonicMergeSort.compute not found in Resources folder.");
        }
    }

    public void SetBuffers(ComputeBuffer indexBuffer, ComputeBuffer offsetBuffer) {
        this._indexBuffer = indexBuffer;
        sortProgram.SetBuffer(sortKernel, "Entries", indexBuffer);
        ComputeHelper.SetBuffer(sortProgram, offsetBuffer, "Offsets", offsetKernel);
        ComputeHelper.SetBuffer(sortProgram, indexBuffer, "Entries", offsetKernel);
    }

    private void updateSettings(int groupWidth, int groupHeight, int stepIndex) {
        sortProgram.SetInt("groupWidth", groupWidth);
        sortProgram.SetInt("groupHeight", groupHeight);
        sortProgram.SetInt("stepIndex", stepIndex);
    }

    public void SortAndCalculateOffsets() {
        sortProgram.SetInt("numEntries", _indexBuffer.count);

        int numStages = (int)Log(NextPowerOfTwo(_indexBuffer.count), 2);

        for (int stageIndex = 0; stageIndex < numStages; stageIndex++)
        {
            for (int stepIndex = 0; stepIndex < stageIndex + 1; stepIndex++)
            {
                // Calculate some pattern stuff
                int groupWidth = 1 << (stageIndex - stepIndex);
                int groupHeight = 2 * groupWidth - 1;
                updateSettings(groupWidth, groupHeight, stepIndex);

                // Run the sorting step on the GPU
                ComputeHelper.Dispatch(sortProgram, NextPowerOfTwo(_indexBuffer.count) / 2, sortKernel);
            }
        }

        ComputeHelper.Dispatch(sortProgram, _indexBuffer.count, offsetKernel);
    }





}
