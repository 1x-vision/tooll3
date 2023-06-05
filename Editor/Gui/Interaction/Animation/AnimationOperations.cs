﻿using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Animation;
using T3.Editor.Gui.Windows.TimeLine;

namespace T3.Editor.Gui.Interaction.Animation
{
    public class AnimationOperations
    {
        public static List<VDefinition> InsertKeyframeToCurves(IEnumerable<Curve> curves, double time, float increment = 0)
        {
            var commands = new List<ICommand>();
            
            var newKeyframes = new List<VDefinition>(4);
            foreach (var curve in curves)
            {
                var value = curve.GetSampledValue(time);
                var previousU = curve.GetPreviousU(time);

                var key = (previousU != null)
                              ? curve.GetV(previousU.Value).Clone()
                              : new VDefinition();

                key.Value = value + increment;
                key.U = time;

                var command = new AddKeyframesCommand(curve, key);
                command.Do();
                commands.Add(command);
                newKeyframes.Add(key);
            }
            
            var marcoCommand = new MacroCommand("Insert Keyframe", commands);
            UndoRedoStack.Add(marcoCommand);
            
            return newKeyframes;
        }

        public static void RemoveKeyframeFromCurves(IEnumerable<Curve> curves, double time)
        {
            var commands = new List<ICommand>();
            foreach (var curve in curves)
            {
                var key = curve.GetV(time);
                if (key != null)
                {
                    var command = new DeleteKeyframeCommand(curve, key);
                    commands.Add(command);
                }
            }
            UndoRedoStack.AddAndExecute(new MacroCommand("Delete keyframes", commands));
        }

        public static void DeleteSelectedKeyframesFromAnimationParameters(HashSet<VDefinition> selectedKeyframes,
                                                                          IEnumerable<TimeLineCanvas.AnimationParameter> animationParameters,
                                                                          Instance compositionOp)
        {
            var commands = new List<ICommand>();
            var selectKeyframesForCurve = new List<VDefinition>();
            
            foreach (var param in animationParameters)
            {
                foreach (var curve in param.Curves)
                {
                    selectKeyframesForCurve.Clear();
                    //var allSelected = true;
                    var vDefinitions = curve.GetVDefinitions().ToList();
                    foreach (var keyframe in vDefinitions)
                    {
                        if (!selectedKeyframes.Contains(keyframe))
                        {
                            continue;
                        }
                        selectKeyframesForCurve.Add(keyframe);
                    }

                    var allSelected = selectKeyframesForCurve.Count == vDefinitions.Count;
                    if (allSelected)
                    {
                        commands.Add(new RemoveAnimationsCommand(compositionOp.Symbol.Animator, new []{param.Input} ));
                    }
                    else
                    {
                        foreach (var keyframe in selectKeyframesForCurve)
                        {
                            commands.Add(new DeleteKeyframeCommand(curve, keyframe));
                            selectedKeyframes.Remove(keyframe);
                        }
                    }

                }
            }
            
            UndoRedoStack.AddAndExecute(new MacroCommand("Delete keyframes", commands));
        }
    }
}