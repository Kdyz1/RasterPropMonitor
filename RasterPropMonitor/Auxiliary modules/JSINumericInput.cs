﻿/*****************************************************************************
 * RasterPropMonitor
 * =================
 * Plugin for Kerbal Space Program
 *
 *  by Mihara (Eugene Medvedev), MOARdV, and other contributors
 * 
 * RasterPropMonitor is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, revision
 * date 29 June 2007, or (at your option) any later version.
 * 
 * RasterPropMonitor is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with RasterPropMonitor.  If not, see <http://www.gnu.org/licenses/>.
 ****************************************************************************/
using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using UnityEngine;

namespace JSI
{
    class JSINumericInput : InternalModule
    {
        [KSPField]
        public string perPodPersistenceName = string.Empty;

        [KSPField]
        public string defaultValue = string.Empty;

        [KSPField]
        public string minValue = string.Empty;

        [KSPField]
        public string maxValue = string.Empty;

        [KSPField]
        public float stepSize = 0.0f;

        private RasterPropMonitorComputer rpmComp;
        private List<NumericInput> numericInputs = new List<NumericInput>();

        private VariableOrNumber minRange;
        private VariableOrNumber maxRange;

        private float remainder = 0.0f;

        public void Start()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(perPodPersistenceName))
                {
                    JUtil.LogErrorMessage(this, "perPodPersistenceName must be defined");
                    return;
                }
                if (string.IsNullOrEmpty(defaultValue))
                {
                    JUtil.LogErrorMessage(this, "defaultValue must be defined");
                    return;
                }

                if(stepSize < 0.0f)
                {
                    stepSize = 0.0f;
                }

                //JUtil.LogMessage(this, "Start(): {0}, {1}, {2}, {3}, {4}", perPodPersistenceName, defaultValue, minValue, maxValue, stepSize);

                if (!string.IsNullOrEmpty(minValue))
                {
                    minRange = VariableOrNumber.Instantiate(minValue);
                    //JUtil.LogMessage(this, "Created lower bound variable");
                }
                if (!string.IsNullOrEmpty(maxValue))
                {
                    maxRange = VariableOrNumber.Instantiate(maxValue);
                    //JUtil.LogMessage(this, "Created upper bound variable");
                }

                rpmComp = RasterPropMonitorComputer.Instantiate(internalProp);
                if(!rpmComp.HasVar(perPodPersistenceName))
                {
                    //JUtil.LogMessage(this, "Initializing per pod persistence value {0}", perPodPersistenceName);
                    
                    RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);
                    VariableOrNumber von = VariableOrNumber.Instantiate(defaultValue);
                    float value;
                    if(von.Get(out value, comp))
                    {
                        //JUtil.LogMessage(this, " ... Initialized to {0}", (int)value);
                        rpmComp.SetVar(perPodPersistenceName, (int)value);
                    }
                    else
                    {
                        JUtil.LogErrorMessage(this, "Failed to evaluate default value of {0} for {1}", defaultValue, perPodPersistenceName);
                        return;
                    }
                }

                ConfigNode moduleConfig = null;
                foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("PROP"))
                {
                    if (node.GetValue("name") == internalProp.propName)
                    {

                        moduleConfig = node.GetNodes("MODULE")[moduleID];
                        ConfigNode[] inputNodes = moduleConfig.GetNodes("USERINPUTSET");

                        for (int i = 0; i < inputNodes.Length; i++)
                        {
                            try
                            {
                                numericInputs.Add(new NumericInput(inputNodes[i], internalProp));
                                //JUtil.LogMessage(this, "Added USERINPUTSET {0}", inputNodes[i].GetValue("switchTransform"));
                            }
                            catch (ArgumentException e)
                            {
                                JUtil.LogErrorMessage(this, "Error in building prop number {1} - {0}", e.Message, internalProp.propID);
                            }
                        }
                        break;
                    }
                }

                enabled = true;
            }
            catch
            {
                JUtil.AnnoyUser(this);
                enabled = false;
                throw;
            }

        }

        public void OnDestroy()
        {
            //JUtil.LogMessage(this, "OnDestroy()");
            rpmComp = null;
            numericInputs = null;
        }

        public override void OnUpdate()
        {
            if (enabled && JUtil.VesselIsInIVA(vessel))
            {
                //JUtil.LogMessage(this, "OnUpdate()");
                double time = Planetarium.GetUniversalTime();
                float change = 0.0f;
                for(int i=0; i<numericInputs.Count; ++i)
                {
                    change += numericInputs[i].Update(time);
                }

                if(change < 0.0f || change > 0.0f)
                {
                    RPMVesselComputer comp = RPMVesselComputer.Instance(vessel);

                    // MOARdV TODO: persistent floats
                    float var = (float)rpmComp.GetVar(perPodPersistenceName);
                    var += change + remainder;

                    if (minRange != null)
                    {
                        float v;
                        if (minRange.Get(out v, comp))
                        {
                            var = Mathf.Max(var, v);
                        }
                    }

                    if (maxRange != null)
                    {
                        float v;
                        if (maxRange.Get(out v, comp))
                        {
                            var = Mathf.Min(var, v);
                        }
                    }

                    if (stepSize > 0.0f)
                    {
                        remainder = var % stepSize;
                        //JUtil.LogMessage(this, "Adjusting {0} to {1} due to stepSize {2}, (remainder {3})",
                        //    var, var-remainder, stepSize, remainder);
                        var -= remainder;
                    }

                    rpmComp.SetVar(perPodPersistenceName, (int)var);
                }
            }
        }

        private class NumericInput
        {
            private float delta = 0.0f;
            private double pressStart = 0.0;
            private double lastUpdate = 0.0;
            private float increment = 0.0f;
            private FloatCurve incrementCurve = null;
            private bool pressed = false;

            internal NumericInput(ConfigNode node, InternalProp internalProp)
            {
                if (!node.HasValue("switchTransform"))
                {
                    throw new Exception("USERINPUTSET missing switchTransform");
                }

                // XNOR!
                if(!(node.HasValue("increment") ^ node.HasNode("incrementCurve")))
                {
                    throw new Exception("USERINPUTSET missing increment or incrementCurve, or it has both");
                }

                if (node.HasValue("increment") && !float.TryParse(node.GetValue("increment"), out increment))
                {
                    throw new Exception("USERINPUTSET bad increment");
                }
                else if (node.HasNode("incrementCurve"))
                {
                    ConfigNode incNode = node.GetNode("incrementCurve");
                    string[] keys = incNode.GetValues("key");
                    incrementCurve = new FloatCurve();
                    for (int i = 0; i < keys.Length; ++i )
                    {
                        string[] values = keys[i].Split(' ');
                        if(values.Length == 2)
                        {
                            incrementCurve.Add(float.Parse(values[0]), float.Parse(values[1]));
                        }
                        else if (values.Length == 4)
                        {
                            incrementCurve.Add(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3]));
                        }
                        else
                        {
                            JUtil.LogErrorMessage(this, "Found a curve key with {0} entries?!?", values.Length);
                        }
                    }
                }

                string switchTransform = node.GetValue("switchTransform");
                if (incrementCurve != null)
                {
                    SmarterButton.CreateButton(internalProp, switchTransform, TimedClick, TimedRelease);
                }
                else
                {
                    SmarterButton.CreateButton(internalProp, switchTransform, Click);
                }
            }

            internal float Update(double currentTime)
            {
                if(pressed)
                {
                    float dT = (float)(currentTime - lastUpdate);
                    float netTime = (float)(currentTime - pressStart);
                    // evaluate curve
                    float rate = incrementCurve.Evaluate(netTime);
                    //JUtil.LogMessage(this, "Evaluating: dT = {0}, netTime = {1}, rate = {2}, change = {3}", dT, netTime, rate, rate*dT);
                    delta += dT * rate;
                    lastUpdate = currentTime;
                }

                float retVal = delta;
                delta = 0.0f;

                return retVal;
            }

            private void Click()
            {
                delta += increment;
                //JUtil.LogMessage(this, "Click!: delta = {0}", delta);
            }

            private void TimedClick()
            {
                lastUpdate = pressStart = Planetarium.GetUniversalTime();
                delta = incrementCurve.Evaluate(0.0f);

                //JUtil.LogMessage(this, "TimedClick! @ {0}", pressStart);
                pressed = true;
            }

            private void TimedRelease()
            {
                //JUtil.LogMessage(this, "TimedRelease!");
                pressed = false;
            }
        }
    }
}
