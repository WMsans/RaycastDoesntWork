using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public static class AwaitableTools
    {
        public static Dictionary<string, string> PreGeneratedKeyNames = new();
        public static string GetPregeneratedName(string key)
        {
            if (!PreGeneratedKeyNames.TryGetValue(key, out var waiterKey))
            {
                waiterKey = key + "-waiter";
                PreGeneratedKeyNames.Add(key, waiterKey);
            }
            return waiterKey;
        }
        public static bool Wait<TAwaitable, TResult, TReuser>(DataStore<string, TAwaitable> awaitableStore, DataStore<string, TResult> dataStore, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var isCompleted = dataStore.TryGetValue(key, out var resultingObject);
            if (!isCompleted)
            {
                string waiterKey = GetPregeneratedName(key);
                if (UnsafeWait(awaitableStore, ref Reuser, out ResultingData, waiterKey))
                {
                    dataStore.AddData(key, ResultingData);
                    isCompleted = true;
                }
                else
                {
                    ResultingData = default;
                }
            }
            else
            {
                ResultingData = resultingObject;
            }

            return isCompleted;
        }

        public static bool Wait<TAwaitable, TResult, TReuser>(DataStore<string, object> store, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var isCompleted = store.TryGetValue(key, out var resultingObject);
            if (!isCompleted)
            {
                string waiterKey = GetPregeneratedName(key);
                if (UnsafeWaitGeneric<TAwaitable, TResult, TReuser>(store, ref Reuser, out ResultingData, waiterKey))
                {
                    store.AddData(key, ResultingData);
                    isCompleted = true;
                }
                else
                {
                    ResultingData = default;
                }
            }
            else
            {
                ResultingData = (TResult)resultingObject;
            }

            return isCompleted;
        }

        public static bool UnsafeWait<TAwaitable, TResult, TReuser>(DataStore<string, TAwaitable> store, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var factory = new ReusableFactory<TAwaitable, TReuser>(Reuser);
            var currentData = store.GetOrCreateData<TAwaitable, ReusableFactory<TAwaitable, TReuser>>(key, ref factory);
            var isCompleted = currentData.ProcessData();
            if (isCompleted)
            {
                ResultingData = currentData.Result;
                GenericPoolLight.Release(currentData);
            }
            else
                ResultingData = default;

            return isCompleted;
        }

        public static bool UnsafeWaitGeneric<TAwaitable, TResult, TReuser>(DataStore<string, object> store, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var factory = new ReusableFactory<TAwaitable, TReuser>(Reuser);
            var currentData = store.GetOrCreateData<TAwaitable, ReusableFactory<TAwaitable, TReuser>>(key, ref factory);
            var isCompleted = currentData.ProcessData();
            if (isCompleted)
            {
                ResultingData = currentData.Result;
                GenericPoolLight.Release(currentData);
            }
            else
                ResultingData = default;

            return isCompleted;
        }

        public static bool WaitNode<TAwaitable, TResult, TReuser>(BranchData settings, ref TReuser Reuser, out TResult ResultingData, InfiniteLandsNode node, string fieldName)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var nodeStore = settings.GetNodeStore(node);
            var awaitableStore = nodeStore.GetStoreOfType<TAwaitable>(true);
            var dataStore = nodeStore.GetStoreOfType<TResult>(true);

            return Wait(awaitableStore, dataStore, ref Reuser, out ResultingData, fieldName);
        }

        public static bool WaitGlobal<TAwaitable, TResult, TReuser>(BranchData settings, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var store = settings.treeData.GlobalStore;
            return Wait<TAwaitable, TResult, TReuser>(store, ref Reuser, out ResultingData, key);
        }

        public static bool IterateOverItems<TItems, TCallback>(List<TItems> items, ref TCallback method)
            where TCallback : struct, ICallMethod<TItems>
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                bool result = method.Callback(items[i]);
                if (result)
                {
                    items.RemoveAt(i);
                }
            }

            return items.Count <= 0;
        }

        public static bool CompactData<T>(List<AwaitableData<T>> WaitingFor, List<T> Targets)
        {
            var compactor = new ICompactData<T>(Targets);
            return IterateOverItems(WaitingFor, ref compactor);
        }

        public static bool CopyHeightMapFromBranchTo<R>(BranchData currentBranch, InfiniteLandsNode node,
            string InputFieldName, ref R branchMaker,
            out HeightData Result, string outputName, float scaleFactor = 1) where R : struct, IFactory<BranchData>
        {
            MoveHeightMapFromToReuser<R> moveHeightMapFromToReuser = new MoveHeightMapFromToReuser<R>(node, currentBranch, InputFieldName, outputName, scaleFactor, ref branchMaker);
            return WaitNode<MoveHeightMapFromTo, HeightData, MoveHeightMapFromToReuser<R>>(currentBranch, ref moveHeightMapFromToReuser, out Result, node, outputName);

        }

        public static bool ValidateIsInsideThreshold(BranchData currentBranch, InfiniteLandsNode node,
            string MaskName, float Threshold,
            out MaskResult result)
        {
            ValidateThresholdReuser moveHeightMapFromToReuser = new ValidateThresholdReuser(node, currentBranch, MaskName, Threshold);
            return WaitNode<ValidateIfMaskContainsValues, MaskResult, ValidateThresholdReuser>(currentBranch, ref moveHeightMapFromToReuser, out result, node, "insideMask");

        }

        private struct ICompactData<T> : ICallMethod<AwaitableData<T>>
        {
            List<T> Results;
            public ICompactData(List<T> Results)
            {
                this.Results = Results;
            }
            public bool Callback(AwaitableData<T> value)
            {
                if (!value.ProcessData()) return false;

                Results.Add(value.Result);
                return true;
            }
        }
        private struct MoveHeightMapFromToReuser<R> : IReuseObject<MoveHeightMapFromTo> where R : struct, IFactory<BranchData>
        {
            private InfiniteLandsNode node;
            private BranchData branch;
            private string InputName;
            private string OutputName;
            private float ScaleFactor;
            private R branchMaker;
            public MoveHeightMapFromToReuser(InfiniteLandsNode node, BranchData branch,
                string InputName, string OutputName, float ScaleFactor, ref R branchMaker)
            {
                this.node = node;
                this.branch = branch;
                this.InputName = InputName;
                this.OutputName = OutputName;
                this.branchMaker = branchMaker;
                this.ScaleFactor = ScaleFactor;
            }
            public void Reuse(MoveHeightMapFromTo instance)
            {
                var fromBranch = branchMaker.Create();
                instance.Reuse(node, fromBranch, branch, InputName, OutputName, ScaleFactor);
            }
        }

        private struct ValidateThresholdReuser : IReuseObject<ValidateIfMaskContainsValues>
        {
            private InfiniteLandsNode node;
            private BranchData branch;
            private string MaskName;
            private float threshold;
            public ValidateThresholdReuser(InfiniteLandsNode node, BranchData branch,
                string MaskName, float threshold)
            {
                this.node = node;
                this.branch = branch;
                this.MaskName = MaskName;
                this.threshold = threshold;
            }
            public void Reuse(ValidateIfMaskContainsValues instance)
            {
                instance.Reuse(node, MaskName, branch, threshold);
            }
        }
        private struct ReusableFactory<A, R> : IFactory<A>
            where A : class, new()
            where R : struct, IReuseObject<A>
        {
            private R reuser;
            public ReusableFactory(R reuser)
            {
                this.reuser = reuser;
            }
            public A Create()
            {
                var value = GenericPoolLight<A>.Get();
                reuser.Reuse(value);
                return value;
            }
        }
        public struct MaskResult
        {
            public bool ContainsData;
            public HeightData MaskData;
        }
        private class ValidateIfMaskContainsValues : AwaitableData<MaskResult>
        {

            public MaskResult Result { get; private set; }

            private string MaskFieldName;
            private InfiniteLandsNode node;
            private BranchData branch;
            private int SubState;
            private float Threshold;

            private NativeArray<int> MinMaxArray;
            private ReturnableBranch returnableBranch;
            private HeightMapBranch heightBranch;
            private JobHandle WaitingForMask;
            private HeightData Mask;

            public void Reuse(InfiniteLandsNode node, string MaskFieldName, BranchData branch, float threshold)
            {
                this.node = node;
                this.MaskFieldName = MaskFieldName;
                this.branch = branch;
                this.Threshold = threshold;
                SubState = 0;

                heightBranch = branch.GetData<HeightMapBranch>();
                returnableBranch = branch.GetData<ReturnableBranch>();
            }

            public bool ProcessData()
            {
                if (SubState == 0)
                {
                    if (!node.ProcessDependency(branch, MaskFieldName)) return false;

                    node.TryGetInputData(branch, out Mask, MaskFieldName);
                    SubState++;
                }
                if (SubState == 1)
                {
                    MinMaxArray = returnableBranch.GetData<int>(1);
                    MinMaxArray[0] = -1;
                    var originMap = heightBranch.GetMap();
                    WaitingForMask = CheckTreshold.ScheduleParallel(MinMaxArray, originMap,
                        Mask.indexData, Mask.indexData.Length, Mask.indexData.Resolution, Threshold, Mask.jobHandle);
                    SubState++;
                }
                if (SubState == 2)
                {
                    if (WaitingForMask.IsCompleted || branch.treeData.ForceComplete)
                    {
                        WaitingForMask.Complete();
                        SubState++;
                    }
                }

                if (SubState == 3)
                {
                    var minValue = MinMaxArray[0];
                    Result = new MaskResult()
                    {
                        ContainsData = minValue > 0,
                        MaskData = Mask
                    };
                    SubState++;
                }

                return SubState == 4;
            }
        }
        private class MoveHeightMapFromTo : AwaitableData<HeightData>
        {
            public HeightData Result { get; private set; }

            BranchData ToBranch;
            InfiniteLandsNode Node;
            BranchData FromBranch;

            private int SubState;
            private HeightData InputData;
            private string InputName;
            private string OutputName;
            private float ScaleFactor;

            public void Reuse(InfiniteLandsNode node, BranchData fromBranch, BranchData toBranch, string inputName, string outputName, float scaleFactor)
            {
                ToBranch = toBranch;
                Node = node;
                InputName = inputName;
                OutputName = outputName;
                FromBranch = fromBranch;
                SubState = 0;
                ScaleFactor = scaleFactor;
            }

            public bool ProcessData()
            {
                if (SubState == 0)
                {
                    if (!Node.ProcessDependency(FromBranch, InputName)) return false;
                    if (!Node.TryGetInputData(FromBranch, out InputData, InputName)) Debug.LogError("Something went wrong");
                    SubState++;
                }

                if (SubState == 1)
                {
                    HeightMapBranch fromHeightBranch = FromBranch.GetData<HeightMapBranch>();
                    var from = fromHeightBranch.GetMap();

                    HeightMapBranch heightBranch = ToBranch.GetData<HeightMapBranch>();
                    var targetSpace = heightBranch.GetAllocationSpace(Node, OutputName, out var to);

                    JobHandle job = CopyToFrom.ScheduleParallel(to, from,
                        targetSpace, InputData.indexData,
                        InputData.jobHandle, ScaleFactor);

                    Result = new HeightData(job, targetSpace, InputData.minMaxValue*ScaleFactor);
                    SubState++;
                }

                return SubState == 2;
            }
        }
    }
}