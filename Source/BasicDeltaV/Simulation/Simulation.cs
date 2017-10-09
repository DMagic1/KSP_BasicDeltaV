﻿// 
//     Code From Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CompoundParts;
using KSP.UI.Screens;
using UnityEngine;

namespace BasicDeltaV.Simulation
{
	public class Simulation
	{
		private const double GRAVITY = 9.80665;
		private const double SECONDS_PER_DAY = 86400;
		private readonly Stopwatch _timer;
		private List<EngineSim> activeEngines;
		private List<EngineSim> allEngines;
		private List<PartSim> allFuelLines;
		private List<PartSim> allParts;
		private double atmosphere;
		private int currentStage;
		private double currentisp;
		private HashSet<PartSim> decoupledParts;
		private bool doingCurrent;
		private List<PartSim> dontStageParts;
		private List<List<PartSim>> dontStagePartsLists;
		private HashSet<PartSim> drainingParts;
		private HashSet<int> drainingResources;
		private double gravity;
		// A dictionary for fast lookup of Part->PartSim during the preparation phase
		private Dictionary<Part, PartSim> partSimLookup;
		private LogMsg log;

		private int lastStage;
		private List<Part> partList;
		private double simpleTotalThrust;
		private double stageStartMass;
		private Vector3d stageStartCom;
		private double stageTime;
		private double stepEndMass;
		private double stepStartMass;
		private double totalStageActualThrust;
		private double totalStageFlowRate;
		private double totalStageIspFlowRate;
		private double totalStageThrust;
		private ForceAccumulator totalStageThrustForce;
		private Vector3 vecActualThrust;
		private Vector3 vecStageDeltaV;
		private Vector3 vecThrust;
		private double mach;
		private float maxMach;
		public String vesselName;
		public VesselType vesselType;
		private WeightedVectorAverager vectorAverager;

		public Simulation()
		{
			_timer = new Stopwatch();
			activeEngines = new List<EngineSim>();
			allEngines = new List<EngineSim>();
			allFuelLines = new List<PartSim>();
			allParts = new List<PartSim>();
			decoupledParts = new HashSet<PartSim>();
			dontStagePartsLists = new List<List<PartSim>>();
			drainingParts = new HashSet<PartSim>();
			drainingResources = new HashSet<int>();
			partSimLookup = new Dictionary<Part, PartSim>();
			partList = new List<Part>();
			totalStageThrustForce = new ForceAccumulator();
			vectorAverager = new WeightedVectorAverager();
		}

		private double ShipMass
		{
			get
			{
				double mass = 0d;

				for (int i = 0; i < allParts.Count; ++i)
				{
					mass += allParts[i].GetMass(currentStage);
				}

				return mass;
			}
		}

		private Vector3d ShipCom
		{
			get
			{
				vectorAverager.Reset();

				for (int i = 0; i < allParts.Count; ++i)
				{
					PartSim partSim = allParts[i];
					vectorAverager.Add(partSim.centerOfMass, partSim.GetMass(currentStage, true));
				}

				return vectorAverager.Get();
			}
		}

		// This function prepares the simulation by creating all the necessary data structures it will 
		// need during the simulation.  All required data is copied from the core game data structures 
		// so that the simulation itself can be run in a background thread without having issues with 
		// the core game changing the data while the simulation is running.
		public bool PrepareSimulation(LogMsg _log, List<Part> parts, double theGravity, double theAtmosphere = 0, double theMach = 0, bool dumpTree = false, bool vectoredThrust = false, bool fullThrust = false)
		{
			log = _log;
			if (log != null) log.AppendLine("PrepareSimulation started");

			_timer.Reset();
			_timer.Start();

			// Store the parameters in members for ease of access in other functions
			partList = parts;
			gravity = theGravity;
			atmosphere = theAtmosphere;
			mach = theMach;
			lastStage = StageManager.LastStage;
			maxMach = 1.0f;
			if (log != null) log.AppendLine("lastStage = ", lastStage);

			// Clear the lists for our simulation parts
			allParts.Clear();
			allFuelLines.Clear();
			drainingParts.Clear();
			allEngines.Clear();
			activeEngines.Clear();
			drainingResources.Clear();

			// A dictionary for fast lookup of Part->PartSim during the preparation phase
			partSimLookup.Clear();

			if (partList.Count > 0 && partList[0].vessel != null)
			{
				vesselName = partList[0].vessel.vesselName;
				vesselType = partList[0].vessel.vesselType;
			}
			// First we create a PartSim for each Part (giving each a unique id)
			int partId = 1;
			for (int i = 0; i < partList.Count; ++i)
			{
				Part part = partList[i];

				// If the part is already in the lookup dictionary then log it and skip to the next part
				if (partSimLookup.ContainsKey(part))
				{
					if (log != null) log.AppendLine("Part ", part.name, " appears in vessel list more than once");
					continue;
				}

				// Create the PartSim
				PartSim partSim = PartSim.New(part, partId, atmosphere, log);

				// Add it to the Part lookup dictionary and the necessary lists
				partSimLookup.Add(part, partSim);
				allParts.Add(partSim);
				if (partSim.isFuelLine)
				{
					allFuelLines.Add(partSim);
				}
				if (partSim.isEngine)
				{
					partSim.CreateEngineSims(allEngines, atmosphere, mach, vectoredThrust, fullThrust, log);
				}

				partId++;
			}

			for (int i = 0; i < allEngines.Count; ++i)
			{
				maxMach = Mathf.Max(maxMach, allEngines[i].maxMach);
			}

			UpdateActiveEngines();

			// Now that all the PartSims have been created we can do any set up that needs access to other parts
			// First we set up all the parent links
			for (int i = 0; i < allParts.Count; i++)
			{
				PartSim partSim = allParts[i];
				partSim.SetupParent(partSimLookup, log);
			}

			// Then, in the VAB/SPH, we add the parent of each fuel line to the fuelTargets list of their targets
			if (HighLogic.LoadedSceneIsEditor)
			{
				for (int i = 0; i < allFuelLines.Count; ++i)
				{
					PartSim partSim = allFuelLines[i];

					CModuleFuelLine fuelLine = partSim.part.GetModule<CModuleFuelLine>();
					if (fuelLine.target != null)
					{
						PartSim targetSim;
						if (partSimLookup.TryGetValue(fuelLine.target, out targetSim))
						{
							if (log != null) log.AppendLine("Fuel line target is ", targetSim.name, ":", targetSim.partId);

							targetSim.fuelTargets.Add(partSim.parent);
						}
						else
						{
							if (log != null) log.AppendLine("No PartSim for fuel line target (", partSim.part.partInfo.name, ")");
						}
					}
					else
					{
						if (log != null) log.AppendLine("Fuel line target is null");
					}
				}
			}

			if (log != null) log.AppendLine("SetupAttachNodes and count stages");
			for (int i = 0; i < allParts.Count; ++i)
			{
				PartSim partSim = allParts[i];

				partSim.SetupAttachNodes(partSimLookup, log);
				if (partSim.decoupledInStage >= lastStage)
				{
					lastStage = partSim.decoupledInStage + 1;
				}
			}

			// And finally release the Part references from all the PartSims
			if (log != null) log.AppendLine("ReleaseParts");
			for (int i = 0; i < allParts.Count; ++i)
			{
				allParts[i].ReleasePart();
			}

			// And dereference the core's part list
			partList = null;

			_timer.Stop();
			if (log != null)
			{
				log.AppendLine("PrepareSimulation: ", _timer.ElapsedMilliseconds, "ms");
				log.Flush();
			}

			Dump();
			log = null;

			return true;
		}

		// This function runs the simulation and returns a newly created array of Stage objects
		public Stage[] RunSimulation(LogMsg _log)
		{
			log = _log;
			if (log != null) log.AppendLine("RunSimulation started");

			_timer.Reset();
			_timer.Start();

			// Start with the last stage to simulate
			// (this is in a member variable so it can be accessed by AllowedToStage and ActivateStage)
			currentStage = lastStage;
			// Work out which engines would be active if just doing the staging and if this is different to the 
			// currently active engines then generate an extra stage
			// Loop through all the engines
			bool anyActive = false;
			for (int i = 0; i < allEngines.Count; ++i)
			{
				EngineSim engine = allEngines[i];

				if (log != null) log.AppendLine("Testing engine mod of ", engine.partSim.name, ":", engine.partSim.partId);

				bool bActive = engine.isActive;
				bool bStage = (engine.partSim.inverseStage >= currentStage);
				if (log != null) log.AppendLine("bActive = ", bActive, "   bStage = ", bStage);
				if (HighLogic.LoadedSceneIsFlight)
				{
					if (bActive)
					{
						anyActive = true;
					}
					if (bActive != bStage)
					{
						// If the active state is different to the state due to staging
						if (log != null) log.AppendLine("Need to do current active engines first");
						doingCurrent = true;
					}
				}
				else
				{
					if (bStage)
					{
						if (log != null) log.AppendLine("Marking as active");
						engine.isActive = true;
					}
				}
			}

			// If we need to do current because of difference in engine activation and there actually are active engines
			// then we do the extra stage otherwise activate the next stage and don't treat it as current
			if (doingCurrent && anyActive)
			{
				currentStage++;
			}
			else
			{
				ActivateStage();
				doingCurrent = false;
			}

			// Create a list of lists of PartSims that prevent decoupling
			BuildDontStageLists(log);

			if (log != null) log.Flush();

			// Create the array of stages that will be returned
			Stage[] stages = new Stage[currentStage + 1];

			int startStage = currentStage;

			// Loop through the stages
			while (currentStage >= 0)
			{
				if (log != null)
				{
					log.AppendLine("Simulating stage ", currentStage);
					log.Flush();
					_timer.Reset();
					_timer.Start();
				}

				// Update active engines and resource drains
				UpdateResourceDrains();

				// Update the masses of the parts to correctly handle "no physics" parts
				stageStartMass = UpdatePartMasses();

				if (log != null)
					allParts[0].DumpPartToLog(log, "", allParts);

				// Create the Stage object for this stage
				Stage stage = new Stage();

				stageTime = 0d;
				vecStageDeltaV = Vector3.zero;

				stageStartCom = ShipCom;

				stepStartMass = stageStartMass;
				stepEndMass = 0;

				CalculateThrustAndISP();

				// Store various things in the Stage object
				stage.thrust = totalStageThrust;
				stage.thrustToWeight = totalStageThrust / (stageStartMass * gravity);
				stage.maxThrustToWeight = stage.thrustToWeight;
				stage.actualThrust = totalStageActualThrust;
				stage.actualThrustToWeight = totalStageActualThrust / (stageStartMass * gravity);
				if (log != null)
				{
					log.AppendLine("stage.thrust = ", stage.thrust);
					log.AppendLine("StageMass = ", stageStartMass);
					log.AppendLine("Initial maxTWR = ", stage.maxThrustToWeight);
				}

				// calculate torque and associates
				stage.maxThrustTorque = totalStageThrustForce.TorqueAt(stageStartCom).magnitude;

				// torque divided by thrust. imagine that all engines are at the end of a lever that tries to turn the ship.
				// this numerical value, in meters, would represent the length of that lever.
				double torqueLeverArmLength = (stage.thrust <= 0) ? 0 : stage.maxThrustTorque / stage.thrust;

				// how far away are the engines from the CoM, actually?
				double thrustDistance = (stageStartCom - totalStageThrustForce.GetAverageForceApplicationPoint()).magnitude;

				// the combination of the above two values gives an approximation of the offset angle.
				double sinThrustOffsetAngle = 0;
				if (thrustDistance > 1e-7)
				{
					sinThrustOffsetAngle = torqueLeverArmLength / thrustDistance;
					if (sinThrustOffsetAngle > 1)
					{
						sinThrustOffsetAngle = 1;
					}
				}

				stage.thrustOffsetAngle = Math.Asin(sinThrustOffsetAngle) * 180 / Math.PI;

				// Calculate the total cost of the vessel at this point
				stage.totalCost = 0d;
				for (int i = 0; i < allParts.Count; ++i)
				{
					if (currentStage > allParts[i].decoupledInStage)
						stage.totalCost += allParts[i].GetCost(currentStage);
				}

				// The total mass is simply the mass at the start of the stage
				stage.totalMass = stageStartMass;

				// If we have done a previous stage
				if (currentStage < startStage)
				{
					// Calculate what the previous stage's mass and cost were by subtraction
					Stage prev = stages[currentStage + 1];
					prev.cost = prev.totalCost - stage.totalCost;
					prev.mass = prev.totalMass - stage.totalMass;
				}

				// The above code will never run for the last stage so set those directly
				if (currentStage == 0)
				{
					stage.cost = stage.totalCost;
					stage.mass = stage.totalMass;
				}

				dontStageParts = dontStagePartsLists[currentStage];

				if (log != null)
				{
					log.AppendLine("Stage setup took ", _timer.ElapsedMilliseconds, "ms");

					if (dontStageParts.Count > 0)
					{
						log.AppendLine("Parts preventing staging:");
						for (int i = 0; i < dontStageParts.Count; i++)
						{
							PartSim partSim = dontStageParts[i];
							partSim.DumpPartToLog(log, "");
						}
					}
					else
					{
						log.AppendLine("No parts preventing staging");
					}

					log.Flush();
				}


				// Now we will loop until we are allowed to stage
				int loopCounter = 0;
				while (!AllowedToStage())
				{
					loopCounter++;
					//if (log != null) log.AppendLine("loop = ", loopCounter);
					// Calculate how long each draining tank will take to drain and run for the minimum time
					double resourceDrainTime = double.MaxValue;
					PartSim partMinDrain = null;
					foreach (PartSim partSim in drainingParts)
					{
						double time = partSim.TimeToDrainResource(log);
						if (time < resourceDrainTime)
						{
							resourceDrainTime = time;
							partMinDrain = partSim;
						}
					}

					if (log != null) log.Append("Drain time = ", resourceDrainTime, " (", partMinDrain.name)
										.AppendLine(":", partMinDrain.partId, ")");
					foreach (PartSim partSim in drainingParts)
					{
						partSim.DrainResources(resourceDrainTime, log);
					}

					// Get the mass after draining
					stepEndMass = ShipMass;
					stageTime += resourceDrainTime;

					double stepEndTWR = totalStageThrust / (stepEndMass * gravity);
					/*if (log != null)
					{
						log.AppendLine("After drain mass = ", stepEndMass);
						log.AppendLine("currentThrust = ", totalStageThrust);
						log.AppendLine("currentTWR = ", stepEndTWR);
					}*/
					if (stepEndTWR > stage.maxThrustToWeight)
					{
						stage.maxThrustToWeight = stepEndTWR;
					}

					//if (log != null) log.AppendLine("newMaxTWR = ", stage.maxThrustToWeight);

					// If we have drained anything and the masses make sense then add this step's deltaV to the stage total
					if (resourceDrainTime > 0d && stepStartMass > stepEndMass && stepStartMass > 0d && stepEndMass > 0d)
					{
						vecStageDeltaV += vecThrust * (float)((currentisp * GRAVITY * Math.Log(stepStartMass / stepEndMass)) / simpleTotalThrust);
					}

					// Update the active engines and resource drains for the next step
					UpdateResourceDrains();

					// Recalculate the current thrust and isp for the next step
					CalculateThrustAndISP();

					// Check if we actually changed anything
					if (stepStartMass == stepEndMass)
					{
						//MonoBehaviour.print("No change in mass");
						break;
					}

					// Check to stop rampant looping
					if (loopCounter == 1000)
					{
						if (log != null)
						{
							log.AppendLine("exceeded loop count");
							log.AppendLine("stageStartMass = " + stageStartMass);
							log.AppendLine("stepStartMass = " + stepStartMass);
							log.AppendLine("StepEndMass   = " + stepEndMass);
						}
						break;
					}

					// The next step starts at the mass this one ended at
					stepStartMass = stepEndMass;
				}

				// Store more values in the Stage object and stick it in the array

				// Store the magnitude of the deltaV vector
				stage.deltaV = vecStageDeltaV.magnitude;
				stage.resourceMass = stageStartMass - stepEndMass;

				// Recalculate effective stage isp from the stage deltaV (flip the standard deltaV calculation around)
				// Note: If the mass doesn't change then this is a divide by zero
				if (stageStartMass != stepStartMass)
				{
					stage.isp = stage.deltaV / (GRAVITY * Math.Log(stageStartMass / stepStartMass));
				}
				else
				{
					stage.isp = 0;
				}

				// Zero stage time if more than a day (this should be moved into the window code)
				stage.time = (stageTime < SECONDS_PER_DAY) ? stageTime : 0d;
				stage.number = doingCurrent ? -1 : currentStage; // Set the stage number to -1 if doing current engines
				stage.totalPartCount = allParts.Count;
				stage.maxMach = maxMach;
				stages[currentStage] = stage;

				// Now activate the next stage
				currentStage--;
				doingCurrent = false;

				if (log != null)
				{
					// Log how long the stage took
					_timer.Stop();
					log.AppendLine("Simulating stage took ", _timer.ElapsedMilliseconds, "ms");
					stage.Dump(log);
					_timer.Reset();
					_timer.Start();
				}

				// Activate the next stage
				ActivateStage();

				if (log != null)
				{
					// Log how long it took to activate
					_timer.Stop();
					log.AppendLine("ActivateStage took ", _timer.ElapsedMilliseconds, "ms");
				}
			}

			// Now we add up the various total fields in the stages
			for (int i = 0; i < stages.Length; i++)
			{
				// For each stage we total up the cost, mass, deltaV and time for this stage and all the stages above
				for (int j = i; j >= 0; j--)
				{
					stages[i].totalDeltaV += stages[j].deltaV;
					stages[i].totalTime += stages[j].time;
					stages[i].partCount = i > 0 ? stages[i].totalPartCount - stages[i - 1].totalPartCount : stages[i].totalPartCount;
				}
				// We also total up the deltaV for stage and all stages below
				for (int j = i; j < stages.Length; j++)
				{
					stages[i].inverseTotalDeltaV += stages[j].deltaV;
				}

				// Zero the total time if the value will be huge (24 hours?) to avoid the display going weird
				// (this should be moved into the window code)
				if (stages[i].totalTime > SECONDS_PER_DAY)
				{
					stages[i].totalTime = 0d;
				}
			}

			FreePooledObject();

			_timer.Stop();

			if (log != null)
			{
				log.AppendLine("RunSimulation: ", _timer.ElapsedMilliseconds, "ms");
				log.Flush();
			}
			log = null;

			return stages;
		}

		public double UpdatePartMasses()
		{
			for (int i = 0; i < allParts.Count; i++)
			{
				allParts[i].baseMass = allParts[i].realMass;
				allParts[i].baseMassForCoM = allParts[i].realMass;
			}

			for (int i = 0; i < allParts.Count; i++)
			{
				PartSim part = allParts[i];

				// Check if part should pass it's mass onto its parent.
				if (part.isNoPhysics && part.parent != null)
				{
					PartSim partParent = part.parent;

					// Loop through all parents until a physically significant parent is found.
					while (partParent != null)
					{
						// Check if parent is physically significant.
						if (partParent.isNoPhysics == false)
						{
							// Apply the mass to the parent and remove it from the originating part.
							partParent.baseMassForCoM += part.baseMassForCoM;
							part.baseMassForCoM = 0.0;

							// Break out of the recursive loop.
							break;
						}

						// Recursively loop through the parent parts.
						partParent = partParent.parent;
					}
				}
			}

			double totalMass = 0d;
			for (int i = 0; i < allParts.Count; i++)
			{
				totalMass += allParts[i].startMass = allParts[i].GetMass(currentStage);
			}

			return totalMass;
		}

		// Make sure we free them all, even if they should all be free already at this point
		public void FreePooledObject()
		{
			//MonoBehaviour.print("FreePooledObject pool size before = " + PartSim.pool.Count() + " for " + allParts.Count + " parts");
			foreach (PartSim part in allParts)
			{
				part.Release();
			}
			//MonoBehaviour.print("FreePooledObject pool size after = " + PartSim.pool.Count());

			//MonoBehaviour.print("FreePooledObject pool size before = " + EngineSim.pool.Count() + " for " + allEngines.Count + " engines");
			foreach (EngineSim engine in allEngines)
			{
				engine.Release();
			}
			//MonoBehaviour.print("FreePooledObject pool size after = " + EngineSim.pool.Count());
		}

		private void BuildDontStageLists(LogMsg log)
		{
			if (log != null) log.AppendLine("Creating list with capacity of ", (currentStage + 1));

			dontStagePartsLists.Clear();
			for (int i = 0; i <= currentStage; i++)
			{
				if (i < dontStagePartsLists.Count)
				{
					dontStagePartsLists[i].Clear();
				}
				else
				{
					dontStagePartsLists.Add(new List<PartSim>());
				}
			}

			for (int i = 0; i < allParts.Count; ++i)
			{
				PartSim partSim = allParts[i];

				if (partSim.isEngine || !partSim.Resources.Empty)
				{
					if (log != null) log.AppendLine(partSim.name, ":", partSim.partId, " is engine or tank, decoupled = ", partSim.decoupledInStage);

					if (partSim.decoupledInStage < -1 || partSim.decoupledInStage > currentStage - 1)
					{
						if (log != null) log.AppendLine("decoupledInStage out of range");
					}
					else
					{
						dontStagePartsLists[partSim.decoupledInStage + 1].Add(partSim);
					}
				}
			}

			for (int i = 1; i <= lastStage; i++)
			{
				if (dontStagePartsLists[i].Count == 0)
				{
					dontStagePartsLists[i] = dontStagePartsLists[i - 1];
				}
			}
		}

		// This function simply rebuilds the active engines by testing the isActive flag of all the engines
		private void UpdateActiveEngines()
		{
			activeEngines.Clear();
			for (int i = 0; i < allEngines.Count; ++i)
			{
				EngineSim engine = allEngines[i];
				if (engine.isActive && engine.isFlamedOut == false)
				{
					activeEngines.Add(engine);
				}
			}
		}

		private void CalculateThrustAndISP()
		{
			// Reset all the values
			vecThrust = Vector3.zero;
			vecActualThrust = Vector3.zero;
			simpleTotalThrust = 0d;
			totalStageThrust = 0d;
			totalStageActualThrust = 0d;
			totalStageFlowRate = 0d;
			totalStageIspFlowRate = 0d;
			totalStageThrustForce.Reset();

			// Loop through all the active engines totalling the thrust, actual thrust and mass flow rates
			// The thrust is totalled as vectors
			for (int i = 0; i < activeEngines.Count; ++i)
			{
				EngineSim engine = activeEngines[i];

				simpleTotalThrust += engine.thrust;
				vecThrust += ((float)engine.thrust * engine.thrustVec);
				vecActualThrust += ((float)engine.actualThrust * engine.thrustVec);

				totalStageFlowRate += engine.ResourceConsumptions.Mass;
				totalStageIspFlowRate += engine.ResourceConsumptions.Mass * engine.isp;

				for (int j = 0; j < engine.appliedForces.Count; ++j)
				{
					totalStageThrustForce.AddForce(engine.appliedForces[j]);
				}
			}
			if (log != null) log.AppendLine("vecThrust = ", vecThrust.ToString(), "   magnitude = ", vecThrust.magnitude);
			totalStageThrust = vecThrust.magnitude;
			totalStageActualThrust = vecActualThrust.magnitude;

			// Calculate the effective isp at this point
			if (totalStageFlowRate > 0d && totalStageIspFlowRate > 0d)
			{
				currentisp = totalStageIspFlowRate / totalStageFlowRate;
			}
			else
			{
				currentisp = 0;
			}
		}

		// This function does all the hard work of working out which engines are burning, which tanks are being drained 
		// and setting the drain rates
		private void UpdateResourceDrains()
		{
			// Update the active engines
			UpdateActiveEngines();

			// Empty the draining resources set
			drainingResources.Clear();

			// Reset the resource drains of all draining parts
			foreach (PartSim partSim in drainingParts)
			{
				partSim.ResourceDrains.Reset();
			}

			// Empty the draining parts set
			drainingParts.Clear();

			// Loop through all the active engine modules
			for (int i = 0; i < activeEngines.Count; ++i)
			{
				EngineSim engine = activeEngines[i];

				// Set the resource drains for this engine
				if (engine.SetResourceDrains(log, allParts, allFuelLines, drainingParts))
				{
					// If it is active then add the consumed resource types to the set
					for (int j = 0; j < engine.ResourceConsumptions.Types.Count; ++j)
					{
						drainingResources.Add(engine.ResourceConsumptions.Types[j]);
					}
				}
			}

			// Update the active engines again to remove any engines that have no fuel supply
			UpdateActiveEngines();

			if (log != null)
			{
				log.AppendLine("Active engines = ", activeEngines.Count);
				int i = 0;
				for (int j = 0; j < activeEngines.Count; j++)
				{
					EngineSim engine = activeEngines[j];
					log.Append("Engine " + (i++) + ":");
					engine.DumpEngineToLog(log);
				}
				log.Flush();
			}
		}

		// This function works out if it is time to stage
		private bool AllowedToStage()
		{
			if (log != null) log.AppendLine("AllowedToStage")
								.AppendLine("currentStage = ", currentStage);

			if (activeEngines.Count > 0)
			{
				for (int i = 0; i < dontStageParts.Count; ++i)
				{
					PartSim partSim = dontStageParts[i];

					if (log != null) partSim.DumpPartToLog(log, "Testing: ");
					//if (log != null) log.AppendLine("isSepratron = ", partSim.isSepratron ? "true" : "false");

					if (!partSim.isSepratron && !partSim.EmptyOf(drainingResources))
					{
						if (log != null) partSim.DumpPartToLog(log, "Decoupled part not empty => false: ");
						return false;
					}

					if (partSim.isEngine)
					{
						for (int j = 0; j < activeEngines.Count; ++j)
						{
							EngineSim engine = activeEngines[j];

							if (engine.dontDecoupleActive && engine.partSim == partSim)
							{
								if (log != null) partSim.DumpPartToLog(log, "Decoupled part is active engine => false: ");
								return false;
							}
						}
					}
				}
			}

			if (currentStage == 0 && doingCurrent)
			{
				if (log != null) log.AppendLine("Current stage == 0 && doingCurrent => false");
				return false;
			}

			if (log != null) log.AppendLine("Returning true");
			return true;
		}

		// This function activates the next stage
		// currentStage must be updated before calling this function
		private void ActivateStage()
		{
			// Build a set of all the parts that will be decoupled
			decoupledParts.Clear();
			for (int i = 0; i < allParts.Count; ++i)
			{
				PartSim partSim = allParts[i];

				if (partSim.decoupledInStage >= currentStage)
				{
					decoupledParts.Add(partSim);
				}
			}

			foreach (PartSim partSim in decoupledParts)
			{
				// Remove it from the all parts list
				allParts.Remove(partSim);
				partSim.Release();
				if (partSim.isEngine)
				{
					// If it is an engine then loop through all the engine modules and remove all the ones from this engine part
					for (int i = allEngines.Count - 1; i >= 0; i--)
					{
						EngineSim engine = allEngines[i];
						if (engine.partSim == partSim)
						{
							allEngines.RemoveAt(i);
							engine.Release();
						}
					}
				}
				// If it is a fuel line then remove it from the list of all fuel lines
				if (partSim.isFuelLine)
				{
					allFuelLines.Remove(partSim);
				}
			}

			// Loop through all the (remaining) parts
			for (int i = 0; i < allParts.Count; ++i)
			{
				// Ask the part to remove all the parts that are decoupled
				allParts[i].RemoveAttachedParts(decoupledParts);
			}

			// Now we loop through all the engines and activate those that are ignited in this stage
			for (int i = 0; i < allEngines.Count; ++i)
			{
				EngineSim engine = allEngines[i];
				if (engine.partSim.inverseStage == currentStage)
				{
					engine.isActive = true;
				}
			}
		}

		public void Dump()
		{
			if (log == null)
				return;

			log.AppendLine("Part count = ", allParts.Count);

			// Output a nice tree view of the rocket
			if (allParts.Count > 0)
			{
				PartSim root = allParts[0];
				while (root.parent != null)
				{
					root = root.parent;
				}

				if (root.hasVessel)
				{
					log.Append("vesselName = '", vesselName, "'  vesselType = ", SimManager.GetVesselTypeString(vesselType));
				}

				root.DumpPartToLog(log, "", allParts);
			}

			log.Flush();
		}
	}
}
