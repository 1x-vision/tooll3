using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Core.Operator
{
    public class SymbolExtension
    {
        // todo: how is a symbol extension defined, what exactly does this mean
    }

    public class Animator : SymbolExtension
    {
        struct CurveId
        {
            public CurveId(Guid symbolChildId, Guid inputId, int index = 0)
            {
                SymbolChildId = symbolChildId;
                InputId = inputId;
                Index = index;
            }

            public CurveId(IInputSlot inputSlot, int index = 0)
            {
                SymbolChildId = inputSlot.Parent.SymbolChildId;
                InputId = inputSlot.Id;
                Index = index;
            }

            public Guid SymbolChildId;
            public Guid InputId;
            public int Index;
        }



        public void CopyAnimationsTo(Animator targetAnimator, List<Guid> childrenToCopyAnimationsFrom, Dictionary<Guid, Guid> oldToNewIdDict)
        {
            foreach (var (id, curve) in _animatedInputCurves)
            {
                if (!childrenToCopyAnimationsFrom.Contains(id.SymbolChildId))
                    continue;

                CloneAndAddCurve(targetAnimator, oldToNewIdDict, id, curve);
            }
        }

        public void RemoveAnimationsFromInstances(List<Guid> instanceIds)
        {
            List<CurveId> elementsToDelete = new List<CurveId>();
            foreach (var (id, curve) in _animatedInputCurves)
            {
                if (!instanceIds.Contains(id.SymbolChildId))
                    continue;

                elementsToDelete.Add(id);
            }

            foreach (var idToDelete in elementsToDelete)
            {
                _animatedInputCurves.Remove(idToDelete);
            }
        }

        private static void CloneAndAddCurve(Animator targetAnimator, Dictionary<Guid, Guid> oldToNewIdDict, CurveId id, Curve curve)
        {
            Guid newInstanceId = oldToNewIdDict[id.SymbolChildId];
            var newCurveId = new CurveId(newInstanceId, id.InputId, id.Index);
            var newCurve = curve.TypedClone();
            targetAnimator._animatedInputCurves.Add(newCurveId, newCurve);
        }

        public Curve[] AddOrRestoreCurvesToInput(IInputSlot inputSlot, Curve[] originalCurves)
        {
            switch (inputSlot)
            {
                case Slot<float> floatInputSlot:
                    return AddCurvesForFloatValue(inputSlot, new[] { floatInputSlot.Value }, originalCurves);
                case Slot<Vector2> vector2InputSlot:
                    return AddCurvesForFloatValue(inputSlot, vector2InputSlot.Value.ToArray(), originalCurves);
                case Slot<Vector3> vector3InputSlot:
                    return AddCurvesForFloatValue(inputSlot, vector3InputSlot.Value.ToArray(), originalCurves);
                case Slot<Vector4> vector4InputSlot:
                    return AddCurvesForFloatValue(inputSlot, vector4InputSlot.Value.ToArray(), originalCurves);
                case Slot<int> intInputSlot:
                    return AddCurvesForIntValue(inputSlot, new []{intInputSlot.Value}, originalCurves);
                case Slot<Size2> size2InputSlot:
                    return AddCurvesForIntValue(inputSlot, new []{size2InputSlot.Value.Width, size2InputSlot.Value.Height }, originalCurves);
                case Slot<bool> boolInputSlot:
                    return AddCurvesForIntValue(inputSlot, new []{boolInputSlot.Value ? 1 :0 }, originalCurves);
                default:
                    Log.Error("Could not create curves for this type");
                    break;
            }

            return null;
        }

        private Curve[] AddCurvesForFloatValue(IInputSlot inputSlot, float[] values, Curve[] originalCurves)
        {
            var curves = originalCurves ?? new Curve[values.Length];
            for (var index = 0; index < values.Length; index++)
            {
                if(originalCurves == null)
                    curves[index] = new Curve();
                
                curves[index].AddOrUpdateV(Playback.Current.TimeInBars, new VDefinition()
                                                                           {
                                                                               Value = values[index],
                                                                               InType = VDefinition.Interpolation.Spline,
                                                                               OutType = VDefinition.Interpolation.Spline,
                                                                           });
                _animatedInputCurves.Add(new CurveId(inputSlot, index), curves[index]);
            }
            return curves;
        }
        
        private Curve[] AddCurvesForIntValue(IInputSlot inputSlot, int[] values, Curve[] originalCurves)
        {
            var curves = originalCurves ?? new Curve[values.Length];
            for (var index = 0; index < values.Length; index++)
            {
                if(originalCurves == null)
                    curves[index] = new Curve();
                
                curves[index].AddOrUpdateV(Playback.Current.TimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = values[index],
                                                                                   InType = VDefinition.Interpolation.Constant,
                                                                                   OutType = VDefinition.Interpolation.Constant,
                                                                                   InEditMode = VDefinition.EditMode.Constant,
                                                                                   OutEditMode = VDefinition.EditMode.Constant,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, index), curves[index]);
            }
            return curves;
        }

        public void CreateUpdateActionsForExistingCurves(IEnumerable<Instance> childInstances)
        {
            // gather all inputs that correspond to stored ids
            var relevantInputs = (from curveEntry in OrderedAnimationCurves
                                 from childInstance in childInstances
                                 where curveEntry.Key.SymbolChildId == childInstance.SymbolChildId
                                 from inputSlot in childInstance.Inputs
                                 where curveEntry.Key.InputId == inputSlot.Id
                                 group (inputSlot, curveEntry.Value) by (Id: childInstance.SymbolChildId, inputSlot.Id, childInstance)
                                 into inputGroup
                                 select inputGroup).ToArray();
            
            foreach (var groupEntry in relevantInputs)
            {
                var count = groupEntry.Count();
                if (count == 1)
                {
                    var (inputSlot, curve) = groupEntry.First();
                    if (inputSlot is Slot<float> typedInputSlot)
                    {
                        typedInputSlot.OverrideWithAnimationAction(context => { typedInputSlot.Value = (float)curve.GetSampledValue(context.LocalTime); });
                        typedInputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                    else if (inputSlot is Slot<int> intSlot)
                    {
                        intSlot.OverrideWithAnimationAction(context => { intSlot.Value = (int)curve.GetSampledValue(context.LocalTime); });
                        intSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                    else if (inputSlot is Slot<bool> boolSlot)
                    {
                        boolSlot.OverrideWithAnimationAction(context => { boolSlot.Value = curve.GetSampledValue(context.LocalTime) > 0.5f; });
                        boolSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                }
                else if (count == 2)
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].inputSlot;
                    if (inputSlot is Slot<Vector2> vector2InputSlot)
                    {
                        vector2InputSlot.OverrideWithAnimationAction(context =>
                                                        {
                                                            vector2InputSlot.Value.X = (float)entries[0].Value.GetSampledValue(context.LocalTime);
                                                            vector2InputSlot.Value.Y = (float)entries[1].Value.GetSampledValue(context.LocalTime);
                                                        });
                        vector2InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                    else if (inputSlot is Slot<Size2> size2InputSlot)
                    {
                        size2InputSlot.OverrideWithAnimationAction(context =>
                                                        {
                                                            size2InputSlot.Value.Width = (int)entries[0].Value.GetSampledValue(context.LocalTime);
                                                            size2InputSlot.Value.Height = (int)entries[1].Value.GetSampledValue(context.LocalTime);
                                                        });
                        size2InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                }
                else if (count == 3)
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].inputSlot;
                    if (inputSlot is Slot<Vector3> vector3InputSlot)
                    {
                        vector3InputSlot.OverrideWithAnimationAction(context =>
                                                        {
                                                            vector3InputSlot.Value.X = (float)entries[0].Value.GetSampledValue(context.LocalTime);
                                                            vector3InputSlot.Value.Y = (float)entries[1].Value.GetSampledValue(context.LocalTime);
                                                            vector3InputSlot.Value.Z = (float)entries[2].Value.GetSampledValue(context.LocalTime);
                                                        });
                        vector3InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                    else if (inputSlot is Slot<Int3> int3InputSlot)
                    {
                        int3InputSlot.OverrideWithAnimationAction(context =>
                                                        {
                                                            int3InputSlot.Value.X = (int)entries[0].Value.GetSampledValue(context.LocalTime);
                                                            int3InputSlot.Value.Y = (int)entries[1].Value.GetSampledValue(context.LocalTime);
                                                            int3InputSlot.Value.Z = (int)entries[2].Value.GetSampledValue(context.LocalTime);
                                                        });
                        int3InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                }
                else if (count == 4)
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].inputSlot;
                    if (inputSlot is Slot<Vector4> vector4InputSlot)
                    {
                        vector4InputSlot.OverrideWithAnimationAction(context =>
                                                        {
                                                            vector4InputSlot.Value.X = (float)entries[0].Value.GetSampledValue(context.LocalTime);
                                                            vector4InputSlot.Value.Y = (float)entries[1].Value.GetSampledValue(context.LocalTime);
                                                            vector4InputSlot.Value.Z = (float)entries[2].Value.GetSampledValue(context.LocalTime);
                                                            vector4InputSlot.Value.W = (float)entries[3].Value.GetSampledValue(context.LocalTime);
                                                        });
                        vector4InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        private IOrderedEnumerable<KeyValuePair<CurveId, Curve>> OrderedAnimationCurves
        {
            get
            {
                var orderedCurves = _animatedInputCurves
                                   .OrderBy(valuePair => valuePair.Key.SymbolChildId)
                                   .ThenBy(valuePair => valuePair.Key.InputId)
                                   .ThenBy(valuePair => valuePair.Key.Index);
                return orderedCurves;
            }
        }

        public void RemoveAnimationFrom(IInputSlot inputSlot)
        {
            inputSlot.RestoreUpdateAction();
            inputSlot.DirtyFlag.Trigger &= ~DirtyFlagTrigger.Animated;
            var curveKeysToRemove = (from curveId in _animatedInputCurves.Keys
                                     where curveId.SymbolChildId == inputSlot.Parent.SymbolChildId
                                     where curveId.InputId == inputSlot.Id
                                     select curveId).ToArray(); // ToArray is needed to remove from collection in batch
            foreach (var curveKey in curveKeysToRemove)
            {
                _animatedInputCurves.Remove(curveKey);
            }
        }
        

        
        public bool TryGetFirstInputAnimationCurve(IInputSlot inputSlot, out Curve curve)
        {
            return _animatedInputCurves.TryGetValue(new CurveId(inputSlot), out curve);
        }

        private static CurveId _lookUpKey;
        public bool IsInputSlotAnimated(IInputSlot inputSlot)
        {
            _lookUpKey.SymbolChildId = inputSlot.Parent.SymbolChildId;
            _lookUpKey.InputId = inputSlot.Id;
            _lookUpKey.Index = 0;
            return _animatedInputCurves.ContainsKey(_lookUpKey);
        }

        public bool IsAnimated(Guid symbolChildId, Guid inputId) 
        {
            _lookUpKey.SymbolChildId = symbolChildId;
            _lookUpKey.InputId = inputId;
            _lookUpKey.Index = 0;

            return _animatedInputCurves.ContainsKey(_lookUpKey);
        }
 
        public bool IsInstanceAnimated(Instance instance)
        {
            using (var e = _animatedInputCurves.Keys.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (e.Current.SymbolChildId == instance.SymbolChildId)
                    {
                        return true;
                    }
                }

                return false;
            }

            // code above generates way less allocations than the line below:
            // return _animatedInputCurves.Any(c => c.Key.InstanceId == instance.Id);
        }

        public IEnumerable<Curve> GetCurvesForInput(IInputSlot inputSlot)
        {
            return from curve in _animatedInputCurves
                   where curve.Key.SymbolChildId == inputSlot.Parent.SymbolChildId
                   where curve.Key.InputId == inputSlot.Id
                   orderby curve.Key.Index
                   select curve.Value;
        }

        public IEnumerable<VDefinition> GetTimeKeys(Guid symbolChildId, Guid inputId, double time)
        {
            var curves = from curve in _animatedInputCurves
                   where curve.Key.SymbolChildId == symbolChildId
                   where curve.Key.InputId == inputId
                   orderby curve.Key.Index
                   select curve.Value;
            
            foreach (var curve in curves)
            {
                yield return curve.GetV(time);
            }
        }

        public void SetTimeKeys(Guid symbolChildId, Guid inputId, double time, List<VDefinition> vDefinitions)
        {
            var curves = from curve in _animatedInputCurves
                         where curve.Key.SymbolChildId == symbolChildId
                         where curve.Key.InputId == inputId
                         orderby curve.Key.Index
                         select curve.Value;

            var index = 0;
            foreach (var curve in curves)
            {
                var vDef = vDefinitions[index++];
                if (vDef == null)
                {
                    curve.RemoveKeyframeAt(time);
                }
                else
                {
                    curve.AddOrUpdateV(time, vDef);
                }
            }
        }
        
        public void Write(JsonTextWriter writer)
        {
            if (_animatedInputCurves.Count == 0)
                return;

            writer.WritePropertyName("Animator");
            writer.WriteStartArray();

            foreach (var (key, curve) in _animatedInputCurves.ToList().OrderBy(valuePair => valuePair.Key.Index))
            {
                writer.WriteStartObject();

                writer.WriteValue("InstanceId", key.SymbolChildId); // TODO: "InstanceId" is a misleading identifier
                writer.WriteValue("InputId", key.InputId);
                if (key.Index != 0)
                {
                    writer.WriteValue("Index", key.Index);
                }

                curve.Write(writer); // write curve itself

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        
        public void Read(JToken inputToken, Symbol symbol)
        {
            var curves = new List< KeyValuePair<CurveId, Curve>>();
            foreach (JToken entry in inputToken)
            {
                var symbolChildId = Guid.Parse(entry["InstanceId"].Value<string>());
                var inputId = Guid.Parse(entry["InputId"].Value<string>());
                var indexToken = entry.SelectToken("Index");
                var index = indexToken?.Value<int>() ?? 0;
                var curve = new Curve();

                if (symbol.Children.All(c => c.Id != symbolChildId))
                    continue;
                
                curve.Read(entry);
                curves.Add( new KeyValuePair<CurveId, Curve>(new CurveId(symbolChildId, inputId, index), curve));
            }

            foreach (var (key, value) in curves.OrderBy(curveId => curveId.Key.Index))
            {
                _animatedInputCurves.Add(key, value);
            }
        }

        /// <summary>
        /// This is used when loading operators 
        /// </summary>
        public void AddCurvesToInput(List<Curve> curves, IInputSlot inputSlot)
        {
            for (var index = 0; index < curves.Count; index++)
            {
                var curve = curves[index];
                var curveId = new CurveId(inputSlot.Parent.SymbolChildId, inputSlot.Id, index);
                _animatedInputCurves.Add(curveId, curve);
            }
            inputSlot.Parent.Parent.Symbol.CreateOrUpdateActionsForAnimatedChildren();
        }
        
        
        private readonly Dictionary<CurveId, Curve> _animatedInputCurves = new();
        
        public static void UpdateVector3InputValue(InputSlot<Vector3> inputSlot, Vector3 value)
        {
            var animator = inputSlot.Parent.Parent.Symbol.Animator;
            if (animator.IsInputSlotAnimated(inputSlot))
            {
                var curves = animator.GetCurvesForInput(inputSlot).ToArray();
                double time = Playback.Current.TimeInBars;
                Vector3 newValue = new Vector3(value.X, value.Y, value.Z);
                for (int i = 0; i < 3; i++)
                {
                    var key = curves[i].GetV(time);
                    if (key == null)
                        key = new VDefinition() { U = time };
                    key.Value = newValue.Axis(i);
                    curves[i].AddOrUpdateV(time, key);
                }
            }
            else
            {
                inputSlot.SetTypedInputValue(value);
            }
        }

        public static void UpdateFloatInputValue(InputSlot<float> inputSlot, float value)
        {
            var animator = inputSlot.Parent.Parent.Symbol.Animator;
            if (animator.IsInputSlotAnimated(inputSlot))
            {
                var curve = animator.GetCurvesForInput(inputSlot).First();
                double time = Playback.Current.TimeInBars;
                var key = curve.GetV(time);
                if (key == null)
                    key = new VDefinition() { U = time };
                key.Value = value;
                curve.AddOrUpdateV(time, key);
            }
            else
            {
                inputSlot.SetTypedInputValue(value);
            }
        }
    }
}