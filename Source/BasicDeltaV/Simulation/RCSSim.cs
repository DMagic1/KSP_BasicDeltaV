using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BasicDeltaV.Simulation
{
    public class RCSSim
    {
        private static readonly Pool<RCSSim> pool = new Pool<RCSSim>(Create, Reset);

        public readonly ResourceContainer resourceConsumptions = new ResourceContainer();
        public readonly ResourceContainer resourceFlowModes = new ResourceContainer();

        public double actualThrust = 0;
        public bool isActive = false;
        public double isp = 0;
        public PartSim partSim;
        public List<AppliedForce> appliedForces = new List<AppliedForce>();
        public float maxMach;
        public bool isFlamedOut;
        public bool dontDecoupleActive = true;

        public double thrust = 0;

        // Add thrust vector to account for directional losses
        public Vector3 thrustVec;

        private static RCSSim Create()
        {
            return new RCSSim();
        }

        private static void Reset(RCSSim engineSim)
        {
            engineSim.resourceConsumptions.Reset();
            engineSim.resourceFlowModes.Reset();
            engineSim.partSim = null;
            engineSim.actualThrust = 0;
            engineSim.isActive = false;
            engineSim.isp = 0;
            for (int i = 0; i < engineSim.appliedForces.Count; i++)
            {
                engineSim.appliedForces[i].Release();
            }
            engineSim.appliedForces.Clear();
            engineSim.thrust = 0;
            engineSim.maxMach = 0f;
            engineSim.isFlamedOut = false;
        }

        public void Release()
        {
            pool.Release(this);
        }

        public static RCSSim New(PartSim theEngine,
                                    ModuleRCS engineMod,
                                    double atmosphere,
                                    float machNumber,
                                    bool vectoredThrust,
                                    bool fullThrust,
                                    LogMsg log)
        {
            double maxFuelFlow = engineMod.maxFuelFlow;
            float thrustPercentage = engineMod.thrustPercentage;
            List<Transform> thrustTransforms = engineMod.thrusterTransforms;
            Vector3 vecThrust = CalculateThrustVector(vectoredThrust ? thrustTransforms : null, log);
            FloatCurve atmosphereCurve = engineMod.atmosphereCurve;
            double IspG = engineMod.G;
            List<Propellant> propellants = engineMod.propellants;
            bool active = engineMod.moduleIsEnabled;
            bool isFlamedOut = engineMod.flameout;

            RCSSim engineSim = pool.Borrow();

            engineSim.isp = 0.0;
            engineSim.maxMach = 0.0f;
            engineSim.actualThrust = 0.0;
            engineSim.partSim = theEngine;
            engineSim.isActive = active;
            engineSim.thrustVec = vecThrust;
            engineSim.isFlamedOut = isFlamedOut;
            engineSim.resourceConsumptions.Reset();
            engineSim.resourceFlowModes.Reset();
            engineSim.appliedForces.Clear();

            double flowRate = 0.0;
            if (engineSim.partSim.hasVessel)
            {
                if (log != null)
                    log.AppendLine("hasVessel is true");

                engineSim.isp = atmosphereCurve.Evaluate((float)atmosphere);
                engineSim.thrust = GetThrust(maxFuelFlow, engineSim.isp);
                engineSim.actualThrust = engineSim.isActive ? engineSim.thrust : 0.0;

                if (log != null)
                {
                    log.buf.AppendFormat("isp     = {0:g6}\n", engineSim.isp);
                    log.buf.AppendFormat("thrust  = {0:g6}\n", engineSim.thrust);
                    log.buf.AppendFormat("actual  = {0:g6}\n", engineSim.actualThrust);
                }

                if (log != null)
                    log.AppendLine("throttleLocked is true, using thrust for flowRate");

                flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
            }
            else
            {
                if (log != null) log.buf.AppendLine("hasVessel is false");
                engineSim.isp = atmosphereCurve.Evaluate((float)atmosphere);
                engineSim.thrust = GetThrust(maxFuelFlow, engineSim.isp);
                engineSim.actualThrust = 0d;
                if (log != null)
                {
                    log.buf.AppendFormat("isp     = {0:g6}\n", engineSim.isp);
                    log.buf.AppendFormat("thrust  = {0:g6}\n", engineSim.thrust);
                    log.buf.AppendFormat("actual  = {0:g6}\n", engineSim.actualThrust);
                    log.AppendLine("no vessel, using thrust for flowRate");
                }

                flowRate = GetFlowRate(engineSim.thrust, engineSim.isp);
            }

            if (log != null)
                log.buf.AppendFormat("flowRate = {0:g6}\n", flowRate);

            float flowMass = 0f;
            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];
                if (!propellant.ignoreForIsp)
                    flowMass += propellant.ratio * ResourceContainer.GetResourceDensity(propellant.id);
            }

            if (log != null)
                log.buf.AppendFormat("flowMass = {0:g6}\n", flowMass);

            for (int i = 0; i < propellants.Count; ++i)
            {
                Propellant propellant = propellants[i];

                if (propellant.name == "ElectricCharge" || propellant.name == "IntakeAir")
                {
                    continue;
                }

                double consumptionRate = propellant.ratio * flowRate / flowMass;
                if (log != null) log.buf.AppendFormat(
                        "Add consumption({0}, {1}:{2:d}) = {3:g6}\n",
                        ResourceContainer.GetResourceName(propellant.id),
                        theEngine.name,
                        theEngine.partId,
                        consumptionRate);
                engineSim.resourceConsumptions.Add(propellant.id, consumptionRate);
                engineSim.resourceFlowModes.Add(propellant.id, (double)propellant.GetFlowMode());
            }

            for (int i = 0; i < thrustTransforms.Count; i++)
            {
                Transform thrustTransform = thrustTransforms[i];
                Vector3d direction = thrustTransform.forward.normalized;
                Vector3d position = thrustTransform.position;

                AppliedForce appliedForce = AppliedForce.New(direction * engineSim.thrust, position);
                engineSim.appliedForces.Add(appliedForce);
            }

            return engineSim;
        }

        private static Vector3 CalculateThrustVector(List<Transform> thrustTransforms, LogMsg log)
        {
            if (thrustTransforms == null)
            {
                return Vector3.forward;
            }

            Vector3 thrustvec = Vector3.zero;
            for (int i = 0; i < thrustTransforms.Count; ++i)
            {
                Transform trans = thrustTransforms[i];

                if (log != null)
                    log.buf.AppendFormat("Transform = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n", trans.forward.x, trans.forward.y, trans.forward.z, trans.forward.magnitude);

                thrustvec -= (trans.forward);
            }

            if (log != null)
                log.buf.AppendFormat("ThrustVec  = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n", thrustvec.x, thrustvec.y, thrustvec.z, thrustvec.magnitude);

            thrustvec.Normalize();

            if (log != null)
                log.buf.AppendFormat("ThrustVecN = ({0:g6}, {1:g6}, {2:g6})   length = {3:g6}\n", thrustvec.x, thrustvec.y, thrustvec.z, thrustvec.magnitude);

            return thrustvec;
        }

        public static double GetExhaustVelocity(double isp)
        {
            return isp * BasicDeltaV.GRAVITY;
        }

        public static float GetFlowModifier(bool atmChangeFlow, FloatCurve atmCurve, double atmDensity, FloatCurve velCurve, float machNumber, ref float maxMach)
        {
            float flowModifier = 1.0f;
            if (atmChangeFlow)
            {
                flowModifier = (float)(atmDensity / 1.225);
                if (atmCurve != null)
                {
                    flowModifier = atmCurve.Evaluate(flowModifier);
                }
            }
            if (velCurve != null)
            {
                flowModifier = flowModifier * velCurve.Evaluate(machNumber);
                maxMach = velCurve.maxTime;
            }
            if (flowModifier < float.Epsilon)
            {
                flowModifier = float.Epsilon;
            }
            return flowModifier;
        }

        public static double GetFlowRate(double thrust, double isp)
        {
            return thrust / GetExhaustVelocity(isp);
        }

        public static float GetThrottlePercent(float currentThrottle, float thrustPercentage)
        {
            return currentThrottle * GetThrustPercent(thrustPercentage);
        }

        public static double GetThrust(double flowRate, double isp)
        {
            return flowRate * GetExhaustVelocity(isp);
        }

        public static float GetThrustPercent(float thrustPercentage)
        {
            return thrustPercentage * 0.01f;
        }

        public void DumpEngineToLog(LogMsg log)
        {
            if (log != null) log.buf.AppendFormat("RCS: [thrust = {0:g6}, actual = {1:g6}, isp = {2:g6}\n", thrust, actualThrust, isp);
        }

        // A dictionary to hold a set of parts for each resource
        Dictionary<int, HashSet<PartSim>> sourcePartSets = new Dictionary<int, HashSet<PartSim>>();

        Dictionary<int, HashSet<PartSim>> stagePartSets = new Dictionary<int, HashSet<PartSim>>();

        HashSet<PartSim> visited = new HashSet<PartSim>();

        public void DumpSourcePartSets(LogMsg log, String msg)
        {
            if (log == null)
                return;

            //log.AppendLine("DumpSourcePartSets ", msg);
            foreach (int type in sourcePartSets.Keys)
            {
                //log.AppendLine("SourcePartSet for ", ResourceContainer.GetResourceName(type));
                HashSet<PartSim> sourcePartSet = sourcePartSets[type];
                //if (sourcePartSet.Count > 0)
                //{
                //    foreach (PartSim partSim in sourcePartSet)
                //    {
                //        log.AppendLine("Part ", partSim.name, ":", partSim.partId);
                //    }
                //}
                //else
                //{
                //    log.AppendLine("No parts");
                //}
            }
        }
    }
}
