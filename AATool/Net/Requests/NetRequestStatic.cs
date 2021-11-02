﻿using System.Collections.Generic;
using System.Linq;
using AATool.Utilities;

namespace AATool.Net
{
    public abstract partial class NetRequest
    {
        private static readonly Queue<NetRequest> Pending = new ();
        private static readonly List<NetRequest> TimedOut = new ();
        private static readonly HashSet<string> Abandoned = new ();
        private static readonly HashSet<string> Completed = new ();
        private static readonly HashSet<string> Active    = new ();
        private static readonly Timer RequestDelay = new (Protocol.REQUEST_NEXT_COOLDOWN);

        public static void Enqueue(NetRequest request)
        {
            //add request to pending queue if unique
            if (!AlreadySubmitted(request.Url))
                Pending.Enqueue(request);
        }

        public static void Update(Time time)
        {
            UpdateTimeouts(time);

            RequestDelay.Update(time);
            if (RequestDelay.IsExpired)
            {
                RequestDelay.Reset();   
                UpdatePending();
            }
        }

        private static bool AlreadySubmitted(string url)
        {
            //check against urls
            if (Completed.Contains(url) || Abandoned.Contains(url) || Active.Contains(url))
                return true;

            //check against pending requests
            foreach (NetRequest request in Pending.Union(TimedOut))
            {
                if (url == request.Url)
                    return true;
            }

            //url nowhere in pipeline
            return false;
        }

        private static void UpdateTimeouts(Time time)
        {
            for (int i = TimedOut.Count - 1; i >= 0 ; i--)
            {
                NetRequest request = TimedOut[i];
                request.UpdateCooldown(time);
                if (!request.IsStillTimedOut)
                {
                    //move back into pending queue
                    TimedOut.RemoveAt(i);
                    Pending.Enqueue(request);
                }
            }
        }

        private static void UpdatePending()
        {
            for (int i = 0; i < Protocol.REQUEST_MAX_CONCURRENT; i++)
            {
                if (!Pending.Any() || Active.Count >= Protocol.REQUEST_MAX_CONCURRENT)
                    return;

                //start next request
                NetRequest next = Pending.Dequeue();
                next.StartAsync();
            }
        }
    }
}