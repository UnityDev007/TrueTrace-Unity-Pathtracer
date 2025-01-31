using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System; 
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
namespace TrueTrace {
    // [System.Serializable]
    unsafe public class BVH2Builder {

        public BVHNode2Data* BVH2Nodes;
        private int* DimensionedIndices;
        private int* temp;
        private bool* indices_going_left;
        public int PrimCount;
        public int[] FinalIndices;
        private ObjectSplit split = new ObjectSplit();
        private float* SAH;
        private AABB* Primitives;
        
        // public BVHNodeVerbose[] VerboseNodes;


        public NativeArray<BVHNode2Data> BVH2NodesArray;
        public NativeArray<int> DimensionedIndicesArray;
        public NativeArray<int> tempArray;
        public NativeArray<bool> indices_going_left_array;
        public NativeArray<float> CentersX;
        public NativeArray<float> CentersY;
        public NativeArray<float> CentersZ;
        public NativeArray<float> SAHArray;

        public struct ObjectSplit {
            public int index;
            public float cost;
            public int dimension;
            public AABB aabb_left;
            public AABB aabb_right;
        }

        AABB aabb_left;
        AABB aabb_right;
        void partition_sah(int first_index, int index_count) {
            split.cost = float.MaxValue;
            split.index = -1;
            split.dimension = -1;
            split.aabb_left.init();
            split.aabb_right.init();

            int Offset;
            for(int dimension = 0; dimension < 3; dimension++) {
                aabb_left.init();
                aabb_right.init();
                Offset = PrimCount * dimension + first_index;
                for(int i = 1; i < index_count; i++) {
                    aabb_left.Extend(ref Primitives[DimensionedIndices[Offset + i - 1]]);

                    SAH[i] = surface_area(ref aabb_left) * (float)i;
                }

                for(int i = index_count - 1; i > 0; i--) {
                    aabb_right.Extend(ref Primitives[DimensionedIndices[Offset + i]]);

                    float cost = SAH[i] + surface_area(ref aabb_right) * (float)(index_count - i);

                    if(cost <= split.cost) {
                        split.cost = cost;
                        split.index = first_index + i;
                        split.dimension = dimension;
                        split.aabb_right = aabb_right;
                    }
                }
            }
            Offset = split.dimension * PrimCount;
            for(int i = first_index; i < split.index; i++) split.aabb_left.Extend(ref Primitives[DimensionedIndices[Offset + i]]);
        }
        void BuildRecursive(int nodesi, ref int node_index, int first_index, int index_count) {
            if(index_count == 1) {
                BVH2Nodes[nodesi].left = first_index;
                BVH2Nodes[nodesi].count = (uint)index_count;
                return;
            }
            
            partition_sah(first_index, index_count);
            int Offset = split.dimension * PrimCount;
            int IndexEnd = first_index + index_count;
            for(int i = first_index; i < IndexEnd; i++) indices_going_left[DimensionedIndices[Offset + i]] = i < split.index;

            for(int dim = 0; dim < 3; dim++) {
                if(dim == split.dimension) continue;

                int index;
                int left = 0;
                int right = split.index - first_index;
                Offset = dim * PrimCount;
                for(int i = first_index; i < IndexEnd; i++) {
                    index = DimensionedIndices[Offset + i];
                    temp[indices_going_left[index] ? (left++) : (right++)] = index;
                }
          
                NativeArray<int>.Copy(tempArray, 0, DimensionedIndicesArray, Offset + first_index, index_count);
            }
            BVH2Nodes[nodesi].left = node_index;

            BVH2Nodes[BVH2Nodes[nodesi].left].aabb = split.aabb_left;
            BVH2Nodes[BVH2Nodes[nodesi].left + 1].aabb = split.aabb_right;
            node_index += 2;
            int Index = split.index;
            BuildRecursive(BVH2Nodes[nodesi].left, ref node_index, first_index, Index - first_index);
            BuildRecursive(BVH2Nodes[nodesi].left + 1, ref node_index, Index, first_index + index_count - Index);
        }

        float surface_area(ref AABB aabb) {
            Vector3 sizes = aabb.BBMax - aabb.BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }

        public unsafe BVH2Builder(AABB* Triangles, int PrimCount) {//Bottom Level Acceleration Structure Builder
            this.PrimCount = PrimCount;
            Primitives = Triangles;
            FinalIndices = new int[PrimCount];
            SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersX = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersY = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            BVH2NodesArray = new NativeArray<BVHNode2Data>(PrimCount * 2, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            BVH2Nodes = (BVHNode2Data*)NativeArrayUnsafeUtility.GetUnsafePtr(BVH2NodesArray);
            float* ptr = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersX);
            float* ptr1 = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersY);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {
                FinalIndices[i] = i;
                ptr[i] = (Triangles[i].BBMax.x - Triangles[i].BBMin.x) / 2.0f + Triangles[i].BBMin.x;
                ptr1[i] = (Triangles[i].BBMax.y - Triangles[i].BBMin.y) / 2.0f + Triangles[i].BBMin.y;
                SAH[i] = (Triangles[i].BBMax.z - Triangles[i].BBMin.z) / 2.0f + Triangles[i].BBMin.z;
                BVH2Nodes[0].aabb.Extend(ref Triangles[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr[s1] - ptr[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersX.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr1[s1] - ptr1[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersY.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);


            indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);
            aabb_left = new AABB();
            aabb_right = new AABB();
            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount);
            indices_going_left_array.Dispose();
            tempArray.Dispose();
            SAHArray.Dispose();
            int BVHNodeCount = BVH2NodesArray.Length;
            NativeArray<int>.Copy(DimensionedIndicesArray, 0, FinalIndices, 0, PrimCount);
            DimensionedIndicesArray.Dispose();
        }

        public unsafe BVH2Builder(AABB[] MeshAABBs) {//Top Level Acceleration Structure
            PrimCount = MeshAABBs.Length;
            FinalIndices = new int[PrimCount];
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersX = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersY = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            BVH2NodesArray = new NativeArray<BVHNode2Data>(PrimCount * 2, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            BVH2Nodes = (BVHNode2Data*)NativeArrayUnsafeUtility.GetUnsafePtr(BVH2NodesArray);
            float* ptr = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersX);
            float* ptr1 = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersY);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {//Treat Bottom Level BVH Root Nodes as triangles
                FinalIndices[i] = i;
                ptr[i] = ((MeshAABBs[i].BBMax.x - MeshAABBs[i].BBMin.x)/2.0f + MeshAABBs[i].BBMin.x);
                ptr1[i] = ((MeshAABBs[i].BBMax.y - MeshAABBs[i].BBMin.y)/2.0f + MeshAABBs[i].BBMin.y);
                SAH[i] = ((MeshAABBs[i].BBMax.z - MeshAABBs[i].BBMin.z)/2.0f + MeshAABBs[i].BBMin.z);
                BVH2Nodes[0].aabb.Extend(ref MeshAABBs[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr[s1] - ptr[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersX.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr1[s1] - ptr1[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersY.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);

            indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            NativeArray<AABB> PrimAABBs = new NativeArray<AABB>(MeshAABBs, Unity.Collections.Allocator.TempJob);
            Primitives = (AABB*)NativeArrayUnsafeUtility.GetUnsafePtr(PrimAABBs);
            tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);
            aabb_left = new AABB();
            aabb_right = new AABB();
            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount);
            indices_going_left_array.Dispose();
            tempArray.Dispose();
            SAHArray.Dispose();
            int BVHNodeCount = BVH2NodesArray.Length;
            NativeArray<int>.Copy(DimensionedIndicesArray, 0, FinalIndices, 0, PrimCount);
            DimensionedIndicesArray.Dispose();
            PrimAABBs.Dispose();

        }

    }
}