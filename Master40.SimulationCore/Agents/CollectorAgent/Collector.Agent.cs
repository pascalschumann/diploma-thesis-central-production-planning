﻿using Akka.Actor;
using AkkaSim;
using Master40.DB.Data.Context;
using Master40.MessageSystem.SignalR;
using Master40.SimulationCore.Helper;
using System;
using System.Collections.Generic;

namespace Master40.SimulationCore.Agents
{
    public partial class Collector : SimulationMonitor
    {
        internal long Time => this._Time;
        private ICollectorBehaviour Behaviour;
        internal IMessageHub messageHub { get; }
        internal MasterDBContext DBContext;
        internal new IUntypedActorContext Context => UntypedActor.Context;
        /// <summary>
        /// Collector Agent for Life data Aquesition
        /// </summary>
        /// <param name="actorPaths"></param>
        /// <param name="time">Current time span</param>
        /// <param name="debug">Parameter to activate Debug Messages on Agent level</param>
        public Collector(ActorPaths actorPaths
            , ICollectorBehaviour collectorBehaviour
            , IMessageHub msgHub
            , MasterDBContext dBContext
            , long time
            , bool debug
            , List<Type> streamTypes)
            : base(time, streamTypes)
        {
            Console.WriteLine("I'm alive: " + Self.Path.ToStringWithAddress());
            Behaviour = collectorBehaviour;
            messageHub = msgHub;
            DBContext = dBContext;
        }

        public static Props Props(ActorPaths actorPaths
            , ICollectorBehaviour collectorBehaviour
            , IMessageHub msgHub
            , MasterDBContext dBContext
            , long time
            , bool debug
            , List<Type> streamTypes)
        {
            return Akka.Actor.Props.Create(() => new Collector(actorPaths, collectorBehaviour, msgHub, dBContext, time, debug, streamTypes));
        }

        protected override void EventHandle(object o)
        {
            Behaviour.EventHandle(this, o);
        }


        protected override void PreStart()
        {
            base.PreStart();
        
            //this.Set(Properties.SIMULATION_WORK_ITEMS, new List<SimulationWorkschedule>());
            //this.Set(Properties.WORKTIME_CURRENT_SPAN, new List<Tuple<string, long>>());
            
            // already Handled in Constructor
            //Context.System.EventStream.Subscribe(Self, typeof(SimulationImmutables.CreateSimulationWork));
        }

        
    }
}